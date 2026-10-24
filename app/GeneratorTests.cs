using System;
using System.IO;
using System.Linq;
using CircuitoStudio;
internal static class GeneratorTests {
 static int n;
 static void Check(bool ok,string label){n++;if(!ok)throw new Exception(label);}
 static void Near(double value,double expected,string label){Check(Math.Abs(value-expected)<=1e-9*Math.Max(1,Math.Abs(expected)),label);}
 static void Reject(Action action,string label){bool failed=false;try{action();}catch(InvalidDataException){failed=true;}Check(failed,label);}
 static Project Fixture(double voltage,double kva,double xd){var p=new Project();p.Source.Model=SourceModel.GeradorSincrono;p.Source.LineVoltage=voltage;p.Source.GeneratorRatedKva=kva;p.Source.GeneratorXdPercent=xd;p.Source.NetworkReference="Ensaio sintético de gerador Ω";var b=new Board{Id="QG",Voltage=voltage};b.Circuits.Add(new Circuit{Id="G1",Voltage=voltage,Phases=PhaseMode.Trifasico,PhaseLabel="ABC",PowerKw=10,LengthM=100,CableMm2=25});p.Boards.Add(b);return p;}
 public static int Main(){string directory=Path.Combine(Path.GetTempPath(),"generator-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);try{
  var p=Fixture(400,1000,20);var c=p.Boards[0].Circuits[0];var z=SourceNetwork.Calculate(p.Source);
  Near(z.GeneratorXOhm.Value,.032,"1 MVA 400 V x=20% gives .032 ohm");Near(z.GeneratorROhm.Value,0,"explicit zero resistance");
  Check(!z.NetworkROhm.HasValue&&!z.TransformerROhm.HasValue,"generator is not reported as utility or transformer");
  var row=Engineering.Analyze(p).For(c);Near(row.FaultOriginKA.Value,7.216878364870322,"independent per-unit generator Ik 5 times rated current");
  Near(row.FaultTwoPhaseOriginKA.Value,6.25,"two phase initial symmetrical generator current");
  double rCable=23.7/25*.1,xCable=.08*.1;
  Near(row.FaultEndKA.Value,400/(Math.Sqrt(3)*Math.Sqrt(rCable*rCable+Math.Pow(.032+xCable,2))*1000),"series feeder impedance applied to generator");
  Check(row.FaultOriginDynamics==null&&row.FaultEndDynamics==null,"constant R-L waveform not misrepresented as machine dynamics");
  Check(!p.Source.SupportsConstantImpedanceWaveform,"generator waveform explicitly unavailable");
  p.Source.GeneratorROhm=.024;z=SourceNetwork.Calculate(p.Source);Near(z.TotalROhm.Value,.024,"manufacturer R used independently from X");Near(Engineering.Analyze(p).For(c).FaultOriginKA.Value,5.773502691896258,"R=.024 X=.032 impedance .04");
  var medium=Fixture(11000,10000,20);Near(SourceNetwork.Calculate(medium.Source).GeneratorXOhm.Value,2.42,"10 MVA 11 kV generator reactance");Near(Engineering.Analyze(medium).For(medium.Boards[0].Circuits[0]).FaultOriginKA.Value,2.62431940540739,"MV per-unit initial current");
  var example=Fixture(400,500,30);Near(Engineering.Analyze(example).For(example.Boards[0].Circuits[0]).FaultOriginKA.Value,2.4056261216234405,"500 kVA 400 V 30 percent analytical example");
  p.Source.VoltageFactor=1.1;Near(Engineering.Analyze(p).For(c).FaultOriginKA.Value,6.350852961085884,"voltage factor scales generator current exactly once");
  p.Source.GeneratorXdPercent=0;Check(!Engineering.Analyze(p).For(c).FaultOriginKA.HasValue,"missing reactance leaves unknown fault");p.Source.GeneratorXdPercent=20;p.Source.GeneratorRatedKva=0;Check(!SourceNetwork.Calculate(p.Source).TotalROhm.HasValue,"missing rating remains unknown");p.Source.GeneratorRatedKva=1000;
  p.Source.ShortCircuitKA=10;Reject(p.Validate,"utility and generator cannot be silently paralleled");p.Source.ShortCircuitKA=0;
  p.Source.TransformerKva=630;Reject(p.Validate,"generator transformer rejected until represented explicitly");p.Source.TransformerKva=0;
  p.Source.CurrentBasis=SourceCurrentBasis.RedeMT;Reject(p.Validate,"utility MT current basis invalid for generator");p.Source.CurrentBasis=SourceCurrentBasis.ReferidaBT;
  p.Source.GeneratorXdPercent=double.NaN;Reject(p.Validate,"NaN reactance rejected");p.Source.GeneratorXdPercent=20;p.Source.GeneratorROhm=-1;Reject(p.Validate,"negative resistance rejected");p.Source.GeneratorROhm=.024;
  p.Source.Model=(SourceModel)200;Reject(p.Validate,"unknown source model rejected");p.Source.Model=SourceModel.GeradorSincrono;
  string path=Path.Combine(directory,"generator.circuito");p.Save(path);Check(File.ReadAllText(path).Contains("\"FileVersion\":3"),"generator uses format 3 so old executables reject rather than silently drop source data");string saved=File.ReadAllText(path);File.WriteAllText(path,saved.Replace("\"FileVersion\":3","\"FileVersion\":2"));Reject(delegate{Project.Load(path);},"generator in legacy format rejected");File.WriteAllText(path,saved);var loaded=Project.Load(path);Check(loaded.Source.Model==SourceModel.GeradorSincrono&&loaded.Source.GeneratorRatedKva==1000&&loaded.Source.GeneratorXdPercent==20&&loaded.Source.GeneratorROhm==.024&&loaded.Source.NetworkReference==p.Source.NetworkReference,"generator and reference survive persistence");
  Near(Engineering.Analyze(loaded).For(loaded.Boards[0].Circuits[0]).FaultOriginKA.Value,Engineering.Analyze(p).For(c).FaultOriginKA.Value,"reopened generator calculation reproducible");
  string report=EngineeringReport.Memory(p,Engineering.Analyze(p));Check(report.Contains("GeradorSincrono")&&report.Contains("Xd''")&&report.Contains(p.Source.NetworkReference)&&report.Contains("Generator_protection"),"report identifies model method inputs and primary reference");Check(report.Contains("não usar como corrente mínima/permanente"),"report preserves scope of initial-current model");
  var legacy=new Project();legacy.Source.ShortCircuitKA=10;legacy.Save(path);string json=File.ReadAllText(path).Replace("\"Model\":0,","");File.WriteAllText(path,json);loaded=Project.Load(path);Check(loaded.Source.Model==SourceModel.Rede,"legacy missing discriminator defaults utility");Check(loaded.Source.SupportsConstantImpedanceWaveform,"utility waveform remains available");
  Console.WriteLine("PASS: "+n+" generator assertions including LV/MV, independent analytical cases, persistence and rejection");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{Directory.Delete(directory,true);}}
}
