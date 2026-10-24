using System;
using System.IO;
using CircuitoStudio;
internal static class FaultDynamicsTests {
 static int n;
 static void Check(bool value,string label){n++;if(!value)throw new Exception(label);}
 static void Near(double actual,double expected,string label){Check(Math.Abs(actual-expected)<=1e-9*Math.Max(1,Math.Abs(expected)),label);}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch(InvalidDataException){rejected=true;}Check(rejected,label);}
 public static int Main(){try{
  var d=FaultDynamics.Calculate(.1,1,50,10);
  Near(d.PowerFactor,1/Math.Sqrt(101),"short-circuit power factor from R/Z");
  Near(d.Kappa,1.7460018562680836,"independent exponential peak-factor benchmark");
  Near(d.PeakKA,24.692195050629234,"10 kA symmetrical produces peak benchmark");
  Near(d.TimeConstantSeconds.Value,1/Math.PI/10,"tau from inductance and resistance");
  Near(d.DcEnvelopeKA(0),Math.Sqrt(2)*10,"maximum initial DC offset envelope");
  Near(d.DcEnvelopeKA(d.TimeConstantSeconds.Value),Math.Sqrt(2)*10/Math.E,"DC envelope decays by e after one tau");
  Check(d.DcEnvelopeKA(.01)>d.DcEnvelopeKA(.02)&&d.DcEnvelopeKA(.02)>d.DcEnvelopeKA(.1),"positive R decays monotonically");
  Near(d.InstantaneousKA(0,0),0,"zero prefault current on voltage-zero closure");
  Near(d.InstantaneousKA(0,72),0,"zero prefault current for arbitrary closure angle");
  double phi=Math.Atan(10),time=.005;
  Near(d.InstantaneousKA(time,0),Math.Sqrt(2)*10*(Math.Sin(Math.PI/2-phi)+Math.Sin(phi)*Math.Exp(-Math.PI/20)),"quarter-cycle R-L solution reference");
  Near(d.InstantaneousKA(.012,30),d.InstantaneousKA(.012,390),"closing angle periodicity");
  var sixty=FaultDynamics.Calculate(.1,1,60,10);Near(sixty.Kappa,d.Kappa,"peak factor independent of frequency at fixed R/X");Near(sixty.TimeConstantSeconds.Value/d.TimeConstantSeconds.Value,5.0/6,"tau follows frequency");
  var resistive=FaultDynamics.Calculate(1,0,50,10);Near(resistive.PowerFactor,1,"pure resistance power factor");Near(resistive.TimeConstantSeconds.Value,0,"pure resistance no time constant");Near(resistive.DcEnvelopeKA(0),0,"pure resistance no DC transient");Near(resistive.InstantaneousKA(.005,0),Math.Sqrt(2)*10,"pure resistive sinusoid");Near(resistive.Kappa,1.02,"empirical kappa resistive limit");
  var inductive=FaultDynamics.Calculate(0,1,50,10);Check(inductive.Undamped&&!inductive.TimeConstantSeconds.HasValue,"pure inductance has no finite decay time");Near(inductive.PowerFactor,0,"pure inductance power factor");Near(inductive.Kappa,2,"pure inductance kappa limit");Near(inductive.DcEnvelopeKA(100),Math.Sqrt(2)*10,"pure inductance retains DC envelope");Near(inductive.InstantaneousKA(.01,0),2*Math.Sqrt(2)*10,"pure inductance first half-cycle peak");
  var zero=FaultDynamics.Calculate(1,1,50,0);Near(zero.PeakKA,0,"zero symmetrical current zero peak");Near(zero.InstantaneousKA(.01,0),0,"zero symmetrical current zero waveform");
  Reject(delegate{FaultDynamics.Calculate(0,0,50,10);},"zero impedance rejected");Reject(delegate{FaultDynamics.Calculate(-1,1,50,10);},"negative R rejected");Reject(delegate{FaultDynamics.Calculate(1,Double.NaN,50,10);},"NaN X rejected");Reject(delegate{FaultDynamics.Calculate(1,1,0,10);},"zero frequency rejected");Reject(delegate{d.DcEnvelopeKA(-.001);},"negative time rejected");Reject(delegate{d.InstantaneousKA(.01,Double.PositiveInfinity);},"non-finite angle rejected");
  var tiny=FaultDynamics.Calculate(1e-300,0,50,10);Near(tiny.PowerFactor,1,"scaled impedance avoids underflow");
  var p=Project.CreateSample();p.Source.ShortCircuitKA=10;p.Source.RXRatio=.1;p.Boards[0].Circuits[0].Phases=PhaseMode.Trifasico;p.Boards[0].Circuits[0].Voltage=380;var c=p.Boards[0].Circuits[0];c.LengthM=20;c.CableMm2=10;var study=Engineering.Analyze(p);var result=study.For(c);Near(result.FaultOriginDynamics.PeakKA,result.FaultOriginKA.Value*Math.Sqrt(2)*d.Kappa,"origin dynamics uses actual network R/X");Check(result.FaultEndDynamics!=null&&result.FaultEndDynamics.PeakKA<result.FaultOriginDynamics.PeakKA,"cable reduces peak in reference circuit");
  string memory=EngineeringReport.Memory(p,study);Check(memory.Contains("pico estimado ip=")&&memory.Contains("envelope CC máximo em 10/20/100 ms")&&memory.Contains("não corrente RMS"),"memory reports peak and separates DC envelope from RMS");
  p.Source.ShortCircuitKA=0;result=Engineering.Analyze(p).For(c);Check(result.FaultOriginDynamics==null&&result.FaultEndDynamics==null,"unknown source produces no invented transient");
  Console.WriteLine("PASS: "+n+" fault-dynamics assertions");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
