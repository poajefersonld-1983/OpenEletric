using System;using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
namespace CircuitoStudio {
public static class MotorInput {public static double ElectricalPowerKw(double shaftPowerKw,double efficiency){Project.Range(shaftPowerKw,0,1e9,"potência mecânica do motor");Project.Range(efficiency,.001,1,"rendimento do motor");return shaftPowerKw/efficiency;}}
[DataContract] public enum ConductorMaterial {[EnumMember] Copper,[EnumMember] Aluminium}
[DataContract] public enum CableInsulation {[EnumMember] PVC,[EnumMember] XLPE}
[DataContract,TypeConverter(typeof(ExpandableObjectConverter))] public partial class CableData {
 [DataMember,DisplayName("Material"),Description("Material. Conferir dados nominais e condições de referência do fabricante.")] public ConductorMaterial Material {get;set;}
 [DataMember,DisplayName("Isolação"),Description("Isolação. Conferir dados nominais e condições de referência do fabricante.")] public CableInsulation Insulation {get;set;}
 [DataMember,DisplayName("Resistência (Ω/km), 0=derivar"),Description("Resistência (Ω/km), 0=derivar. Conferir dados nominais e condições de referência do fabricante.")] public double ResistanceOhmKm {get;set;}
 [DataMember,DisplayName("Reatância (Ω/km)"),Description("Reatância (Ω/km). Conferir dados nominais e condições de referência do fabricante.")] public double ReactanceOhmKm {get;set;}
 [DataMember,DisplayName("Ampacidade de referência (A), 0=desconhecida"),Description("Ampacidade de referência (A), 0=desconhecida. Conferir dados nominais e condições de referência do fabricante.")] public double AmpacityA {get;set;}
 [DataMember,DisplayName("Fator de temperatura"),Description("Fator de temperatura. Conferir dados nominais e condições de referência do fabricante.")] public double TemperatureFactor {get;set;}
 [DataMember,DisplayName("Fator de agrupamento"),Description("Fator de agrupamento. Conferir dados nominais e condições de referência do fabricante.")] public double GroupingFactor {get;set;}
 [DataMember,DisplayName("Condutores em paralelo por fase"),Description("Condutores em paralelo por fase. Conferir dados nominais e condições de referência do fabricante.")] public int Parallel {get;set;}
 [DataMember,DisplayName("Método de instalação"),Description("Método de instalação. Conferir dados nominais e condições de referência do fabricante.")] public string InstallationMethod {get;set;}
 [DataMember,DisplayName("Referência da tabela"),Description("Referência da tabela. Conferir dados nominais e condições de referência do fabricante.")] public string Reference {get;set;}
 public CableData(){ Defaults(); }
 void Defaults(){ReactanceOhmKm=.08;TemperatureFactor=GroupingFactor=1;Parallel=1;InstallationMethod="Informado";Reference="";}
 [OnDeserializing] void Read(StreamingContext c){Defaults();}
 public override string ToString(){return Material+" / "+Insulation;}
}
[DataContract,TypeConverter(typeof(ExpandableObjectConverter))] public partial class ProtectionData {
 [DataMember,DisplayName("Capacidade de interrupção (kA), 0=desconhecida"),Description("Capacidade de interrupção (kA), 0=desconhecida. Conferir dados nominais e condições de referência do fabricante.")] public double BreakingCapacityKA {get;set;}
 [DataMember,DisplayName("Corrente convencional I2 (A), 0=desconhecida"),Description("Corrente convencional I2 (A), 0=desconhecida. Conferir dados nominais e condições de referência do fabricante.")] public double ConventionalTripA {get;set;}
 [DataMember,DisplayName("Energia passante I²t (A²s), 0=desconhecida"),Description("Energia passante I²t (A²s), 0=desconhecida. Conferir dados nominais e condições de referência do fabricante.")] public double LetThroughA2s {get;set;}
 [DataMember,DisplayName("Tempo de seccionamento (s), 0=desconhecido"),Description("Tempo de seccionamento (s), 0=desconhecido. Conferir dados nominais e condições de referência do fabricante.")] public double DisconnectSeconds {get;set;}
 [DataMember,DisplayName("Sensibilidade DR (mA), 0=ausente"),Description("Sensibilidade DR (mA), 0=ausente. Conferir dados nominais e condições de referência do fabricante.")] public double RcdMilliA {get;set;}
 [DataMember,DisplayName("Resistência de terra Ra (Ω), 0=desconhecida"),Description("Resistência de terra Ra (Ω), 0=desconhecida. Conferir dados nominais e condições de referência do fabricante.")] public double EarthResistanceOhm {get;set;}
 [DataMember,DisplayName("Limite de contato UL (V)"),Description("Limite de contato UL (V). Conferir dados nominais e condições de referência do fabricante.")] public double ContactLimitV {get;set;}
 [DataMember,DisplayName("Corrente de atuação Ia (A), 0=desconhecida"),Description("Corrente de atuação Ia (A), 0=desconhecida. Conferir dados nominais e condições de referência do fabricante.")] public double MagneticTripA {get;set;}
 [DataMember,DisplayName("Impedância do laço Zs (Ω), 0=desconhecida"),Description("Impedância do laço Zs (Ω), 0=desconhecida. Conferir dados nominais e condições de referência do fabricante.")] public double FaultLoopOhm {get;set;}
 public ProtectionData(){ContactLimitV=50;}
 [OnDeserializing] void Read(StreamingContext c){ContactLimitV=50;}
 public override string ToString(){return BreakingCapacityKA>0?BreakingCapacityKA+" kA":"Dados pendentes";}
}
[DataContract,TypeConverter(typeof(ExpandableObjectConverter))] public partial class SourceSettings {
 [DataMember,DisplayName("Tensão entre fases (V)"),Description("Tensão entre fases (V). Conferir dados nominais e condições de referência do fabricante.")] public double LineVoltage {get;set;}
 [DataMember,DisplayName("Icc trifásico da rede (kA), 0=desconhecido"),Description("Interpretado conforme Base da corrente de curto: referido à BT ou medido na rede MT antes do transformador. Não informe o curto já resultante após o transformador quando esse transformador estiver ativado.")] public double ShortCircuitKA {get;set;}
 [DataMember,DisplayName("Relação R/X da fonte"),Description("Relação R/X da fonte. Conferir dados nominais e condições de referência do fabricante.")] public double RXRatio {get;set;}
 [DataMember,DisplayName("Fator de tensão"),Description("Fator de tensão. Conferir dados nominais e condições de referência do fabricante.")] public double VoltageFactor {get;set;}
 [DataMember,DisplayName("Transformador (kVA), 0=desativado"),Description("Transformador (kVA), 0=desativado. Conferir dados nominais e condições de referência do fabricante.")] public double TransformerKva {get;set;}
 [DataMember,DisplayName("Impedância uk (%)"),Description("Impedância uk (%). Conferir dados nominais e condições de referência do fabricante.")] public double TransformerUkPercent {get;set;}
 [DataMember,DisplayName("Perdas no cobre (kW), 0=desconhecidas"),Description("Perdas no cobre (kW), 0=desconhecidas. Conferir dados nominais e condições de referência do fabricante.")] public double TransformerCopperLossKw {get;set;}
 [DataMember,DisplayName("Queda máxima acumulada (%)"),Description("Queda máxima acumulada (%). Conferir dados nominais e condições de referência do fabricante.")] public double MaxDropPercent {get;set;}
 [DataMember,DisplayName("Fator de potência alvo"),Description("Fator de potência alvo. Conferir dados nominais e condições de referência do fabricante.")] public double TargetPowerFactor {get;set;}
 public SourceSettings(){Defaults();}
 void Defaults(){LineVoltage=380;RXRatio=.1;VoltageFactor=1;TransformerUkPercent=6;MaxDropPercent=4;TargetPowerFactor=.95;}
 [OnDeserializing] void Read(StreamingContext c){Defaults();}
 public override string ToString(){return LineVoltage+" V";}
}
public sealed partial class Project {
 string standardReference,calculationMethod;
 [DataMember(Order=30),Category("Método"),DisplayName("Referência normativa")] public string StandardReference {get{return standardReference??"Não definido";}set{standardReference=value;}}
 [DataMember(Order=31),Category("Método"),DisplayName("Método de cálculo")] public string CalculationMethod {get{return calculationMethod??"Radial senoidal equilibrado — OpenEletric 0.2";}set{calculationMethod=value;}}
 [DataMember(Order=20),Category("Estudos"),DisplayName("Alimentação e limites")] public SourceSettings Source {get;set;}
 [OnDeserializing] void EngineeringDefaults(StreamingContext c){Source=new SourceSettings();}
 internal void ValidateEngineering(){
  Text(StandardReference,"referência normativa",false);Text(CalculationMethod,"método de cálculo",false);
  if(!Enum.IsDefined(typeof(DiagramTheme),DrawingTheme))throw new InvalidDataException("Estilo de desenho inválido.");
  if(Source==null)throw new InvalidDataException("Alimentação ausente.");
  Range(Source.LineVoltage,1,1000000,"tensão fonte");Range(Source.ShortCircuitKA,0,1000000,"curto fonte");Range(Source.RXRatio,0,1000,"R/X");Range(Source.VoltageFactor,.1,2,"fator tensão");
  Range(Source.TransformerKva,0,1e9,"transformador");Range(Source.TransformerUkPercent,.01,100,"uk");Range(Source.TransformerCopperLossKw,0,1e9,"perdas cobre");Range(Source.MaxDropPercent,.01,100,"limite queda");Range(Source.TargetPowerFactor,.001,1,"FP alvo");
  Source.ValidateNetwork();
  var boards=new Dictionary<string,Board>();foreach(var b in Boards)boards.Add(b.Id,b);
  var parent=new Dictionary<string,string>();
  foreach(var b in Boards)foreach(var x in b.Circuits){
   if(!Enum.IsDefined(typeof(DiagramSymbol),x.DrawingSymbol)||x.DiagramLabel.Length>200||x.DiagramLabel.IndexOf('\0')>=0)throw new InvalidDataException("Símbolo inválido ou etiqueta maior que 200 caracteres.");
   if(x.DriveMotor!=null){x.DriveMotor.Validate();if(x.ChildBoardId!="")throw new InvalidDataException("Inversor/motor deve estar em circuito terminal, sem alimentar outro quadro.");}
   if(x.CurveStudy!=null)x.CurveStudy.Validate();
   if(x.ReactiveKvarOverride.HasValue)Range(x.ReactiveKvarOverride.Value,-1e9,1e9,"potência reativa explícita");
   Text(x.ChildBoardId,"quadro destino",false);Text(x.PhaseLabel,"fase",true);
   if(x.PhaseLabel!="L1"&&x.PhaseLabel!="L2"&&x.PhaseLabel!="L3"&&x.PhaseLabel!="ABC")throw new InvalidDataException("Fase inválida.");
   if(x.Cable==null||x.Protection==null)throw new InvalidDataException("Dados elétricos ausentes.");
   var a=x.Cable;var p=x.Protection;CableFactors.Validate(a);BreakerSelection.Validate(x);
   if(!Enum.IsDefined(typeof(ConductorMaterial),a.Material)||!Enum.IsDefined(typeof(CableInsulation),a.Insulation))throw new InvalidDataException("Material/isolação inválidos.");
   Range(a.ResistanceOhmKm,0,1e9,"resistência");Range(a.ReactanceOhmKm,0,1e9,"reatância");Range(a.AmpacityA,0,1e9,"ampacidade");Range(a.TemperatureFactor,.001,10,"fator temperatura");Range(a.GroupingFactor,.001,10,"fator agrupamento");Range(a.Parallel,1,1000,"paralelos");Text(a.InstallationMethod,"instalação",false);Text(a.Reference,"referência",false);
   foreach(double v in new[]{p.BreakingCapacityKA,p.ConventionalTripA,p.LetThroughA2s,p.DisconnectSeconds,p.RcdMilliA,p.EarthResistanceOhm,p.MagneticTripA,p.FaultLoopOhm})Range(v,0,1e15,"proteção");
   Range(p.ContactLimitV,.01,1000,"tensão contato");
   if(x.ChildBoardId!=""){
    if(!boards.ContainsKey(x.ChildBoardId)||x.ChildBoardId==b.Id||parent.ContainsKey(x.ChildBoardId))throw new InvalidDataException("Topologia inválida: quadro inexistente, próprio ou múltiplos alimentadores.");
    if(x.Phases!=PhaseMode.Trifasico||Math.Abs(x.Voltage-boards[x.ChildBoardId].Voltage)>.001||Math.Abs(x.Voltage-b.Voltage)>.001)throw new InvalidDataException("Alimentador deve ser trifásico e ter a mesma tensão entre fases dos quadros de origem e destino; transformação entre quadros não é modelada.");
    parent.Add(x.ChildBoardId,b.Id);
   }
  }
  foreach(var b in Boards){var seen=new HashSet<string>();string id=b.Id;while(parent.ContainsKey(id)){if(!seen.Add(id))throw new InvalidDataException("Topologia contém ciclo.");if(seen.Count>64)throw new InvalidDataException("A topologia excede 64 níveis de quadros.");id=parent[id];}}
 }
}
public sealed partial class Circuit {
 [DataMember(Order=25),Category("Estudos"),DisplayName("Limite de queda acumulada (%)"),Description("Vazio: herdar o limite do projeto. Valor informado compara o módulo da queda acumulada até este circuito; elevação capacitiva também é verificada.")] public double? VoltageDropLimitPercent {get;set;}

 [DataMember(Order=24),Category("Carga"),DisplayName("Reativo explícito (kvar)"),Description("Opcional; substitui Q derivado do fator de potência. Valor negativo para banco capacitivo. Multiplicado pelo fator de demanda.")] public double? ReactiveKvarOverride {get;set;}
 [DataMember(Order=20),Category("Topologia"),DisplayName("Quadro alimentado (ID)")] public string ChildBoardId {get;set;}
 [DataMember(Order=21),Category("Carga"),DisplayName("Fase (L1/L2/L3/ABC)")] public string PhaseLabel {get;set;}
 [DataMember(Order=22),Category("Estudos"),DisplayName("Cabo")] public CableData Cable {get;set;}
 [DataMember(Order=23),Category("Estudos"),DisplayName("Proteção")] public ProtectionData Protection {get;set;}
 [OnDeserializing] void EngineeringDefaults(StreamingContext c){ChildBoardId="";PhaseLabel="L1";Cable=new CableData();Protection=new ProtectionData();}
}
public class Check {public string CircuitId,Rule,Status,Detail;}
public class CircuitResult {
 public bool VoltageDropKnown,AccumulatedDropKnown; public Circuit Circuit;public Board Board;public double CurrentA,DemandKw,ReactiveKvar,VoltageDropV,VoltageDropPercent,AccumulatedDropPercent;
 public double? FaultOriginKA,FaultEndKA,FaultTwoPhaseOriginKA,FaultTwoPhaseEndKA,FaultLoopKA,CorrectedAmpacityA,AdiabaticMinMm2;
 public FaultDynamics FaultOriginDynamics,FaultEndDynamics;public MotorStartingResult MotorStarting;
 public string Assumptions;
}
public class Study {
 public List<CircuitResult> Circuits=new List<CircuitResult>();public List<Check> Checks=new List<Check>();
 public Dictionary<string,BusbarResult> Busbars=new Dictionary<string,BusbarResult>();public FaultExtremeResult FaultExtremes;public NetworkFlowResult NetworkFlow;public double TotalKw,TotalKvar,TotalKva,PowerFactor,CapacitorKvar;public double[] PhaseCurrentA=new double[3];public bool PhaseCurrentKnown;
 Dictionary<Circuit,CircuitResult> lookup;public CircuitResult For(Circuit c){if(lookup==null||lookup.Count!=Circuits.Count){lookup=new Dictionary<Circuit,CircuitResult>();foreach(var row in Circuits)lookup[row.Circuit]=row;}CircuitResult result;return lookup.TryGetValue(c,out result)?result:null;}
}
public static class Engineering {
 const double Sqrt3=1.7320508075688772;
 struct Load {public double P,Q;public Load(double p,double q){P=p;Q=q;}}
 static Load Aggregate(Board b,Dictionary<string,Board> boards){double p=0,q=0;foreach(var c in b.Circuits){Load l=GetLoad(c,boards);p+=l.P;q+=l.Q;}return new Load(p,q);}
 static Load GetLoad(Circuit c,Dictionary<string,Board> boards){return c.ChildBoardId!=""?Aggregate(boards[c.ChildBoardId],boards):new Load(c.DemandKw,c.ReactiveKvarOverride.HasValue?c.ReactiveKvarOverride.Value*c.DemandFactor:c.DemandKw*Math.Tan(Math.Acos(c.PowerFactor)));}
 static void Add(Study s,Circuit c,string rule,bool? pass,string detail){s.Checks.Add(new Check{CircuitId=c.Id,Rule=rule,Status=pass.HasValue?(pass.Value?"OK":"Falha"):"Pendente",Detail=detail});}
 public static Study Analyze(Project p){
  p.Validate();var s=new Study();var boards=new Dictionary<string,Board>();var children=new HashSet<string>();
  foreach(var b in p.Boards){boards.Add(b.Id,b);foreach(var c in b.Circuits)if(c.ChildBoardId!="")children.Add(c.ChildBoardId);}
  var src=p.Source;var sourceImpedance=SourceNetwork.Calculate(src);double? r=sourceImpedance.TotalROhm,x=sourceImpedance.TotalXOhm;
  foreach(var b in p.Boards)if(!children.Contains(b.Id)){var l=Aggregate(b,boards);s.TotalKw+=l.P;s.TotalKvar+=l.Q;Visit(p,s,b,boards,0,true,Math.Abs(b.Voltage-src.LineVoltage)<.001?r:null,Math.Abs(b.Voltage-src.LineVoltage)<.001?x:null);}
  double[] phaseActiveCurrent=new double[3],phaseReactiveCurrent=new double[3];s.PhaseCurrentKnown=p.Boards.TrueForAll(b=>Math.Abs(b.Voltage-src.LineVoltage)<.001);
  foreach(var b in p.Boards)foreach(var c in b.Circuits)if(c.ChildBoardId==""){
   var l=GetLoad(c,boards);if(c.Phases==PhaseMode.Trifasico){if(Math.Abs(c.Voltage-src.LineVoltage)>.001)s.PhaseCurrentKnown=false;double vphase=c.Voltage/Sqrt3;for(int i=0;i<3;i++){phaseActiveCurrent[i]+=(l.P/3)*1000/vphase;phaseReactiveCurrent[i]+=(l.Q/3)*1000/vphase;}}
   else{if(Math.Abs(c.Voltage-src.LineVoltage/Sqrt3)>Math.Max(1,src.LineVoltage/Sqrt3*.01))s.PhaseCurrentKnown=false;int i=c.PhaseLabel=="L1"?0:c.PhaseLabel=="L2"?1:c.PhaseLabel=="L3"?2:-1;if(i>=0){phaseActiveCurrent[i]+=l.P*1000/c.Voltage;phaseReactiveCurrent[i]+=l.Q*1000/c.Voltage;}else s.PhaseCurrentKnown=false;}
  }
  for(int i=0;i<3;i++)s.PhaseCurrentA[i]=Math.Sqrt(phaseActiveCurrent[i]*phaseActiveCurrent[i]+phaseReactiveCurrent[i]*phaseReactiveCurrent[i]);
  s.TotalKva=Math.Sqrt(s.TotalKw*s.TotalKw+s.TotalKvar*s.TotalKvar);s.PowerFactor=s.TotalKva>0?s.TotalKw/s.TotalKva:1;
  if(p.FaultExtremes!=null){s.FaultExtremes=FaultExtremes.Run(p);foreach(var c in p.Boards.SelectMany(b=>b.Circuits))Add(s,c,"Extremos iniciais3F/LL: dados/modelo",s.FaultExtremes.Accepted&&c.Phases==PhaseMode.Trifasico?(bool?)true:null,s.FaultExtremes.Status+". "+s.FaultExtremes.Detail);}
  s.CapacitorKvar=Math.Max(0,s.TotalKvar-s.TotalKw*Math.Tan(Math.Acos(src.TargetPowerFactor)));foreach(var row in s.Circuits)if(row.Circuit.MotorStarting!=null){row.MotorStarting=MotorStarting.Evaluate(p,s,row.Circuit);s.Checks.Add(new Check{CircuitId=row.Circuit.Id,Rule="Partida do motor — variação estimada",Status=row.MotorStarting.Status,Detail=row.MotorStarting.Detail});}foreach(var row in s.Circuits)foreach(CoordinationKind kind in Enum.GetValues(typeof(CoordinationKind))){if((kind==CoordinationKind.Selectivity?row.Circuit.SelectivityEvidence:row.Circuit.BackupEvidence)==null)continue;var pair=Coordination.EvaluateBinding(p,s,row.Circuit,kind);s.Checks.Add(new Check{CircuitId=row.Circuit.Id,Rule=kind==CoordinationKind.Selectivity?"Seletividade por tabela de par":"Backup por tabela de par",Status=pair.Status,Detail=pair.Detail});}foreach(var row in s.Circuits)if(row.Circuit.SwitchgearSelection!=null){var device=SwitchgearSelection.Evaluate(p,s,row.Circuit,row.Circuit.SwitchgearSelection);s.Checks.Add(new Check{CircuitId=row.Circuit.Id,Rule="Fusível / seccionador nominal",Status=device.Status,Detail=device.Detail});}foreach(var row in s.Circuits)if(row.Circuit.ResidualSelection!=null){var residual=ResidualSelection.Evaluate(p,s,row.Circuit,row.Circuit.ResidualSelection);s.Checks.Add(new Check{CircuitId=row.Circuit.Id,Rule="Diferencial / relé / acessórios",Status=residual.Status,Detail=residual.Detail});}foreach(var row in s.Circuits)if(row.Circuit.MotorStarterSelection!=null){var starter=MotorStarterSelection.Evaluate(p,s,row.Circuit,row.Circuit.MotorStarterSelection);s.Checks.Add(new Check{CircuitId=row.Circuit.Id,Rule="Conjunto de partida do motor",Status=starter.Status,Detail=starter.Detail});}if(p.FlowSettings!=null||p.Boards.Any(b=>b.Circuits.Any(c=>c.FlowLoad!=null))){s.NetworkFlow=NetworkFlow.Run(p);foreach(var row in s.Circuits){var flow=s.NetworkFlow.For(row.Circuit);if(flow==null){Add(s,row.Circuit,"Fluxo por fase: convergência/modelo",null,s.NetworkFlow.Status+". "+s.NetworkFlow.Detail);continue;}Add(s,row.Circuit,"Fluxo por fase: queda máxima por ligação",flow.MaximumAbsDropPercent<=(row.Circuit.VoltageDropLimitPercent??p.Source.MaxDropPercent),"Máxima diferença de magnitudes | |E|−|V| | em base nominal="+flow.MaximumAbsDropPercent+"%; ligações "+flow.VoltageMeaning+"; PU mínimo/máximo="+flow.MinimumVoltagePu+" / "+flow.MaximumVoltagePu+". Modelo fundamental convergido, sem harmônicas.");Add(s,row.Circuit,"Fluxo por fase: corrente máxima ≤ Iz",row.CorrectedAmpacityA.HasValue?(bool?)(flow.MaximumPhaseCurrentA<=row.CorrectedAmpacityA):null,"Máxima fase fundamental="+flow.MaximumPhaseCurrentA+" A; não é média equilibrada nem inclui harmônicas.");var returns=row.Circuit.ReturnSizing;var neutral=returns==null?null:row.Circuit.Conductors!=null&&row.Circuit.Conductors.HasPEN==true?returns.Protective:returns.Neutral;if(flow.NeutralCurrentA>1e-8)Add(s,row.Circuit,"Fluxo por fase: neutro fundamental ≤ IzN",neutral!=null&&neutral.CorrectedIzA>0?(bool?)(flow.NeutralCurrentA<=neutral.CorrectedIzA):null,"IN fundamental="+flow.NeutralCurrentA+" A; harmônicas/IN RMS total permanecem estudo próprio.");}}
foreach(var b in p.Boards)if(b.BusbarSelection!=null){var evaluated=BusbarSelection.Evaluate(p,b,b.BusbarSelection);s.Busbars[b.Id]=evaluated;s.Checks.Add(new Check{CircuitId=b.Id,Rule="Conjunto de barras/busway do quadro",Status=evaluated.Accepted?"Atende":evaluated.Failed?"Falha":"Pendente",Detail=evaluated.Detail});}
return s;
 }
 static void Visit(Project p,Study s,Board b,Dictionary<string,Board> boards,double upstreamDrop,bool upstreamKnown,double? upstreamR,double? upstreamX){
  foreach(var c in b.Circuits){
   var l=GetLoad(c,boards);double kva=Math.Sqrt(l.P*l.P+l.Q*l.Q);double pf=kva>0?l.P/kva:1;
   var a=c.Cable;var pr=c.Protection;
   double current=c.ChildBoardId!=""||c.ReactiveKvarOverride.HasValue?kva*1000/(c.Voltage*(c.Phases==PhaseMode.Trifasico?Sqrt3:1)):c.DesignCurrentA;
   double rr=a.ResistanceOhmKm>0?a.ResistanceOhmKm:(c.CableMm2>0?(a.Material==ConductorMaterial.Copper?23.7:37.6)/c.CableMm2:0);
   double cableR=rr*c.LengthM/1000/a.Parallel, cableX=a.ReactanceOhmKm*c.LengthM/1000/a.Parallel;
   bool knownCable=a.ResistanceOhmKm>0||c.CableMm2>0;
   double drop=(c.Phases==PhaseMode.Trifasico?Sqrt3:2)*current*(cableR*pf+cableX*(kva>0?l.Q/kva:0));
   var result=new CircuitResult{VoltageDropKnown=knownCable,AccumulatedDropKnown=knownCable&&upstreamKnown,Circuit=c,Board=b,CurrentA=current,DemandKw=l.P,ReactiveKvar=l.Q,VoltageDropV=drop,VoltageDropPercent=100*drop/c.Voltage,AccumulatedDropPercent=upstreamDrop+100*drop/c.Voltage,Assumptions="Modelo radial senoidal equilibrado; curto trifásico aproximado e bifásico com impedâncias de sequência positiva/negativa iguais. Corrente fase-terra somente quando o Zs total é informado em circuito monofásico TT/TN. Sem curto mínimo/máximo, sistema IT, contribuição de motores ou paralelismo de fontes. Gerador, se selecionado, usa apenas a impedância inicial subtransitória; não é implementação certificada IEC 60909. Icc da fonte deve ser informado no ponto a montante do transformador opcional; impedância equivalente inferida na tensão nominal e fator de tensão selecionados. Se o Icc já corresponde ao secundário, deixar transformador desativado. Queda acumulada soma percentuais na mesma base nominal; circuitos monofásicos usam a tensão informada. R derivada a temperatura operacional; ampacidade e curvas exigem dados de referência."};
   if(upstreamR.HasValue&&upstreamX.HasValue){double z=Math.Sqrt(upstreamR.Value*upstreamR.Value+upstreamX.Value*upstreamX.Value);if(z>0){result.FaultOriginKA=p.Source.VoltageFactor*p.Source.LineVoltage/(Sqrt3*z*1000);result.FaultTwoPhaseOriginKA=p.Source.VoltageFactor*p.Source.LineVoltage/(2*z*1000);if(p.Source.SupportsConstantImpedanceWaveform)result.FaultOriginDynamics=FaultDynamics.Calculate(upstreamR.Value,upstreamX.Value,p.Frequency,result.FaultOriginKA.Value);}
    if(c.Phases==PhaseMode.Trifasico&&knownCable){double er=upstreamR.Value+cableR,ex=upstreamX.Value+cableX;double loop=Math.Sqrt(er*er+ex*ex);result.FaultEndKA=p.Source.VoltageFactor*p.Source.LineVoltage/(Sqrt3*loop*1000);result.FaultTwoPhaseEndKA=p.Source.VoltageFactor*p.Source.LineVoltage/(2*loop*1000);if(p.Source.SupportsConstantImpedanceWaveform)result.FaultEndDynamics=FaultDynamics.Calculate(er,ex,p.Frequency,result.FaultEndKA.Value);}}
   string factorDetail;bool factorsApplicable=CableFactors.Applicable(c,out factorDetail);if(a.AmpacityA>0&&factorsApplicable)result.CorrectedAmpacityA=a.AmpacityA*a.TemperatureFactor*a.GroupingFactor*a.Parallel;Add(s,c,"Condições e referência dos fatores",a.FactorsSnapshot==null?(bool?)null:factorsApplicable,CableFactors.Describe(c));
   s.Circuits.Add(result);
   if(c.DriveMotor!=null){var d=c.DriveMotor;Add(s,c,"Inversor: corrente nominal de entrada",current<=d.InputRatedA,"Ib estimada senoidal "+current.ToString("F2")+" A; corrente RMS nominal de entrada do inversor "+d.InputRatedA+" A. Dados de placa não substituem análise de harmônicas, regime e sobrecarga.");Add(s,c,"Inversor: corrente nominal de saída",d.MotorRatedA<=d.OutputRatedA,"Motor nominal "+d.MotorRatedA+" A; saída nominal do inversor "+d.OutputRatedA+" A. Comparação nominal; confirmar regime, frequência, desclassificação e dados do fabricante.");Add(s,c,"Inversor: cabo do motor e harmônicas",null,"O cabo deste circuito está na entrada do inversor. Cabo entre inversor e motor, PWM, EMC, correntes harmônicas e sobrecarga dinâmica não são avaliados. A potência do eixo não é somada novamente à carga de entrada.");}
   if(c.ReturnSizing!=null){if(c.Conductors!=null){if(c.Conductors.HasNeutral==true&&c.ReturnSizing.Neutral==null)Add(s,c,"Dimensionamento do neutro ausente",null,"Condutor N declarado sem seção selecionada.");if((c.Conductors.HasPE==true||c.Conductors.HasPEN==true)&&c.ReturnSizing.Protective==null)Add(s,c,"Dimensionamento PE/PEN ausente",null,"Condutor de proteção declarado sem seção selecionada.");}foreach(var conductor in new[]{c.ReturnSizing.Neutral,c.ReturnSizing.Protective}.Where(item=>item!=null)){var evaluated=ReturnSizing.Evaluate(p,c,c.ReturnSizing.Requirements,conductor);Add(s,c,"Dimensionamento "+conductor.Role,evaluated.Status=="Dados incompletos"?(bool?)null:evaluated.Status=="Atende critérios locais declarados",evaluated.Detail);}}
   if(c.LoopFault!=null){var derived=FaultLoops.Evaluate(p,c,c.LoopFault);Add(s,c,"Laço de falta (modelo declarado)",derived.Current.HasValue?(bool?)true:null,derived.Detail);}
   if(c.ContactTimeStudy!=null){var contact=ContactTime.Evaluate(p,c,c.ContactTimeStudy);Add(s,c,"Contato e tempo (regra declarada)",contact.Status=="Dados incompletos"?(bool?)null:contact.Status=="Atende comparações do caso declarado",contact.Detail);}
   double dropLimit=c.VoltageDropLimitPercent??p.Source.MaxDropPercent;Add(s,c,"Queda de tensão",result.AccumulatedDropKnown?(bool?)(Math.Abs(result.AccumulatedDropPercent)<=dropLimit):null,"Variação acumulada "+result.AccumulatedDropPercent.ToString("F2")+"% (negativa = elevação); limite absoluto "+dropLimit+"% ("+(c.VoltageDropLimitPercent.HasValue?"definido no circuito":"herdado do projeto")+"). Sem seção ou R, cálculo pendente.");
   Add(s,c,"Sobrecarga Ib ≤ In ≤ Iz",result.CorrectedAmpacityA.HasValue&&c.BreakerA>0?(bool?)(current<=c.BreakerA&&c.BreakerA<=result.CorrectedAmpacityA.Value):null,"Iz exige ampacidade de referência e fatores informados.");
   Add(s,c,"Atuação convencional I2 ≤ 1,45 Iz",result.CorrectedAmpacityA.HasValue&&pr.ConventionalTripA>0?(bool?)(pr.ConventionalTripA<=1.45*result.CorrectedAmpacityA.Value):null,"I2 deve vir da curva/norma do dispositivo.");
   string breakerDetail="Dados manuais; tensão, polos e classificação Icu/Icn ainda sem vínculo.";bool breakerApplies=pr.BreakerSnapshot==null||BreakerSelection.Applicable(p,result,pr.BreakerSnapshot,out breakerDetail);Add(s,c,"Condições do disjuntor",pr.BreakerSnapshot==null?(bool?)null:breakerApplies,breakerDetail);Add(s,c,"Capacidade de interrupção",breakerApplies&&result.FaultOriginKA.HasValue&&pr.BreakingCapacityKA>0?(bool?)(pr.BreakingCapacityKA>=result.FaultOriginKA.Value):null,"Comparação com curto nominal 3F aproximado na origem; Ik máximo, coordenação e condições ambientais ainda exigem estudo. "+breakerDetail);
   double k=a.Material==ConductorMaterial.Copper?(a.Insulation==CableInsulation.PVC?115:143):(a.Insulation==CableInsulation.PVC?76:94);
   if(a.Insulation==CableInsulation.PVC&&c.CableMm2>300)k=a.Material==ConductorMaterial.Copper?103:68;
   if(pr.LetThroughA2s>0&&a.Parallel==1&&pr.DisconnectSeconds>0&&pr.DisconnectSeconds<=5)result.AdiabaticMinMm2=Math.Sqrt(pr.LetThroughA2s)/k;
   Add(s,c,"Suportabilidade adiabática",result.AdiabaticMinMm2.HasValue&&c.CableMm2>0?(bool?)(c.CableMm2>=result.AdiabaticMinMm2.Value):null,"Usa somente I²t informado e k PVC/XLPE Cu/Al; modelo adiabático até 5 s, temperaturas iniciais/finais PVC 70/160 °C (140 °C acima de 300 mm²), XLPE 90/250 °C. Confirmar condições do fabricante; paralelos exigem análise da divisão da falta.");
   string effectiveEarthing=Grounding.EffectiveSystem(p,c);bool tt=effectiveEarthing.ToUpperInvariant().Contains("TT"),tn=effectiveEarthing.ToUpperInvariant().Contains("TN");double? earthRa=Grounding.Ra(p,c);if(p.GroundingSource!=null)Add(s,c,"Topologia de aterramento declarada",Grounding.Complete(p,c)?(bool?)true:null,Grounding.Describe(p,c));
   if(tt)Add(s,c,"TT: Ra × IΔn ≤ UL",earthRa.HasValue&&pr.RcdMilliA>0?(bool?)(earthRa.Value*pr.RcdMilliA/1000<=pr.ContactLimitV):null,"Somente critério de contato; não comprova tempo de seccionamento.");
   bool neutralKnown=p.Source.SupportsVoltageSourceFaults&&c.Phases==PhaseMode.Monofasico&&Math.Abs(b.Voltage-p.Source.LineVoltage)<.001&&Math.Abs(c.Voltage-b.Voltage/Sqrt3)<=Math.Max(1,b.Voltage/Sqrt3*.01)&&c.PhaseLabel!="ABC";
   if(p.GroundingSource!=null)neutralKnown=neutralKnown&&p.GroundingSource.NeutralDistributed==true&&p.GroundingSource.NeutralConnection==NeutralEarthConnection.Direct&&c.Conductors!=null&&c.Conductors.Connection==LiveConnection.LineNeutral&&(c.Conductors.HasNeutral==true||c.Conductors.HasPEN==true);
   if((tt||tn)&&neutralKnown&&pr.FaultLoopOhm>0)result.FaultLoopKA=(p.Source.LineVoltage/Sqrt3)/pr.FaultLoopOhm/1000;
   if(tn)Add(s,c,"TN: Zs × Ia ≤ U0",neutralKnown&&pr.FaultLoopOhm>0&&pr.MagneticTripA>0?(bool?)(pr.FaultLoopOhm*pr.MagneticTripA<=p.Source.LineVoltage/Sqrt3):null,"Critério disponível somente em circuito monofásico fase-neutro com tensão ≈ Ulinha/√3 e quadro compatível com fonte; confirmar neutro/estrela e corrente de atuação. Fase-fase ou contexto de neutro desconhecido: pendente.");
   Add(s,c,"Tempo de seccionamento",null,"Exige curva, tipo de falta e limite aplicável; tempo informado isoladamente não comprova atuação.");
   if(c.ChildBoardId!="")Visit(p,s,boards[c.ChildBoardId],boards,result.AccumulatedDropPercent,result.AccumulatedDropKnown,knownCable&&upstreamR.HasValue?upstreamR+cableR:null,knownCable&&upstreamX.HasValue?upstreamX+cableX:null);
  }
 }
}
}

