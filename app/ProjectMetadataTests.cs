using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CircuitoStudio;
class ProjectMetadataTests {
 static int n;static void Check(bool b,string text){n++;if(!b)throw new Exception(text);}
 static void Reject(Action action){try{action();}catch(InvalidDataException){n++;return;}throw new Exception("Invalid description accepted");}
 static BindingFlags flags=BindingFlags.Instance|BindingFlags.NonPublic;
 static void SetText(object target,string field,string text){((TextBox)target.GetType().GetField(field,flags).GetValue(target)).Text=text;}
 [STAThread] static void Main(){string dir=Path.Combine(Path.GetTempPath(),"metadata-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);try{
  var p=Project.CreateSample();p.Name="Instalação elétrica Ω";p.Client="Cliente João";p.Site="Fábrica 北";p.Designer="Engenheira Ç";p.Notes="Observação independente";p.DescriptionLine1=" Linha 1: <Q> & área ";p.DescriptionLine2="Linha 2: descrição çã";p.DescriptionLine3="=2+2";string file=Path.Combine(dir,"metadata.circuito");p.Save(file);var loaded=Project.Load(file);Check(loaded.Name==p.Name&&loaded.Client==p.Client&&loaded.Site==p.Site&&loaded.Designer==p.Designer&&loaded.Notes==p.Notes,"All basic project fields preserve Unicode");Check(loaded.DescriptionRows.SequenceEqual(p.DescriptionRows),"Three descriptions and surrounding spaces preserved");
  p.DescriptionLine2="";p.Save(file);loaded=Project.Load(file);Check(loaded.DescriptionLine2==""&&loaded.DescriptionLine3=="=2+2","Empty middle row remains distinct");Check(Project.Load(file+".bak").DescriptionLine2=="Linha 2: descrição çã","Backup retains previous descriptions");
  string html=Path.Combine(dir,"metadata.html"),csv=Path.Combine(dir,"metadata.csv"),rtf=Path.Combine(dir,"metadata.rtf");EngineeringReport.Html(p,html);Exports.Csv(p,csv);ReportPrinting.SaveRtf(p,rtf);string htmlText=File.ReadAllText(html),csvText=File.ReadAllText(csv);Check(htmlText.Contains("&lt;Q&gt; &amp;")&&!htmlText.Contains("<Q>"),"Description escaped in HTML");Check(htmlText.Contains("Linha 1")&&htmlText.Contains("=2+2"),"All descriptions exported");Check(csvText.Contains("Descrição — linha 2\";\"\"")&&csvText.Contains("\"'=2+2\""),"CSV retains blank row and neutralizes formulas");string memory=EngineeringReport.Memory(p,Engineering.Analyze(p));Check(memory.Contains("Instalação / local: Fábrica 北")&&memory.Contains("Descrição — linha 3: =2+2"),"Metadata included in calculation memory");using(var rich=new RichTextBox()){rich.Rtf=File.ReadAllText(rtf);Check(rich.Text.Contains(p.DescriptionLine1)&&rich.Text.Contains(p.Site),"RTF preserves metadata Unicode");}
  byte[] before=File.ReadAllBytes(file);p.DescriptionLine1=new string('x',501);Reject(delegate{p.Save(file);});Check(File.ReadAllBytes(file).SequenceEqual(before),"Invalid metadata save preserves original file");p.DescriptionLine1="a\nb";Reject(delegate{p.Validate();});p.DescriptionLine1="";p.DescriptionLine3="";string legacy=Regex.Replace(File.ReadAllText(file),",\"DescriptionLine[123]\":\"(?:[^\"\\\\]|\\\\.)*\"","");File.WriteAllText(file,legacy);var old=Project.Load(file);Check(old.DescriptionRows.All(s=>s==""),"Files without description fields remain supported");
  using(var wizard=new ProjectWizard()){wizard.CreateControl();SetText(wizard,"name","Projeto assistente Ω");SetText(wizard,"client","Cliente assistente ç");SetText(wizard,"site","Obra assistente 北");SetText(wizard,"designer","Responsável");SetText(wizard,"description1","Texto 1");SetText(wizard,"description2","");SetText(wizard,"description3","Texto 3");Check((bool)wizard.GetType().GetMethod("CommitStep",flags).Invoke(wizard,null),"Wizard accepts project data");wizard.GetType().GetMethod("BuildResult",flags).Invoke(wizard,null);Check(wizard.Result.DescriptionRows.SequenceEqual(new[]{"Texto 1","","Texto 3"})&&wizard.Result.Client=="Cliente assistente ç"&&wizard.Result.Site=="Obra assistente 北","Wizard builds all metadata with empty middle row");}
  Console.WriteLine("PASS: "+n+" project metadata assertions");
 }finally{Directory.Delete(dir,true);}}
}
