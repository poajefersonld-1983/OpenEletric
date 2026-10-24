using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using CircuitoStudio;
internal static class CatalogSearchTests {
 static int n;static void Check(bool value,string label){n++;if(!value)throw new Exception(label);}
 static void Reject(Action action,string label){bool failed=false;try{action();}catch(InvalidDataException){failed=true;}Check(failed,label);}
 static Equipment Item(){return new Equipment{Manufacturer="Fábrica Ω",Reference="REF-01",Description="Disjuntor de ensaio",Category="Disjuntor",RatedA=16,BreakingKA=10,TripI2A=23.2,Source="Manual sintético, p. 12",Family="Família Norte",Model="Modelo A",DataVersion="2026.10",DataSheet="Ficha técnica ç 北; revisão 2"};}
 static void Snapshot(CatalogForm form,Size size,string path){
  if(Environment.GetEnvironmentVariable("CIRCUITO_QA_VISUAL")!="1")return;
  // Opt-in only: the Linux runner uses an isolated X display, never the desktop.
  form.StartPosition=FormStartPosition.Manual;form.Location=new Point(10,10);form.ShowInTaskbar=false;form.Size=size;
  form.Show();Application.DoEvents();form.PerformLayout();Application.DoEvents();
  Check(form.Visible&&form.Controls.Cast<Control>().All(child=>child.Visible),"visual snapshot controls visible");
  Check(form.Controls.OfType<FlowLayoutPanel>().All(bar=>bar.Controls.Cast<Control>().All(control=>control.Bottom+control.Margin.Bottom<=bar.ClientSize.Height-bar.Padding.Bottom)),"visual action/filter rows fit");
  using(var bitmap=new Bitmap(form.ClientSize.Width,form.ClientSize.Height)){
   using(var graphics=Graphics.FromImage(bitmap))graphics.CopyFromScreen(form.PointToScreen(Point.Empty),Point.Empty,form.ClientSize);
   var colors=new System.Collections.Generic.HashSet<int>();
   for(int y=0;y<bitmap.Height;y+=3)for(int x=0;x<bitmap.Width;x+=3)colors.Add(bitmap.GetPixel(x,y).ToArgb());
   bitmap.Save(path);Check(colors.Count>16,"visual capture contains text and rendered controls");
  }
  form.Hide();
 }
 static void BoundaryTests(string directory,CatalogData data,Circuit circuit,Equipment item){
  string path=Path.Combine(directory,"size-limit.json");data.Save(path);
  using(var stream=new FileStream(path,FileMode.Open,FileAccess.Write)){
   stream.Position=stream.Length;byte[] spaces=Enumerable.Repeat((byte)' ',65536).ToArray();
   while(stream.Length<32L*1024*1024){int count=(int)Math.Min(spaces.Length,32L*1024*1024-stream.Length);stream.Write(spaces,0,count);}
  }
  Check(CatalogData.Load(path).Items.Count==data.Items.Count,"library of exactly 32 MiB is readable");
  using(var stream=new FileStream(path,FileMode.Append))stream.WriteByte((byte)' ');
  Reject(delegate{CatalogData.Load(path);},"library exceeding 32 MiB rejected before deserialization");
  data.Save(path);data.Save(path);byte[] before=File.ReadAllBytes(path),backup=File.ReadAllBytes(path+".bak");
  var huge=new CatalogData();string large=new string('z',2000);
  for(int i=0;i<2300;i++)huge.Items.Add(new Equipment{Reference="SIZE-"+i,Manufacturer=large,Description=large,Category=large,Source=large,Family=large,Model=large,DataVersion=large,DataSheet=large});
  Reject(delegate{huge.Save(path);},"serialized library exceeding 32 MiB rejected");
  Check(File.ReadAllBytes(path).SequenceEqual(before)&&File.ReadAllBytes(path+".bak").SequenceEqual(backup),"oversized save preserves library and existing backup");
  Check(Directory.GetFiles(directory,"size-limit.json.*.tmp").Length==0,"oversized save removes temporary file");
  circuit.Notes="";CatalogApplication.Apply(circuit,item);int recordLength=circuit.Notes.Length;
  circuit.Notes=new string('x',100000-recordLength-2);CatalogApplication.Apply(circuit,item);
  Check(circuit.Notes.Length==100000,"resulting notes of exactly 100000 characters accepted");
  circuit.Notes=new string('x',100001-recordLength-2);string notes=circuit.Notes;double current=circuit.BreakerA,capacity=circuit.Protection.BreakingCapacityKA;
  Reject(delegate{CatalogApplication.Apply(circuit,item);},"resulting notes one character over limit rejected");
  Check(circuit.Notes==notes&&circuit.BreakerA==current&&circuit.Protection.BreakingCapacityKA==capacity,"notes overflow preserves prior values atomically");
 }
 [STAThread] public static int Main(){string directory=Path.Combine(Path.GetTempPath(),"catalog-search-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);try{
  var data=new CatalogData();var item=Item();data.Items.Add(item);data.Items.Add(new Equipment{Manufacturer="Outra fábrica",Reference="REF-02",Family="Família Sul",Model="Modelo B",Source="Outra fonte",DataVersion="2025"});
  Check(CatalogSearch.Filter(data,new CatalogQuery()).Count==2,"empty filters show all");Check(CatalogSearch.Filter(data,new CatalogQuery{Manufacturer=" FÁBRICA Ω ",Family="norte",Model="modelo a"}).Single()==item,"manufacturer family model filters combine ignoring case");Check(CatalogSearch.Filter(data,new CatalogQuery{Manufacturer="Outra",Family="Norte"}).Count==0,"filters combine with AND");Check(CatalogSearch.Filter(data,new CatalogQuery{Text="2026.10"}).Single()==item,"general search includes data version");Check(CatalogSearch.Filter(data,new CatalogQuery{Text="北"}).Single()==item,"general search includes Unicode datasheet");Check(CatalogSearch.Filter(data,new CatalogQuery{Text="Manual sintético"}).Single()==item,"general search includes source");
  var copy=item.Copy();copy.Family="Outra";Check(item.Family=="Família Norte"&&copy.DataVersion==item.DataVersion,"copy preserves metadata independently");
  foreach(string extension in new[]{"csv","xlsx"}){string path=Path.Combine(directory,"metadata."+extension);if(extension=="csv")CatalogData.ExportCsv(data,path);else CatalogData.ExportXlsx(data,path);var loaded=CatalogData.ImportTable(path);var e=loaded.Items[0];Check(e.Family==item.Family&&e.Model==item.Model&&e.DataVersion==item.DataVersion&&e.DataSheet==item.DataSheet,"extended "+extension+" retains all metadata");}
  string json=Path.Combine(directory,"catalogo.json");data.Save(json);Check(CatalogData.Load(json).Items[0].DataSheet==item.DataSheet,"library JSON preserves metadata");item.DataVersion="2026.11";data.Save(json);Check(CatalogData.Load(json+".bak").Items[0].DataVersion=="2026.10","library backup preserves previous version");
  string legacy=File.ReadAllText(json);foreach(string field in new[]{"Family","Model","DataVersion","DataSheet"})legacy=Regex.Replace(legacy,"\""+field+"\":\"(?:[^\"\\\\]|\\\\.)*\",","");File.WriteAllText(json,legacy,Encoding.UTF8);var old=CatalogData.Load(json);Check(old.Items[0].Family==""&&old.Items[0].Model==""&&old.Items[0].DataVersion==""&&old.Items[0].DataSheet=="","old library metadata defaults empty");
  string legacyCsv=Path.Combine(directory,"legacy.csv");File.WriteAllText(legacyCsv,CatalogData.Header+"\nLegado;R;Descrição;Proteção;12.5;6;18;;;;Manual\n",Encoding.UTF8);var imported=CatalogData.ImportCsv(legacyCsv);Check(imported.Items[0].RatedA==12.5&&imported.Items[0].Family=="","11-column CSV remains supported");
  string invalid=Path.Combine(directory,"bad.csv");File.WriteAllText(invalid,CatalogData.ExtendedHeader+"\nA;B;C;D;1;2;3;4;5;6;S;F;M;V\n");Reject(delegate{CatalogData.ImportCsv(invalid);},"incomplete extended row rejected");
  var p=Project.CreateSample();p.Source.ShortCircuitKA=6;var circuit=p.Boards[0].Circuits[0];circuit.PowerKw=2.2;circuit.Voltage=220;circuit.Cable.AmpacityA=20;var context=Engineering.Analyze(p).For(circuit);
  string detail=CatalogSearch.Describe(item,new CatalogQuery{Family="Norte"},context);Check(detail.Contains(item.DataSheet)&&detail.Contains("2026.11")&&detail.Contains(item.Source),"detail includes datasheet source version");Check(detail.Contains("Atende nominalmente: Ib ≤ In")&&detail.Contains("Atende nominalmente: In ≤ Iz"),"nominal current and ampacity comparisons explained");Check(detail.Contains("não recebe aprovação global"),"pre-analysis does not claim global approval");item.RatedA=5;Check(CatalogSearch.Describe(item,new CatalogQuery(),context).Contains("Não atende: Ib ≤ In"),"undersized nominal current rejected in explanation");item.RatedA=25;Check(CatalogSearch.Describe(item,new CatalogQuery(),context).Contains("Não atende: In ≤ Iz"),"oversized device against cable explained");item.RatedA=16;item.BreakingKA=3;Check(CatalogSearch.Describe(item,new CatalogQuery(),context).Contains("Não atende: Interrupção"),"insufficient interruption comparison explained");item.BreakingKA=0;Check(CatalogSearch.Describe(item,new CatalogQuery(),context).Contains("Pendente: Interrupção"),"unknown interruption stays pending");item.BreakingKA=10;Check(CatalogSearch.Describe(item,new CatalogQuery{Family="Ausente"},context).Contains("não corresponde ao filtro"),"explanation does not invent filter match");Check(CatalogSearch.Describe(item,new CatalogQuery(),null).Contains("Sem circuito selecionado"),"library can be browsed without circuit");
  circuit.Notes="Nota original preservada ";CatalogApplication.Apply(circuit,item);Check(circuit.BreakerA==16&&circuit.Protection.BreakingCapacityKA==10&&circuit.Protection.ConventionalTripA==23.2,"apply copies nominal values");Check(circuit.Notes.StartsWith("Nota original preservada ")&&circuit.Notes.Contains(item.DataVersion)&&circuit.Notes.Contains(item.DataSheet),"apply preserves original notes and snapshots metadata");item.DataVersion="Alterada depois";Check(!circuit.Notes.Contains(item.DataVersion),"project reference is independent of later library edits");string project=Path.Combine(directory,"project.circuito");p.Save(project);var reopened=Project.Load(project);Check(reopened.Boards[0].Circuits[0].Notes==circuit.Notes,"project metadata reference survives reopen");Check(EngineeringReport.Memory(p,Engineering.Analyze(p)).Contains(item.DataSheet),"technical report includes equipment reference notes");
  double previous=circuit.BreakerA;string previousNotes=circuit.Notes;item.RatedA=1e10;Reject(delegate{CatalogApplication.Apply(circuit,item);},"out-of-model current rejected before mutation");Check(circuit.BreakerA==previous&&circuit.Notes==previousNotes,"invalid application leaves previous selection intact");item.RatedA=16;circuit.Notes=new string('x',100000);Reject(delegate{CatalogApplication.Apply(circuit,item);},"oversized resulting notes rejected before mutation");Check(circuit.Notes.Length==100000&&circuit.BreakerA==previous,"oversized notes preserve circuit");circuit.Notes=previousNotes;
  using(var form=new CatalogForm(json,p,circuit)){form.CreateControl();form.ApplyFilters("Fábrica Ω","","","");Check(form.VisibleItemCount==1,"browser applies manufacturer filter");form.ApplyFilters("","Ausente","","");Check(form.VisibleItemCount==0,"browser handles empty result");form.ApplyFilters("","","","");form.Size=form.MinimumSize;form.PerformLayout();Check(form.Controls.OfType<FlowLayoutPanel>().All(bar=>bar.Controls.Cast<Control>().All(control=>control.Bottom+control.Margin.Bottom<=bar.ClientSize.Height-bar.Padding.Bottom)),"filter and action rows fit minimum window");}
  byte[] original=File.ReadAllBytes(json);string migrated=Path.Combine(directory,"catalogo-v2.json");using(var form=new CatalogForm(migrated)){form.CreateControl();Check(form.VisibleItemCount==2,"new library reads old path as migration source");}Check(File.ReadAllBytes(json).SequenceEqual(original)&&!File.Exists(migrated),"opening migration does not overwrite legacy library");
  string newLibrary=Path.Combine(directory,"metadata.json");data.Save(newLibrary);using(var form=new CatalogForm(newLibrary,p,circuit)){form.CreateControl();form.ApplyFilters("","Norte","Modelo A","");string artifacts=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"referencias-catalogo");Directory.CreateDirectory(artifacts);Snapshot(form,new Size(1200,820),Path.Combine(artifacts,"catalogo.png"));Snapshot(form,form.MinimumSize,Path.Combine(artifacts,"catalogo-minimo.png"));form.ApplyFilters("","Ausente","","");Snapshot(form,form.MinimumSize,Path.Combine(artifacts,"catalogo-vazio.png"));Check(form.ItemDetails.Contains("Nenhum item selecionado"),"empty browser clears previous details");form.ApplyFilters("","Norte","Modelo A","");Check(form.VisibleItemCount==1,"family and model visible in browser");}
  BoundaryTests(directory,data,circuit,item);
  Console.WriteLine("PASS: "+n+" catalog-search assertions including migration and off-screen layout");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{Directory.Delete(directory,true);}}
}
