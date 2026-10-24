using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace CircuitoStudio
{
    [DataContract] public enum PhaseMode { [EnumMember] Monofasico, [EnumMember] Trifasico }

    [DataContract] public sealed partial class Project
    {
        [DataMember(Order=0)] private int FileVersion = 2;
        List<string> requiredCapabilities;
        [DataMember(Order=35)] private List<string> RequiredCapabilities {get{return requiredCapabilities??new List<string>();}set{requiredCapabilities=value;}}
        List<string> ActualCapabilities(){var result=new List<string>();if(Boards.Any(b=>b.Circuits.Any(c=>c.MotorStarting!=null)))result.Add("motor-start-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.Cable.FactorsSnapshot!=null)))result.Add("cable-factors-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.Protection.BreakerSnapshot!=null)))result.Add("breaker-rating-v1");if(CalculationProfile!=null)result.Add("calculation-profile-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.SelectivityEvidence!=null||c.BackupEvidence!=null)))result.Add("coordination-tables-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.SwitchgearSelection!=null)))result.Add("switchgear-selection-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.ProtectionResponses!=null)))result.Add("protection-responses-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.ResidualSelection!=null)))result.Add("residual-assemblies-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.MotorStarterSelection!=null)))result.Add("motor-starter-assemblies-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.ReturnSizing!=null)))result.Add("return-sizing-v1");if(GroundingSource!=null)result.Add("earthing-conductors-v1");if(LoopSource!=null||Boards.Any(b=>b.Circuits.Any(c=>c.LoopFault!=null)))result.Add("fault-loops-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.ContactTimeStudy!=null)))result.Add("contact-time-v1");if(FlowSettings!=null||Boards.Any(b=>b.Circuits.Any(c=>c.FlowLoad!=null)))result.Add("network-flow-v1");if(Boards.Any(b=>b.EnclosureCatalogSelection!=null||b.Circuits.Any(c=>c.DinFootprint!=null)))result.Add("enclosure-catalog-v1");if(Boards.Any(b=>b.BusbarSelection!=null))result.Add("busbar-assembly-v1");if(Boards.Any(b=>b.Circuits.Any(c=>c.FaultTimeTrace!=null)))result.Add("fault-time-trace-v1");if(FaultExtremes!=null)result.Add("fault-extremes-v1");return result;}
        void ValidateLoadedCapabilities(){var declared=RequiredCapabilities;if(declared.Count>100||declared.Any(x=>x!="motor-start-v1"&&x!="cable-factors-v1"&&x!="breaker-rating-v1"&&x!="calculation-profile-v1"&&x!="coordination-tables-v1"&&x!="switchgear-selection-v1"&&x!="protection-responses-v1"&&x!="residual-assemblies-v1"&&x!="motor-starter-assemblies-v1"&&x!="earthing-conductors-v1"&&x!="return-sizing-v1"&&x!="fault-loops-v1"&&x!="contact-time-v1"&&x!="network-flow-v1"&&x!="fault-extremes-v1"&&x!="fault-time-trace-v1"&&x!="busbar-assembly-v1"&&x!="enclosure-catalog-v1")||declared.Distinct().Count()!=declared.Count)throw new InvalidDataException("O projeto exige capacidades desconhecidas; use uma versão compatível.");var actual=ActualCapabilities();if(actual.Any(x=>!declared.Contains(x))||(actual.Count>0&&FileVersion<4))throw new InvalidDataException("Dados novos exigem formato 4 e declaração de capacidades.");}
        [DataMember(Order=1), DisplayName("Nome"), Category("Projeto")] public string Name { get; set; }
        [DataMember(Order=2), DisplayName("Cliente"), Category("Projeto")] public string Client { get; set; }
        [DataMember(Order=3), DisplayName("Instalação / local"), Category("Projeto")] public string Site { get; set; }
        [DataMember(Order=4), DisplayName("Responsável"), Category("Projeto")] public string Designer { get; set; }
        [DataMember(Order=5), DisplayName("Notas"), Category("Projeto")] public string Notes { get; set; }
        [DataMember(Order=6), DisplayName("Frequência (Hz)"), Category("Alimentação")] public double Frequency { get; set; }
        [DataMember(Order=7), DisplayName("Aterramento"), Category("Alimentação")] public string Earthing { get; set; }
        [DataMember(Order=8), Browsable(false)] public List<Board> Boards { get; set; }

        public Project() { Source=new SourceSettings(); Name="Novo projeto"; Client=Site=Designer=Notes=""; Frequency=60; Earthing="A definir"; Boards=new List<Board>(); }
        public static Project CreateSample()
        {
            Project p=new Project(); p.Name="Projeto exemplo";
            Board b=new Board(); b.Id="Q1"; b.Name="Quadro geral"; b.Voltage=380;
            b.Circuits.Add(new Circuit { Id="C1", Name="Iluminação", Kind="Iluminação", PowerKw=1.2, Voltage=220, BreakerA=10, CableMm2=1.5 });
            b.Circuits.Add(new Circuit { Id="C2", Name="Tomadas", Kind="Tomadas", PowerKw=2.4, Voltage=220, BreakerA=16, CableMm2=2.5 });
            p.Boards.Add(b); return p;
        }
        internal static void Range(double value,double min,double max,string name)
        { if (Double.IsNaN(value)||Double.IsInfinity(value)||value<min||value>max) throw new InvalidDataException("Valor inválido: "+name); }
        internal static void Text(string value,string name,bool required)
        { if(value==null || value.Length>100000 || (required && String.IsNullOrWhiteSpace(value))) throw new InvalidDataException("Texto inválido: "+name); }
        internal static void IdCheck(string id,HashSet<string> ids)
        { Text(id,"identificador",true); if(!ids.Add(id)) throw new InvalidDataException("Identificador duplicado: "+id); }
        public void Validate()
        {
            if(FileVersion!=1 && FileVersion!=2 && FileVersion!=3 && FileVersion!=4) throw new InvalidDataException("Versão de arquivo não suportada.");
            Text(Name,"nome",true); Text(Client,"cliente",false); Text(Site,"local",false); Text(Designer,"responsável",false); Text(Notes,"notas",false); Text(Earthing,"aterramento",false);
            Range(Frequency,1,1000,"frequência");
            ValidateMetadata();if(CalculationProfile!=null)CalculationProfile.Validate();
            if(Boards==null || Boards.Count>10000) throw new InvalidDataException("Lista de quadros inválida.");
            HashSet<string> ids=new HashSet<string>(StringComparer.Ordinal);
            foreach(Board b in Boards) {
                if(b==null) throw new InvalidDataException("Quadro ausente."); IdCheck(b.Id,ids); Text(b.Name,"quadro",true); Range(b.Voltage,1,1000000,"tensão do quadro");
                if(b.Circuits==null || b.Circuits.Count>10000) throw new InvalidDataException("Lista de circuitos inválida.");
                foreach(Circuit c in b.Circuits) {
                    if(c==null) throw new InvalidDataException("Circuito ausente."); IdCheck(c.Id,ids); Text(c.Name,"circuito",true); Text(c.Kind,"tipo",false); Text(c.Notes,"notas",false);
                    if(c.Phases!=PhaseMode.Monofasico && c.Phases!=PhaseMode.Trifasico) throw new InvalidDataException("Número de fases inválido.");
                    Range(c.Voltage,1,1000000,"tensão"); Range(c.PowerKw,0,1000000000,"potência"); Range(c.PowerFactor,0.000001,1,"fator de potência"); Range(c.DemandFactor,0,1,"fator de demanda");
                    if(c.DinFootprint!=null)c.DinFootprint.Validate();if(c.FaultTimeTrace!=null)c.FaultTimeTrace.Validate();if(c.FlowLoad!=null)c.FlowLoad.Validate();if(c.ContactTimeStudy!=null)c.ContactTimeStudy.Validate();if(c.LoopFault!=null)c.LoopFault.Validate();if(c.ReturnSizing!=null)c.ReturnSizing.Validate();if(c.MotorStarterSelection!=null)c.MotorStarterSelection.Validate();if(c.ResidualSelection!=null)c.ResidualSelection.Validate();if(c.ProtectionResponses!=null)c.ProtectionResponses.Validate();if(c.SwitchgearSelection!=null)c.SwitchgearSelection.Validate();Coordination.Validate(c);MotorStarting.ValidateCircuit(c,b);if(c.VoltageDropLimitPercent.HasValue)Range(c.VoltageDropLimitPercent.Value,.01,100,"limite de queda do circuito");
                    Range(c.DiagramX,0,4000,"posição horizontal"); Range(c.DiagramY,0,4000,"posição vertical"); Range(c.LengthM,0,10000000,"comprimento"); Range(c.CableMm2,0,1000000,"seção"); Range(c.BreakerA,0,1000000000,"disjuntor"); Range(c.DesignCurrentA,0,Double.MaxValue,"corrente calculada");
                }
            }
            foreach(var board in Boards){if(board.BusbarSelection!=null)board.BusbarSelection.Validate();if(board.EnclosureCatalogSelection!=null)board.EnclosureCatalogSelection.Validate();}ValidateEngineering();Grounding.Validate(this);if(LoopSource!=null)LoopSource.Validate();if(FlowSettings!=null)FlowSettings.Validate();if(FaultExtremes!=null)FaultExtremes.Validate();
        }
        bool RequiresFormat3 {get{return Source.Model!=SourceModel.Rede||Boards.Any(b=>b.Circuits.Any(c=>c.VoltageDropLimitPercent.HasValue));}}
        public static Project Load(string path)
        {
            Project p;
            using(FileStream s=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read)) {
                if(s.Length>32*1024*1024) throw new InvalidDataException("Arquivo excessivamente grande.");
                p=(Project)new DataContractJsonSerializer(typeof(Project)).ReadObject(s);
            }
            if(p==null) throw new InvalidDataException("Projeto ausente."); p.Validate();p.ValidateLoadedCapabilities();if(p.RequiresFormat3&&p.FileVersion<3)throw new InvalidDataException("Modelo de fonte ou limite por circuito exige formato de projeto 3.");return p;
        }
        public void Save(string path)
        {
            Validate();requiredCapabilities=ActualCapabilities();FileVersion=requiredCapabilities.Count>0?4:RequiresFormat3?3:2; string full=Path.GetFullPath(path); string directory=Path.GetDirectoryName(full);
            string temp=Path.Combine(directory,"."+Path.GetFileName(full)+"."+Guid.NewGuid().ToString("N")+".tmp");
            try {
                using(FileStream s=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)) {
                    new DataContractJsonSerializer(typeof(Project)).WriteObject(s,this);if(s.Length>32*1024*1024)throw new InvalidDataException("O projeto excede 32 MiB. O arquivo anterior foi preservado; reduza os dados anexados antes de salvar.");s.Flush(true);
                }
                if(File.Exists(full)) File.Replace(temp,full,full+".bak"); else File.Move(temp,full);
            } finally { if(File.Exists(temp)) File.Delete(temp); }
        }
    }
    [DataContract] public sealed partial class Board
    {
        [DataMember(Order=0), Browsable(false)] public string Id {get;set;}
        [DataMember(Order=1), DisplayName("Nome"), Category("Quadro")] public string Name {get;set;}
        [DataMember(Order=2), DisplayName("Tensão (V)"), Category("Quadro")] public double Voltage {get;set;}
        [DataMember(Order=3), Browsable(false)] public List<Circuit> Circuits {get;set;}
        public Board() { Id=Guid.NewGuid().ToString("N"); Name="Novo quadro"; Voltage=220; Circuits=new List<Circuit>(); }
        public override string ToString() {return Name;}
    }
    [DataContract] public sealed partial class Circuit
    {
        [DataMember(Order=0), Browsable(false)] public string Id {get;set;}
        [DataMember(Order=1), DisplayName("Nome"), Category("Circuito")] public string Name {get;set;}
        [DataMember(Order=2), DisplayName("Tipo"), Category("Circuito")] public string Kind {get;set;}
        [DataMember(Order=3), DisplayName("Fases"), Category("Carga")] public PhaseMode Phases {get;set;}
        [DataMember(Order=4), DisplayName("Tensão (V)"), Description("Em trifásico, tensão entre fases."), Category("Carga")] public double Voltage {get;set;}
        [DataMember(Order=5), DisplayName("Potência ativa de entrada (kW)"), Category("Carga"), Description("Para motor cadastrado, altere eixo e rendimento em Ferramentas → Motor; esta potência é derivada desses dados.")] public double PowerKw {get;set;}
        [DataMember(Order=6), DisplayName("Fator de potência"), Category("Carga")] public double PowerFactor {get;set;}
        [DataMember(Order=7), DisplayName("Fator de demanda"), Category("Carga")] public double DemandFactor {get;set;}
        [DataMember(Order=8), DisplayName("Comprimento (m)"), Category("Dados manuais")] public double LengthM {get;set;}
        [DataMember(Order=9), DisplayName("Seção (mm²)"), Category("Dados manuais")] public double CableMm2 {get;set;}
        [DataMember(Order=10), DisplayName("Disjuntor (A)"), Category("Dados manuais")] public double BreakerA {get;set;}
        [DataMember(Order=11), DisplayName("Notas"), Category("Circuito")] public string Notes {get;set;}
        [DataMember(Order=82), Category("Carga"), DisplayName("Travar fase no balanceamento")] public bool PhaseLocked {get;set;}
        [DataMember(Order=80), Category("Unifilar"), DisplayName("Posição X (0 automática)")] public double DiagramX {get;set;}
        [DataMember(Order=81), Category("Unifilar"), DisplayName("Posição Y (0 automática)")] public double DiagramY {get;set;}
        [Browsable(false)] public double DemandKw {get{return PowerKw*DemandFactor;}}
        // Balanced sinusoidal loads; PowerKw is electrical active input, without efficiency adjustment.
        [Browsable(false)] public double DesignCurrentA {get{return DemandKw*1000/(Voltage*PowerFactor*(Phases==PhaseMode.Trifasico?Math.Sqrt(3):1));}}
        public Circuit() { ChildBoardId=""; PhaseLabel="L1"; Cable=new CableData(); Protection=new ProtectionData(); Id=Guid.NewGuid().ToString("N"); Name="Novo circuito"; Kind="Carga"; Voltage=220; PowerFactor=1; DemandFactor=1; LengthM=10; Notes=""; }
        public override string ToString() {return Name;}
    }
}
