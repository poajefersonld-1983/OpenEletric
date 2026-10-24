using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
namespace CircuitoStudio {
 [DataContract] public enum SourceCurrentBasis {
  [EnumMember] ReferidaBT,
  [EnumMember] RedeMT
 }
 [DataContract] public enum SourceModel {
  [EnumMember] Rede,
  [EnumMember] GeradorSincrono,
  [EnumMember] ImpedanciaDefinida,
  [EnumMember] UPS
 }
 [DataContract] public enum UPSOperatingMode {[EnumMember] Inversor,[EnumMember] Bypass}
 public partial class SourceSettings {
  [DataMember,DisplayName("UPS — modo declarado")] public UPSOperatingMode UPSMode {get;set;}
  [DataMember,DisplayName("UPS — potência nominal (kVA)")] public double UPSRatedKva {get;set;}
  [DataMember,DisplayName("UPS — limite de curto do inversor (kA)"),Description("Dado do fabricante. Zero: desconhecido. Não é corrente prospectiva calculada no circuito; não representa operação em bypass.")] public double UPSCurrentLimitKA {get;set;}
  [DataMember,DisplayName("UPS — duração do limite declarado (s)"),Description("Dado do fabricante. Zero: duração desconhecida. Não é tempo de atuação da proteção.")] public double UPSCurrentLimitSeconds {get;set;}
  [DataMember,DisplayName("Fonte definida — R por fase (Ω)"),Description("Fonte equilibrada; sequência positiva, por fase. Vazio: desconhecido. Zero é valor explicitamente informado.")] public double? DefinedPhaseROhm {get;set;}
  [DataMember,DisplayName("Fonte definida — X por fase (Ω)")] public double? DefinedPhaseXOhm {get;set;}
  [DataMember,DisplayName("Fonte definida — R neutro (Ω)")] public double? DefinedNeutralROhm {get;set;}
  [DataMember,DisplayName("Fonte definida — X neutro (Ω)")] public double? DefinedNeutralXOhm {get;set;}
  [DataMember,DisplayName("Fonte definida — R PE (Ω)")] public double? DefinedPEROhm {get;set;}
  [DataMember,DisplayName("Fonte definida — X PE (Ω)")] public double? DefinedPEXOhm {get;set;}
  [Browsable(false)] public bool SupportsVoltageSourceFaults {get{return Model!=SourceModel.UPS||UPSMode==UPSOperatingMode.Bypass;}}

  [DataMember,DisplayName("Modelo da fonte"),Description("Rede: corrente informada e transformador opcional. Gerador: curto inicial subtransitório. ImpedanciaDefinida: fonte equilibrada por fase, N/PE separados. UPS: inversor limitado ou bypass explicitamente escolhido; sem transição automática.")] public SourceModel Model {get;set;}
  [DataMember,DisplayName("Gerador — potência nominal (kVA)"),Description("Potência aparente nominal do gerador. Zero: dado desconhecido; não é potência ativa do motor térmico.")] public double GeneratorRatedKva {get;set;}
  [DataMember,DisplayName("Gerador — reatância subtransitória Xd'' (%)"),Description("Dado do fabricante, na base da potência e tensão nominais. Não é a impedância percentual total. Zero: desconhecida.")] public double GeneratorXdPercent {get;set;}
  [DataMember,DisplayName("Gerador — resistência por fase (Ω)"),Description("Resistência de sequência positiva na tensão nominal; zero representa aproximação sem resistência. Não se aplica à corrente permanente nem ao regime transitório.")] public double GeneratorROhm {get;set;}
  [Browsable(false)] public bool SupportsConstantImpedanceWaveform {get{return Model==SourceModel.Rede||Model==SourceModel.ImpedanciaDefinida||(Model==SourceModel.UPS&&UPSMode==UPSOperatingMode.Bypass);}}

  [DataMember,DisplayName("Base da corrente de curto"),Description("ReferidaBT: corrente da rede a montante referida à tensão BT. RedeMT: corrente na tensão MT, convertida pela relação nominal do transformador.")] public SourceCurrentBasis CurrentBasis {get;set;}
  [DataMember,DisplayName("Tensão da rede MT (V)"),Description("Utilizada somente na base RedeMT; deve ser maior que a tensão BT. Exige transformador ativado.")] public double UpstreamVoltageV {get;set;}
  string networkReference;
  [DataMember,DisplayName("Fonte dos dados de alimentação")] public string NetworkReference {get{return networkReference??"";}set{networkReference=value;}}
  internal void ValidateNetwork(){
   if(!Enum.IsDefined(typeof(SourceModel),Model))throw new InvalidDataException("Modelo da fonte inválido.");
   if(!Enum.IsDefined(typeof(UPSOperatingMode),UPSMode))throw new InvalidDataException("Modo UPS inválido.");
   foreach(double value in new[]{UPSRatedKva,UPSCurrentLimitKA,UPSCurrentLimitSeconds})Project.Range(value,0,1e9,"dado nominal UPS");
   foreach(double? value in new[]{DefinedPhaseROhm,DefinedPhaseXOhm,DefinedNeutralROhm,DefinedNeutralXOhm,DefinedPEROhm,DefinedPEXOhm})if(value.HasValue)Project.Range(value.Value,0,1e15,"impedância definida");
   if(Model==SourceModel.ImpedanciaDefinida&&DefinedPhaseROhm.HasValue&&DefinedPhaseXOhm.HasValue&&DefinedPhaseROhm.Value==0&&DefinedPhaseXOhm.Value==0)throw new InvalidDataException("Impedância de fase definida não pode ser nula.");
   if((Model==SourceModel.ImpedanciaDefinida||(Model==SourceModel.UPS&&UPSMode==UPSOperatingMode.Inversor))&&(CurrentBasis!=SourceCurrentBasis.ReferidaBT||TransformerKva>0||ShortCircuitKA>0))throw new InvalidDataException("Fonte definida ou UPS inversor: deixe corrente da rede/transformador em zero e base ReferidaBT. Bypass exige modo explicitamente selecionado.");
   Project.Range(GeneratorRatedKva,0,1e9,"potência nominal do gerador");Project.Range(GeneratorXdPercent,0,10000,"reatância subtransitória do gerador");Project.Range(GeneratorROhm,0,1e15,"resistência do gerador");
   if(Model==SourceModel.GeradorSincrono&&(CurrentBasis!=SourceCurrentBasis.ReferidaBT||TransformerKva>0||ShortCircuitKA>0))throw new InvalidDataException("Gerador é fonte única na tensão da rede: deixe corrente da rede e transformador em zero e base ReferidaBT. Não há paralelismo de fontes neste modelo.");
   Project.Range(LineVoltage,1,1000000,"tensão BT");Project.Range(ShortCircuitKA,0,1000000,"corrente de curto da rede");Project.Range(RXRatio,0,1000,"R/X da rede");Project.Range(VoltageFactor,.1,2,"fator de tensão");
   Project.Range(TransformerKva,0,1e9,"potência do transformador");Project.Range(TransformerUkPercent,.01,100,"uk do transformador");Project.Range(TransformerCopperLossKw,0,1e9,"perdas no cobre");
   if(!Enum.IsDefined(typeof(SourceCurrentBasis),CurrentBasis))throw new InvalidDataException("Base da corrente de curto inválida.");
   Project.Range(UpstreamVoltageV,0,1000000,"tensão MT");Project.Text(NetworkReference,"fonte dos dados de alimentação",false);
   if(CurrentBasis==SourceCurrentBasis.RedeMT&&(UpstreamVoltageV<=1000||UpstreamVoltageV<=LineVoltage||LineVoltage>1000||TransformerKva<=0))throw new InvalidDataException("A base RedeMT exige tensão MT acima de 1000 V, tensão BT até 1000 V inferior à MT e transformador ativado.");
  }
 }
 public sealed class SourceImpedance {
  public double? NetworkROhm,NetworkXOhm,TransformerROhm,TransformerXOhm,TotalROhm,TotalXOhm;
  public double? GeneratorROhm,GeneratorXOhm,NeutralROhm,NeutralXOhm,PEROhm,PEXOhm,UPSCurrentLimitKA,UPSCurrentLimitSeconds;
  public string Detail;
 }
 public static class SourceNetwork {
  public static SourceImpedance Calculate(SourceSettings src){
   if(src==null)throw new ArgumentNullException("src");src.ValidateNetwork();
   var value=new SourceImpedance();
   if(src.Model==SourceModel.ImpedanciaDefinida){
    value.NeutralROhm=src.DefinedNeutralROhm;value.NeutralXOhm=src.DefinedNeutralXOhm;value.PEROhm=src.DefinedPEROhm;value.PEXOhm=src.DefinedPEXOhm;
    value.TotalROhm=src.DefinedPhaseROhm.HasValue&&src.DefinedPhaseXOhm.HasValue?src.DefinedPhaseROhm:null;value.TotalXOhm=src.DefinedPhaseROhm.HasValue&&src.DefinedPhaseXOhm.HasValue?src.DefinedPhaseXOhm:null;
    value.Detail="Impedância de fonte equilibrada definida pelo usuário, por fase (sequência positiva). N e PE são dados separados; não são somados à sequência positiva nem tomados como Zs total. Vazio significa desconhecido; zero é valor explícito. Não há modelo de fases assimétricas ou sequência zero derivada automaticamente.";return value;
   }
   if(src.Model==SourceModel.UPS&&src.UPSMode==UPSOperatingMode.Inversor){
    value.UPSCurrentLimitKA=src.UPSCurrentLimitKA>0?(double?)src.UPSCurrentLimitKA:null;value.UPSCurrentLimitSeconds=src.UPSCurrentLimitSeconds>0?(double?)src.UPSCurrentLimitSeconds:null;
    value.Detail="UPS em inversor: fonte limitada em corrente; não converter o limite em impedância de Thévenin nem em Ik mínimo/máximo do circuito. Limite e duração são declarações do fabricante; sem simulação de controle, transição automática para bypass, divisão de corrente entre ramos ou aprovação de seccionamento. Correntes prospectivas por U/Z e forma de onda R-L permanecem pendentes.";return value;
   }
   string prefix=src.Model==SourceModel.UPS?"UPS em bypass declarado: rede a montante, sem limite do inversor. Não se presume transferência automática nem suportabilidade do chaveamento estático. ":"";
   if(src.Model==SourceModel.GeradorSincrono){
    value.Detail="Gerador síncrono como fonte única na tensão nominal. Modelo inicial simétrico subtransitório: Xd''=(xd''/100)*Un²/Sn; Z1=R+jXd''; Ik''=c*Un/(sqrt(3)*|Z1|). Sn em VA, Un em V. Sem fatores de correção de máquina IEC 60909, AVR, decaimento AC, paralelismo ou contribuição de motores; não usar como corrente mínima/permanente de seccionamento.";
    if(src.GeneratorRatedKva<=0||src.GeneratorXdPercent<=0){value.Detail+=" Potência nominal ou Xd'' ausente: corrente de curto pendente.";return value;}
    double gx=(src.GeneratorXdPercent/100)*(src.LineVoltage*src.LineVoltage)/(src.GeneratorRatedKva*1000);
    if(Double.IsNaN(gx)||Double.IsInfinity(gx)||gx<=0||gx>1e15){value.Detail+=" Impedância do gerador fora da faixa numérica: cálculo pendente.";return value;}
    value.GeneratorROhm=src.GeneratorROhm;value.GeneratorXOhm=gx;value.TotalROhm=src.GeneratorROhm;value.TotalXOhm=gx;
    if(src.GeneratorROhm==0)value.Detail+=" R=0: aproximação explicitamente sem resistência.";
    return value;
   }
   if(src.ShortCircuitKA<=0){value.Detail=prefix+"Corrente de curto da rede não informada.";return value;}
   double inputVoltage=src.CurrentBasis==SourceCurrentBasis.RedeMT?src.UpstreamVoltageV:src.LineVoltage;
   double ratio=inputVoltage/src.LineVoltage;
   double z=src.VoltageFactor*inputVoltage/(Math.Sqrt(3)*src.ShortCircuitKA*1000)/(ratio*ratio);
   if(Double.IsNaN(z)||Double.IsInfinity(z)||z<=0||z>1e15){value.Detail=prefix+"Impedância da rede fora da faixa numérica do modelo; cálculo pendente.";return value;}
   double x=z/Math.Sqrt(1+src.RXRatio*src.RXRatio),r=x*src.RXRatio;
   value.NetworkROhm=r;value.NetworkXOhm=x;
   value.Detail=prefix+(src.CurrentBasis==SourceCurrentBasis.RedeMT?"Impedância da rede MT convertida para BT pela relação nominal ao quadrado.":"Corrente da rede a montante já referida à tensão BT.");
   if(src.TransformerKva>0){
    if(src.TransformerCopperLossKw<=0){value.Detail+=" Perdas no cobre desconhecidas: impedância total pendente.";return value;}
    double rated=src.TransformerKva*1000;
    double zt=src.TransformerUkPercent/100*src.LineVoltage*src.LineVoltage/rated;
    double rt=src.TransformerCopperLossKw*1000*src.LineVoltage*src.LineVoltage/(rated*rated);
    if(Double.IsNaN(zt)||Double.IsInfinity(zt)||Double.IsNaN(rt)||Double.IsInfinity(rt)||zt>1e15||rt>1e15){value.Detail+=" Impedância do transformador fora da faixa numérica do modelo; cálculo pendente.";return value;}
    if(rt>zt){value.Detail+=" Perdas no cobre incompatíveis com uk: R do transformador excede |Z|; impedância total pendente.";return value;}
    double xt=Math.Sqrt(Math.Max(0,zt*zt-rt*rt));value.TransformerROhm=rt;value.TransformerXOhm=xt;r+=rt;x+=xt;
    value.Detail+=" Transformador em série, R por perdas nominais e X por uk; sem correção normativa de impedância.";
   }
   value.TotalROhm=r;value.TotalXOhm=x;return value;
  }
 }
}
