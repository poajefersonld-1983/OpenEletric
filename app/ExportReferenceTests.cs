using System;
using System.IO;
using CircuitoStudio;
internal static class ExportReferenceTests {
 public static void Main(){string d=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"referencias-exportacao");Directory.CreateDirectory(d);var p=new Project{Name="Projeto Ω; <ensaio>",Client="Cliente ç",Earthing="TN-S"};p.Source.LineVoltage=400;p.Source.ShortCircuitKA=10;
 var root=new Board{Id="ROOT",Name="Quadro principal Ω",Voltage=400};var child=new Board{Id="CHILD",Name="Quadro intermediário",Voltage=400};var last=new Board{Id="END",Name="Quadro final",Voltage=400};p.Boards.AddRange(new[]{root,child,last});
 root.Circuits.Add(new Circuit{Id="F1",Name="Alimentador 1",ChildBoardId="CHILD",Phases=PhaseMode.Trifasico,PhaseLabel="ABC",Voltage=400,PowerKw=999,PowerFactor=.7,DemandFactor=.8,CableMm2=25,LengthM=10,DiagramX=450,DiagramY=200});
 child.Circuits.Add(new Circuit{Id="F2",Name="Alimentador 2",ChildBoardId="END",Phases=PhaseMode.Trifasico,PhaseLabel="ABC",Voltage=400,PowerKw=888,PowerFactor=.8,DemandFactor=.5,CableMm2=16,LengthM=20});
 child.Circuits.Add(new Circuit{Id="MONO;\"Ω",Name="Carga monofásica; \"Unicode ç\"",Phases=PhaseMode.Monofasico,PhaseLabel="L2",Voltage=230.94,PowerKw=2.4,PowerFactor=.8,DemandFactor=.75,CableMm2=2.5,LengthM=15,Notes="Linha um\nLinha dois; delimitador"});
 last.Circuits.Add(new Circuit{Id="MOTOR",Name="Motor trifásico",Phases=PhaseMode.Trifasico,PhaseLabel="ABC",Voltage=400,PowerKw=12,PowerFactor=.8,DemandFactor=.5,CableMm2=10,LengthM=30,DrawingSymbol=DiagramSymbol.Motor});
 last.Circuits.Add(new Circuit{Id="CAP",Name="Compensação",Phases=PhaseMode.Trifasico,PhaseLabel="ABC",Voltage=400,PowerKw=0,ReactiveKvarOverride=-3,DemandFactor=1,CableMm2=4,LengthM=10,DrawingSymbol=DiagramSymbol.Capacitor});
 p.Validate();p.Save(Path.Combine(d,"origem.circuito"));Exports.Csv(p,Path.Combine(d,"dados.csv"));Exports.Html(p,Path.Combine(d,"dados.html"));foreach(var b in p.Boards)Exports.Svg(p,b,Path.Combine(d,b.Id+".svg"));Console.WriteLine("PASS: independent export verification fixture generated");
 }
}
