using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using CircuitoStudio;
internal static class LoadReferenceTests {
 static int n;static void Check(bool ok,string label){n++;if(!ok)throw new Exception(label);}static void Near(double a,double b,string label){Check(Math.Abs(a-b)<1e-8*Math.Max(1,Math.Abs(b)),label);}
 static string N(double value){return value.ToString("R",CultureInfo.InvariantCulture);}
 public static int Main(){try{
  var p=new Project{Name="Cargas — ensaio senoidal Ω"};p.Source.LineVoltage=400;p.Source.NetworkReference="Referência de cargas sintéticas; tensão nominal";double u0=400/Math.Sqrt(3);
  var root=new Board{Id="Q1",Voltage=400};var child=new Board{Id="Q2",Voltage=400};var last=new Board{Id="Q3",Voltage=400};p.Boards.AddRange(new[]{root,child,last});
  var motor=new Circuit{Id="MOTOR",Voltage=400,Phases=PhaseMode.Trifasico,PhaseLabel="ABC",DemandFactor=.6,CableMm2=10};root.Circuits.Add(motor);MotorStarting.Apply(p,root,motor,new MotorStartingData{ShaftKw=7.5,Efficiency=.9,Source="Placa sintética do motor; revisão 1"},PhaseMode.Trifasico,400,.8);
  root.Circuits.Add(new Circuit{Id="FEED1",ChildBoardId="Q2",Voltage=400,Phases=PhaseMode.Trifasico,PhaseLabel="ABC",PowerKw=999,PowerFactor=.5,DemandFactor=.01,CableMm2=25});child.Circuits.Add(new Circuit{Id="FEED2",ChildBoardId="Q3",Voltage=400,Phases=PhaseMode.Trifasico,PhaseLabel="ABC",PowerKw=888,PowerFactor=.5,DemandFactor=.02,CableMm2=16});
  var drive=new Circuit{Id="DRIVE",Kind="Inversor + motor",Voltage=400,Phases=PhaseMode.Trifasico,PhaseLabel="ABC",PowerKw=4,PowerFactor=.9,DemandFactor=.75,CableMm2=6,DriveMotor=new DriveMotorData{DriveReference="Drive sintético",InputRatedA=10,OutputRatedA=9,OutputVoltageV=400,MotorReference="Motor sintético",MotorRatedA=8,MotorVoltageV=400,MotorShaftKw=3,Source="Placas sintéticas; PF de deslocamento declarado no modelo senoidal"}};child.Circuits.Add(drive);
  child.Circuits.Add(new Circuit{Id="LIGHT",Kind="Iluminação resistiva",Voltage=u0,Phases=PhaseMode.Monofasico,PhaseLabel="L1",PowerKw=1.15,PowerFactor=1,DemandFactor=.8,CableMm2=2.5});child.Circuits.Add(new Circuit{Id="SOCKET",Kind="Carga genérica senoidal",Voltage=u0,Phases=PhaseMode.Monofasico,PhaseLabel="L2",PowerKw=2.3,PowerFactor=.8,DemandFactor=.5,CableMm2=2.5});
  var cap=new Circuit{Id="CAP",Kind="Banco capacitor",Voltage=400,Phases=PhaseMode.Trifasico,PhaseLabel="ABC",PowerKw=0,PowerFactor=1,ReactiveKvarOverride=-3,DemandFactor=1,CableMm2=4};last.Circuits.Add(cap);last.Circuits.Add(new Circuit{Id="HEATER",Kind="Aquecimento resistivo",Voltage=u0,Phases=PhaseMode.Monofasico,PhaseLabel="L3",PowerKw=2,PowerFactor=1,DemandFactor=.75,CableMm2=4});
  var s=Engineering.Analyze(p);Near(s.TotalKw,11.57,"total active input includes shaft efficiency and demand once");Near(s.For(motor).DemandKw,5,"motor shaft conversion feeds demand");Near(s.For(drive).DemandKw,3,"drive output shaft not added to input load");Near(s.For(cap).ReactiveKvar,-3,"capacitor signed reactive load");Check(s.For(cap).CurrentA>0,"reactive-only capacitor has positive RMS current");Check(s.PhaseCurrentKnown,"consistent line/phase voltages make phase currents available");
  string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"referencias-cargas");Directory.CreateDirectory(directory);p.Save(Path.Combine(directory,"origem.circuito"));
  var csv=new StringBuilder("Tipo;Id;P_kW;Q_kvar;S_kVA;FP;I_A\n");csv.AppendLine("Total;Projeto;"+N(s.TotalKw)+";"+N(s.TotalKvar)+";"+N(s.TotalKva)+";"+N(s.PowerFactor)+";");for(int i=0;i<3;i++)csv.AppendLine("Fase;L"+(i+1)+";;;;;"+N(s.PhaseCurrentA[i]));foreach(var row in s.Circuits)csv.AppendLine("Circuito;"+row.Circuit.Id+";"+N(row.DemandKw)+";"+N(row.ReactiveKvar)+";;;"+N(row.CurrentA));File.WriteAllText(Path.Combine(directory,"resultados.csv"),csv.ToString(),Encoding.UTF8);
  var loaded=Project.Load(Path.Combine(directory,"origem.circuito"));var reopened=Engineering.Analyze(loaded);Near(reopened.TotalKw,s.TotalKw,"active power roundtrip reproducible");Near(reopened.TotalKvar,s.TotalKvar,"reactive power roundtrip reproducible");for(int i=0;i<3;i++)Near(reopened.PhaseCurrentA[i],s.PhaseCurrentA[i],"phase current roundtrip reproducible");
  p.Source.LineVoltage=380;Check(!Engineering.Analyze(p).PhaseCurrentKnown,"inconsistent source voltage does not claim valid phase aggregation");
  Console.WriteLine("PASS: "+n+" load reference assertions; independent comparison fixture generated");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
