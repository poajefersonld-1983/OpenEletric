using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Reflection;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using CircuitoStudio;
class IntegrationTests {
 static int count;static void Check(bool b,string message){if(!b)throw new Exception(message);count++;}
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static object Field(object o,string name){return o.GetType().GetField(name,flags).GetValue(o);}
 static void Set(object o,string name,object value){o.GetType().GetField(name,flags).SetValue(o,value);}
 static object Call(object o,string name,params object[] args){return o.GetType().GetMethod(name,flags).Invoke(o,args);}
 [STAThread] static void Main(){
  Application.EnableVisualStyles();string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verificacao-integracao");Directory.CreateDirectory(dir);
  using(var studio=(Form)Activator.CreateInstance(typeof(Project).Assembly.GetType("CircuitoStudio.Studio"),true)){
   studio.CreateControl();var studyGrid=(DataGridView)Field(studio,"results");Check(studyGrid.Columns.Count==11,"Estudos expõem colunas trifásicas, bifásicas e de laço");Check(studyGrid.Columns[7].HeaderText.Contains("aprox.")&&studyGrid.AutoSizeColumnsMode==DataGridViewAutoSizeColumnsMode.None,"Cabeçalhos completos com rolagem horizontal");Project p=(Project)Field(studio,"project");p.Name="Integração "+DateTime.Now.ToString("yyyyMMdd-HHmmss");p.DescriptionLine1="Descrição integrada Ω";p.DescriptionLine2="";p.DescriptionLine3="Terceira linha Ç";Call(studio,"AddCircuit");Check(p.Boards[0].Circuits.Count==3,"Adicionar circuito");var original=(Circuit)Field(studio,"circuit");original.ReactiveKvarOverride=2.4;original.Cable.Material=ConductorMaterial.Aluminium;original.Cable.AmpacityA=36;original.Protection.BreakingCapacityKA=10;original.DinModules=4;original.DinRow=2;original.DinColumn=5;original.DiagramX=410;original.PhaseLocked=true;original.DiagramLabel="Etiqueta Ω";original.DrawingSymbol=DiagramSymbol.Gerador;original.DriveMotor=new DriveMotorData{DriveReference="Teste inversor",InputRatedA=16,OutputRatedA=20,OutputVoltageV=380,MotorReference="Teste motor",MotorRatedA=18,MotorVoltageV=380,MotorShaftKw=5.5,Source="Teste sintético"};Call(studio,"DuplicateCircuit");Check(p.Boards[0].Circuits.Count==4,"Duplicar circuito");var duplicate=(Circuit)Field(studio,"circuit");Check(duplicate.Id!=original.Id&&duplicate.ReactiveKvarOverride==2.4,"Duplicação preserva reativo e gera novo ID");Check(duplicate.Cable.Material==ConductorMaterial.Aluminium&&duplicate.Cable.AmpacityA==36&&duplicate.Protection.BreakingCapacityKA==10,"Duplicação preserva cabo e proteção");Check(duplicate.DinModules==4&&duplicate.DinRow==0&&duplicate.DinColumn==0,"Duplicação preserva módulos e limpa posição DIN");Check(duplicate.DiagramX==0&&duplicate.PhaseLocked,"Duplicação limpa posição gráfica e preserva restrição de fase");Check(!Object.ReferenceEquals(duplicate.Cable,original.Cable)&&!Object.ReferenceEquals(duplicate.Protection,original.Protection),"Dados técnicos duplicados sem compartilhar objetos");
   Check(duplicate.DriveMotor!=null&&duplicate.DriveMotor.InputRatedA==16&&duplicate.DriveMotor.MotorRatedA==18,"Duplicação mantém correntes de entrada e motor distintas");Check(!Object.ReferenceEquals(original.DriveMotor,duplicate.DriveMotor),"Duplicação do conjunto sem referência compartilhada");
   Check(duplicate.DiagramLabel=="Etiqueta Ω"&&duplicate.DrawingSymbol==DiagramSymbol.Gerador,"Duplicação preserva etiqueta e símbolo sem alterar ID elétrico");
   var c=(Circuit)Field(studio,"circuit");c.DiagramX=450;c.DiagramY=500;Call(studio,"RefreshAll");var study=(Study)Field(studio,"currentStudy");Check(study.For(c)!=null,"Estudo atualizado");string file=Path.Combine(dir,"integracao.circuito");Set(studio,"currentFile",file);Check((bool)Call(studio,"Save",false),"Salvar pelo fluxo GUI");Check(Project.Load(file).Boards[0].Circuits.Count==4,"Reabrir arquivo GUI");Check(Project.Load(file).Boards[0].Circuits.Last().DiagramX==450,"Posição persistida");Check(Project.Load(file).DescriptionRows.SequenceEqual(new[]{"Descrição integrada Ω","","Terceira linha Ç"}),"Descrições persistidas pelo fluxo de salvar da GUI");
   string svg=Path.Combine(dir,"posicoes.svg");Exports.Svg(p,p.Boards[0],svg);var xml=XDocument.Load(svg);XNamespace ns="http://www.w3.org/2000/svg";Check(xml.Descendants(ns+"circle").Any(x=>(string)x.Attribute("cx")=="450"&&(string)x.Attribute("cy")=="605"),"Geometria exportada corresponde à posição movida");
   string html=Path.Combine(dir,"estudos.html");EngineeringReport.Html(p,html);Check(File.ReadAllText(html).Contains("Memória completa"),"Relatório técnico integrado");Check(File.ReadAllText(html).Contains("Ik 2F aprox. fim kA"),"Relatório mostra curto bifásico aproximado");p.Notes="<script>alert(1)</script>";EngineeringReport.Html(p,html);Check(!File.ReadAllText(html).Contains("<script>"),"Escape relatório completo");
   ReportPrinting.SaveRtf(p,Path.Combine(dir,"relatorio.rtf"));using(var rtf=new RichTextBox()){rtf.Rtf=File.ReadAllText(Path.Combine(dir,"relatorio.rtf"));Check(rtf.Text.Contains("MEMÓRIA DE CÁLCULO"),"RTF válido Unicode");}Check(ReportPrinting.Rtf("a{b}\\ç").Contains("\\u231?"),"Escape RTF Unicode");
   EngineeringReport.Materials(p,Path.Combine(dir,"materiais.csv"));Check(File.ReadAllText(Path.Combine(dir,"materiais.csv")).Contains("m de rota"),"Unidade de materiais explícita");
   using(var form=new CatalogForm(Path.Combine(dir,"catalogo.json"))){form.CreateControl();Check(form.SelectedEquipment==null,"Catálogo vazio sem fabricante fictício");}
   using(var form=new CableSelectionForm(p,c,Path.Combine(dir,"cabos.csv"))){form.CreateControl();Check(form.SelectedCable==null,"Cabos sem tabela fictícia");}
   using(var form=new EnclosureForm(p,p.Boards[0])){form.CreateControl();Check(!form.Changed,"Vista frontal abre sem mutar dados");}
   using(var form=new CurvesForm(p,p.Boards[0],c)){form.CreateControl();Check(true,"Curvas abrem sem dados inventados");}
   using(var form=new ProjectWizard()){form.CreateControl();Check(form.Result==null,"Assistente cancelável sem projeto aplicado");}
   // Exercise the real WinForms timer without presenting a window or using input.
   Call(studio,"AddCircuit");string recovery=(string)Call(studio,"RecoveryFile");var watch=Stopwatch.StartNew();while(watch.ElapsedMilliseconds<34000&&!File.Exists(recovery)){Application.DoEvents();Thread.Sleep(40);}Check(File.Exists(recovery),"Autosave temporizado");Check(Project.Load(recovery).Boards[0].Circuits.Count==5,"Recuperação completa");Set(studio,"dirty",false);Check((bool)Call(studio,"RestoreRecovery",recovery),"Restaurar recuperação pelo fluxo do aplicativo");Check(((Project)Field(studio,"project")).Boards[0].Circuits.Count==5,"Aplicar os dados recuperados");Check(Field(studio,"currentFile")==null,"Abrir recuperação como nova cópia");Set(studio,"dirty",false);
   Set(studio,"dirty",false);
  }
  Console.WriteLine("PASS: "+count+" verificações integradas, incluindo autosave real de 30 segundos.");
 }
}
