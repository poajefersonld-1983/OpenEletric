using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CircuitoStudio;
internal static class SourceNetworkTests {
 static int count;
 static void Check(bool pass,string label){count++;if(!pass)throw new Exception(label);}
 static void Near(double actual,double expected,string label){Check(Math.Abs(actual-expected)<=1e-10*Math.Max(1,Math.Abs(expected)),label);}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected,label);}
 static Project Fixture(){var p=Project.CreateSample();p.Source.LineVoltage=400;p.Source.CurrentBasis=SourceCurrentBasis.RedeMT;p.Source.UpstreamVoltageV=20000;p.Source.ShortCircuitKA=12.5;p.Source.RXRatio=0;p.Source.TransformerKva=630;p.Source.TransformerUkPercent=6;p.Source.TransformerCopperLossKw=6.3;p.Source.NetworkReference="Ensaio sintético MT/BT Ω";p.Boards[0].Voltage=400;foreach(var c in p.Boards[0].Circuits)c.Voltage=230.9;return p;}
 [STAThread] public static int Main(){string directory=Path.Combine(Path.GetTempPath(),"source-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);try{
  var p=Fixture();var source=p.Source;var z=SourceNetwork.Calculate(source);
  Near(z.NetworkROhm.Value,0,"pure reactive network");
  Near(z.NetworkXOhm.Value,0.0003695041722813605,"20 kV network referred to 400 V");
  Near(z.TransformerROhm.Value,0.0025396825396825397,"630 kVA transformer copper-loss resistance");
  Near(Math.Sqrt(Math.Pow(z.TransformerROhm.Value,2)+Math.Pow(z.TransformerXOhm.Value,2)),0.015238095238095238,"transformer impedance from nominal uk");
  Near(z.TotalROhm.Value,z.TransformerROhm.Value,"series resistance");
  Near(z.TotalXOhm.Value,z.NetworkXOhm.Value+z.TransformerXOhm.Value,"series reactance");
  double ik=Engineering.Analyze(p).For(p.Boards[0].Circuits[0]).FaultOriginKA.Value;
  // Independent admittance expression for two impedances in series.
  double rt=1600.0/630000,xt=Math.Sqrt(Math.Pow(9600.0/630000,2)-rt*rt);
  Near(ik,400/(Math.Sqrt(3)*Math.Sqrt(rt*rt+Math.Pow(xt+0.0003695041722813605,2))*1000),"short circuit propagated into study");
  source.CurrentBasis=SourceCurrentBasis.ReferidaBT;source.ShortCircuitKA=625;
  Near(Engineering.Analyze(p).For(p.Boards[0].Circuits[0]).FaultOriginKA.Value,ik,"equivalent BT current gives same result");
  source.TransformerKva=0;Near(Engineering.Analyze(p).For(p.Boards[0].Circuits[0]).FaultOriginKA.Value,625,"BT source without transformer");
  source.CurrentBasis=SourceCurrentBasis.RedeMT;Reject(p.Validate,"MT requires transformer");source.TransformerKva=630;
  source.UpstreamVoltageV=400;Reject(p.Validate,"MT cannot use BT input voltage");source.UpstreamVoltageV=20000;
  source.LineVoltage=1100;Reject(p.Validate,"MT to BT model rejects non-BT secondary");source.LineVoltage=400;
  source.CurrentBasis=(SourceCurrentBasis)9;Reject(p.Validate,"invalid basis rejected");source.CurrentBasis=SourceCurrentBasis.RedeMT;
  source.ShortCircuitKA=Double.NaN;Reject(delegate{SourceNetwork.Calculate(source);},"standalone calculation rejects NaN");source.ShortCircuitKA=12.5;
  source.TransformerCopperLossKw=0;Check(!SourceNetwork.Calculate(source).TotalROhm.HasValue&&SourceNetwork.Calculate(source).NetworkXOhm.HasValue,"missing losses retain network but no invented total");
  source.TransformerCopperLossKw=40;Check(!SourceNetwork.Calculate(source).TotalXOhm.HasValue&&SourceNetwork.Calculate(source).Detail.Contains("incompatíveis"),"inconsistent losses do not produce imaginary reactance");
  source.TransformerCopperLossKw=37.8;Near(SourceNetwork.Calculate(source).TransformerXOhm.Value,0,"resistive transformer boundary");source.TransformerCopperLossKw=6.3;
  source.ShortCircuitKA=0;Check(!SourceNetwork.Calculate(source).TotalXOhm.HasValue,"unknown network stays pending");source.ShortCircuitKA=12.5;
  source.ShortCircuitKA=1e-300;Check(!SourceNetwork.Calculate(source).TotalXOhm.HasValue,"extremely weak source stays pending without overflow");source.ShortCircuitKA=12.5;source.TransformerKva=1e-300;Check(!SourceNetwork.Calculate(source).TotalROhm.HasValue,"extremely small transformer stays pending without NaN");source.TransformerKva=630;
  string path=Path.Combine(directory,"source.circuito");p.Save(path);var loaded=Project.Load(path);Check(loaded.Source.CurrentBasis==SourceCurrentBasis.RedeMT&&loaded.Source.UpstreamVoltageV==20000&&loaded.Source.NetworkReference==source.NetworkReference,"new source data persist with Unicode");
  Near(Engineering.Analyze(loaded).For(loaded.Boards[0].Circuits[0]).FaultOriginKA.Value,ik,"reopened project produces identical short circuit");
  string legacy=File.ReadAllText(path);foreach(string field in new[]{"CurrentBasis","UpstreamVoltageV","NetworkReference"})legacy=System.Text.RegularExpressions.Regex.Replace(legacy,"\""+field+"\":(?:\"[^\"]*\"|[0-9]+),","");
  Check(!legacy.Contains("\"CurrentBasis\"")&&!legacy.Contains("\"UpstreamVoltageV\"")&&!legacy.Contains("\"NetworkReference\""),"legacy fixture omits all new fields");File.WriteAllText(path,legacy);var old=Project.Load(path);Check(old.Source.CurrentBasis==SourceCurrentBasis.ReferidaBT&&old.Source.UpstreamVoltageV==0&&old.Source.NetworkReference=="","old files retain BT interpretation");
  source.CurrentBasis=SourceCurrentBasis.ReferidaBT;Near(Engineering.Analyze(old).For(old.Boards[0].Circuits[0]).FaultOriginKA.Value,Engineering.Analyze(p).For(p.Boards[0].Circuits[0]).FaultOriginKA.Value,"legacy result unchanged without optional data");source.CurrentBasis=SourceCurrentBasis.RedeMT;
  string memory=EngineeringReport.Memory(p,Engineering.Analyze(p));Check(memory.Contains("RedeMT")&&memory.Contains("20000 V")&&memory.Contains(source.NetworkReference)&&memory.Contains("Impedâncias referidas à BT"),"memory identifies basis reference and impedances");
  using(var wizard=new ProjectWizard()){wizard.CreateControl();var flags=BindingFlags.Instance|BindingFlags.NonPublic;wizard.GetType().GetField("draft",flags).SetValue(wizard,p);wizard.GetType().GetMethod("BuildResult",flags).Invoke(wizard,null);Check(wizard.Result.Source.CurrentBasis==SourceCurrentBasis.RedeMT&&wizard.Result.Boards[0].Voltage==400,"wizard retains MT source and creates BT board");}
  Console.WriteLine("PASS: "+count+" source-network assertions");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{Directory.Delete(directory,true);}}
}
