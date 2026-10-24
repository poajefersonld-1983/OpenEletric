using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
namespace CircuitoStudio {
 [DataContract] public sealed class MotorStartingData {
  [DataMember] public double ShaftKw {get;set;}
  [DataMember] public double Efficiency {get;set;}
  [DataMember] public double StartingCurrentA {get;set;}
  [DataMember] public double? StartingPowerFactor {get;set;}
  [DataMember] public double StartingSeconds {get;set;}
  [DataMember] public double? AllowedStartDropPercent {get;set;}
  [DataMember] public string Source {get;set;}
  public MotorStartingData(){Efficiency=1;Source="";}
  public MotorStartingData Copy(){return (MotorStartingData)MemberwiseClone();}
  public void Validate(){Project.Range(ShaftKw,.000001,1e9,"potência no eixo");Project.Range(Efficiency,.001,1,"rendimento do motor");Project.Range(StartingCurrentA,0,1e9,"corrente declarada na partida");if(StartingPowerFactor.HasValue)Project.Range(StartingPowerFactor.Value,0,1,"FP na partida");Project.Range(StartingSeconds,0,1e6,"tempo declarado na partida");if(AllowedStartDropPercent.HasValue)Project.Range(AllowedStartDropPercent.Value,.01,100,"limite de queda na partida");if(Source==null||Source.Length>2000||Source.IndexOf('\0')>=0)throw new InvalidDataException("Referência dos dados do motor inválida.");Project.Range(MotorInput.ElectricalPowerKw(ShaftKw,Efficiency),0,1e9,"potência elétrica do motor");}
 }
 public sealed partial class Circuit {
  [DataMember(Order=84),Browsable(false)] public MotorStartingData MotorStarting {get;set;}
 }
 public sealed class MotorStartingResult {
  public double ElectricalKw,NominalCurrentA;public double? StartingDropPercent,PathROhm,PathXOhm;public string Status="Pendente",Detail="";
 }
 public static class MotorStarting {
  public static void Apply(Project project,Board board,Circuit circuit,MotorStartingData values,PhaseMode phases,double voltage,double powerFactor){
   if(project==null||board==null||circuit==null||values==null||!project.Boards.Contains(board)||!board.Circuits.Contains(circuit))throw new InvalidDataException("Selecione um circuito do projeto.");project.Validate();values.Validate();if(circuit.ChildBoardId!="")throw new InvalidDataException("Motor exige circuito terminal.");if(!Enum.IsDefined(typeof(PhaseMode),phases))throw new InvalidDataException("Fases do motor inválidas.");Project.Range(voltage,1,1e6,"tensão do motor");Project.Range(powerFactor,.000001,1,"FP nominal do motor");
   double expected=phases==PhaseMode.Trifasico?board.Voltage:board.Voltage/Math.Sqrt(3);if(Math.Abs(voltage-expected)>Math.Max(1,expected*.01))throw new InvalidDataException("A tensão do motor deve corresponder ao quadro (linha-linha em 3 fases, fase-neutro em 1 fase). Transformação local não é modelada.");
   var copy=values.Copy();double input=MotorInput.ElectricalPowerKw(copy.ShaftKw,copy.Efficiency);
   circuit.PowerKw=input;circuit.Phases=phases;circuit.Voltage=voltage;circuit.PowerFactor=powerFactor;circuit.PhaseLabel=phases==PhaseMode.Trifasico?"ABC":circuit.PhaseLabel=="ABC"?"L1":circuit.PhaseLabel;circuit.ReactiveKvarOverride=null;circuit.DriveMotor=null;circuit.Kind="Motor";circuit.MotorStarting=copy;
  }
  public static void Remove(Circuit circuit){if(circuit==null)throw new ArgumentNullException("circuit");circuit.MotorStarting=null;}
  internal static void ValidateCircuit(Circuit circuit,Board board){var data=circuit.MotorStarting;if(data==null)return;data.Validate();if(circuit.ChildBoardId!=""||circuit.DriveMotor!=null||circuit.ReactiveKvarOverride.HasValue)throw new InvalidDataException("Partida de motor deve ser terminal, sem inversor associado ou Q substituído.");double input=MotorInput.ElectricalPowerKw(data.ShaftKw,data.Efficiency);if(Math.Abs(input-circuit.PowerKw)>1e-9*Math.Max(1,input))throw new InvalidDataException("Altere potência e rendimento pelo painel do motor para preservar os dados de origem.");double expected=circuit.Phases==PhaseMode.Trifasico?board.Voltage:board.Voltage/Math.Sqrt(3);if(Math.Abs(circuit.Voltage-expected)>Math.Max(1,expected*.01))throw new InvalidDataException("Tensão do motor incompatível com seu quadro.");}
  internal static MotorStartingResult Evaluate(Project project,Study study,Circuit circuit){
   var data=circuit.MotorStarting;var row=study.For(circuit);var result=new MotorStartingResult{ElectricalKw=MotorInput.ElectricalPowerKw(data.ShaftKw,data.Efficiency)};result.NominalCurrentA=result.ElectricalKw*1000/(circuit.Voltage*circuit.PowerFactor*(circuit.Phases==PhaseMode.Trifasico?Math.Sqrt(3):1));
   var pending=new List<string>();
   if(circuit.Phases!=PhaseMode.Trifasico)pending.Add("modelo de partida monofásica não implementado");
   if(!data.StartingPowerFactor.HasValue)pending.Add("FP de partida desconhecido");if(data.StartingCurrentA<=0)pending.Add("corrente de partida desconhecida");if(data.StartingSeconds<=0)pending.Add("duração de partida desconhecida");if(!data.AllowedStartDropPercent.HasValue)pending.Add("limite específico de partida não informado");if(String.IsNullOrWhiteSpace(data.Source)||String.IsNullOrWhiteSpace(project.Source.NetworkReference))pending.Add("referência dos dados do motor ou alimentação ausente");
   if(!project.Source.SupportsConstantImpedanceWaveform)pending.Add("a fonte exige dinâmica de máquina ou controle de corrente");
   if(!study.PhaseCurrentKnown||study.Circuits.Any(item=>item.Circuit.ChildBoardId==""&&item.Circuit.Phases!=PhaseMode.Trifasico))pending.Add("modelo de partida pressupõe cargas e tensão equilibradas");
   var source=SourceNetwork.Calculate(project.Source);double r=source.TotalROhm??0,x=source.TotalXOhm??0;bool known=source.TotalROhm.HasValue&&source.TotalXOhm.HasValue&&project.Source.SupportsConstantImpedanceWaveform&&circuit.Phases==PhaseMode.Trifasico&&row.AccumulatedDropKnown;
   var path=new List<Circuit>{circuit};var board=row.Board;var parents=new Dictionary<string,Tuple<Board,Circuit>>();foreach(var b in project.Boards)foreach(var c in b.Circuits)if(c.ChildBoardId!="")parents.Add(c.ChildBoardId,Tuple.Create(b,c));
   while(parents.ContainsKey(board.Id)){var parent=parents[board.Id];path.Add(parent.Item2);board=parent.Item1;}
   if(Math.Abs(board.Voltage-project.Source.LineVoltage)>.001||Math.Abs(circuit.Voltage-project.Source.LineVoltage)>.001)known=false;
   if(path.Any(segment=>String.IsNullOrWhiteSpace(segment.Cable.Reference)))pending.Add("referência dos cabos do percurso ausente");
   foreach(var segment in path){var cable=segment.Cable;bool available=cable.ResistanceOhmKm>0||segment.CableMm2>0;if(!available){known=false;continue;}double resistance=cable.ResistanceOhmKm>0?cable.ResistanceOhmKm:(cable.Material==ConductorMaterial.Copper?23.7:37.6)/segment.CableMm2;r+=resistance*segment.LengthM/1000/cable.Parallel;x+=cable.ReactanceOhmKm*segment.LengthM/1000/cable.Parallel;}
   if(known&&data.StartingCurrentA>0&&data.StartingPowerFactor.HasValue){
    result.PathROhm=r;result.PathXOhm=x;
    double baseSourceDrop=Math.Sqrt(3)*(source.TotalROhm.Value*study.TotalKw*1000/(Math.Sqrt(3)*project.Source.LineVoltage)+source.TotalXOhm.Value*study.TotalKvar*1000/(Math.Sqrt(3)*project.Source.LineVoltage));
    double normalReal=row.DemandKw*1000/(Math.Sqrt(3)*circuit.Voltage),normalReactive=row.ReactiveKvar*1000/(Math.Sqrt(3)*circuit.Voltage);
    double deltaReal=data.StartingCurrentA*data.StartingPowerFactor.Value-normalReal,deltaReactive=data.StartingCurrentA*Math.Sqrt(1-data.StartingPowerFactor.Value*data.StartingPowerFactor.Value)-normalReactive;
    result.StartingDropPercent=row.AccumulatedDropPercent+100*baseSourceDrop/circuit.Voltage+100*Math.Sqrt(3)*(r*deltaReal+x*deltaReactive)/circuit.Voltage;
   }else pending.Add("impedância completa da fonte/percurso ou contexto de tensão indisponível");
   bool failed=result.StartingDropPercent.HasValue&&data.AllowedStartDropPercent.HasValue&&Math.Abs(result.StartingDropPercent.Value)>data.AllowedStartDropPercent.Value;
   result.Status=failed?"Falha":pending.Count>0?"Pendente":"OK";
   var text=new StringBuilder();text.AppendLine("Motor: eixo "+N(data.ShaftKw)+" kW; rendimento "+N(data.Efficiency)+"; P elétrica="+N(result.ElectricalKw)+" kW; I nominal calculada sem demanda="+N(result.NominalCurrentA)+" A.");text.AppendLine("Partida declarada: "+N(data.StartingCurrentA)+" A; FP="+(data.StartingPowerFactor.HasValue?N(data.StartingPowerFactor.Value):"desconhecido")+"; duração="+N(data.StartingSeconds)+" s; limite="+(data.AllowedStartDropPercent.HasValue?N(data.AllowedStartDropPercent.Value)+"%":"não informado")+".");text.AppendLine("Variação estimada na partida="+(result.StartingDropPercent.HasValue?N(result.StartingDropPercent.Value)+"%":"pendente")+"; R/X do percurso completo="+(result.PathROhm.HasValue?N(result.PathROhm.Value)+" / "+N(result.PathXOhm.Value)+" Ω":"pendente")+". Fonte dos dados: "+data.Source);
   text.AppendLine("A corrente declarada de partida não é multiplicada pelo fator de demanda. Modelo linear equilibrado com corrente RMS constante: substitui o fasor de corrente do motor no ponto de operação, preservando as demais cargas, e inclui a fonte. Não calcula aceleração, torque, aquecimento, variação da corrente com a tensão, contribuição do motor ao curto ou atuação da proteção. Um motor é avaliado por vez; os demais permanecem no ponto de operação. O resultado OK refere-se apenas ao limite de variação informado.");if(pending.Count>0)text.AppendLine("Pendências: "+String.Join("; ",pending)+".");result.Detail=text.ToString();return result;
  }
  static string N(double value){return value.ToString("N6",CultureInfo.GetCultureInfo("pt-BR"));}
 }
}
