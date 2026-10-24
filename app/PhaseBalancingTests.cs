using System;using System.IO;using System.Linq;using CircuitoStudio;
class PhaseBalancingTests {
 static int count;static void Check(bool b){if(!b)throw new Exception("Balance test "+count);count++;}
 static void Main(){var p=new Project();var b=new Board{Id="Q",Voltage=380};p.Boards.Add(b);for(int i=0;i<3;i++)b.Circuits.Add(new Circuit{Id="C"+i,Voltage=220,PowerKw=2.2,PhaseLabel="L1"});var r=PhaseBalancing.Run(p);Check(Math.Abs(r.BeforePercent-200)<1e-8);Check(r.AfterPercent<1e-8);Check(b.Circuits.Select(c=>c.PhaseLabel).Distinct().Count()==3);b.Circuits[0].PhaseLocked=true;b.Circuits[0].PhaseLabel="L3";r=PhaseBalancing.Run(p);Check(b.Circuits[0].PhaseLabel=="L3");Check(r.Locked==1&&r.Assigned==2);b.Circuits[0].Voltage=127;bool rejected=false;try{PhaseBalancing.Run(p);}catch(InvalidDataException){rejected=true;}Check(rejected);Check(b.Circuits[0].PhaseLabel=="L3");Console.WriteLine("PASS: "+count+" phase balancing assertions");}
}
