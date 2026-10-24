using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using CircuitoStudio;
internal static class FaultWaveformTests {
 static int n;
 static void Check(bool value,string label){n++;if(!value)throw new Exception(label);}
 static double Number(string s){return Double.Parse(s,CultureInfo.InvariantCulture);}
 static void Near(double a,double b,string label){Check(Math.Abs(a-b)<=1e-10*Math.Max(1,Math.Abs(b)),label);}
 static void Reject(Action action,string label){bool failed=false;try{action();}catch(InvalidDataException){failed=true;}Check(failed,label);}
 [STAThread] public static int Main(string[] args){if(args.Length==2&&args[0]=="--benchmark-dir"){Directory.CreateDirectory(args[1]);FaultWaveform.ExportCsv(FaultDynamics.Calculate(.1,1,50,10),.1,30,Path.Combine(args[1],"rl-50hz.csv"));FaultWaveform.ExportCsv(FaultDynamics.Calculate(.03,.6,60,7),.1,85,Path.Combine(args[1],"rl-60hz.csv"));FaultWaveform.ExportCsv(FaultDynamics.Calculate(0,1,50,10),.1,0,Path.Combine(args[1],"l-50hz.csv"));return 0;}string directory=Path.Combine(Path.GetTempPath(),"wave-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);try{
  var model=FaultDynamics.Calculate(.1,1,50,10);string path=Path.Combine(directory,"wave.csv");FaultWaveform.ExportCsv(model,.1,30,path);string[] rows=File.ReadAllLines(path);
  Check(rows[0]==FaultWaveform.Header,"explicit unit-bearing header");Check(rows.Length==502,"100 samples per cycle plus endpoints");Check(rows.Skip(1).All(row=>row.Split(';').Length==11),"fixed columns for every point");var first=rows[1].Split(';');Near(Number(first[0]),0,"initial time");Near(Number(first[1]),0,"initial R-L current");var last=rows.Last().Split(';');Near(Number(last[0]),.1,"exact final time");Near(Number(last[1]),model.InstantaneousKA(.1,30),"CSV waveform matches model");Near(Number(last[2]),model.DcEnvelopeKA(.1),"CSV DC envelope matches model");Near(Number(last[6]),30,"closing angle recorded");Near(Number(last[9]),model.TimeConstantSeconds.Value,"tau units seconds");Check(last[10]=="RL_RADIAL_SEM_LIMITACAO","explicit model identifier");
  string previous=File.ReadAllText(path);Reject(delegate{FaultWaveform.ExportCsv(model,0,0,path);},"invalid duration rejected");Check(File.ReadAllText(path)==previous,"invalid duration preserves output");Reject(delegate{FaultWaveform.ExportCsv(model,.1,Double.NaN,path);},"NaN angle rejected");Check(File.ReadAllText(path)==previous&&Directory.GetFiles(directory,"*.tmp").Length==0,"invalid angle preserves output without temporary leak");
  FaultWaveform.ExportCsv(FaultDynamics.Calculate(0,1,50,10),.01,0,path);Check(File.ReadAllLines(path).Last().Split(';')[9]=="","undamped case has empty finite tau field");
  Near(FaultWaveform.Intervals(FaultDynamics.Calculate(1,1,1000,10),5),50000,"bounded export and chart sampling");Reject(delegate{FaultWaveform.Intervals(null,.1);},"unknown dynamics rejected");
  var p=Project.CreateSample();p.Source.ShortCircuitKA=10;var b=p.Boards[0];var c=b.Circuits[0];string snapshot=Path.Combine(directory,"before.circuito");p.Save(snapshot);string before=File.ReadAllText(snapshot);
  using(var form=new FaultWaveformForm(p,b,c)){form.CreateControl();Check(form.SelectedModel!=null,"origin bus dynamics available");var controls=form.Controls.OfType<FlowLayoutPanel>().Single();var location=controls.Controls.OfType<ComboBox>().Single();location.SelectedIndex=1;Check(form.SelectedModel==null,"single-phase end has no invented 3F waveform");location.SelectedIndex=0;Check(form.SelectedModel!=null,"origin can be selected again");}
  c.Phases=PhaseMode.Trifasico;c.Voltage=380;using(var form=new FaultWaveformForm(p,b,c)){form.CreateControl();var controls=form.Controls.OfType<FlowLayoutPanel>().Single();controls.Controls.OfType<ComboBox>().Single().SelectedIndex=1;Check(form.SelectedModel!=null,"three-phase end uses cable impedance");var panel=form.Controls.OfType<Panel>().Where(x=>!(x is FlowLayoutPanel)).Single();panel.Size=new Size(700,350);using(var bitmap=new Bitmap(700,350))panel.DrawToBitmap(bitmap,new Rectangle(0,0,700,350));Check(true,"chart draws without displaying a desktop window");}
  c.Phases=PhaseMode.Monofasico;c.Voltage=220;p.Save(snapshot);Check(File.ReadAllText(snapshot)==before,"waveform viewing does not mutate project");
  Console.WriteLine("PASS: "+n+" waveform assertions, including off-screen rendering");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{Directory.Delete(directory,true);}}
}
