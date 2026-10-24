using System;
using System.IO;
using CircuitoStudio;
class CurvesTests {
 static int count;static void A(bool b,string m){if(!b)throw new Exception(m);count++;}
 static ManufacturerCurve C(string rows){return ManufacturerCurve.Parse("CurrentA;MinSeconds;MaxSeconds\n"+rows,"fabricante.csv");}
 static void Bad(string rows){bool bad=false;try{C(rows);}catch(InvalidDataException){bad=true;}A(bad,"reject "+rows);}
 static void Main(){
 var u=C("10;10;20\n100;1;2");A(u.Source=="fabricante.csv","source");
 A(Math.Abs(u.At(Math.Sqrt(1000),true)-Math.Sqrt(10))<1e-10,"log interpolation");
 var d=C("10;1;2\n100;0.1;0.2");A(CurveMath.Compare(u,d).Status.StartsWith("Envelopes separados"),"separated");
 var overlap=C("10;1;2\n30;10;12\n100;0.1;0.2");
 A(CurveMath.Compare(u,overlap).Status.StartsWith("Sobreposição"),"interior breakpoint finds crossing");
 var narrow=C("20;1;2\n40;1;2");var result=CurveMath.Compare(u,narrow);A(result.MinCurrent==20&&result.MaxCurrent==40,"shared range");
 A(CurveMath.Compare(u,C("100;1;2\n200;1;2")).Status=="Dados insuficientes","point not range");
 A(CurveMath.Compare(null,d).Status=="Dados insuficientes","missing");
 Bad("10;1;2");Bad("10;1;2\n10;1;2");Bad("20;1;2\n10;1;2");Bad("0;1;2\n10;1;2");Bad("1;0;2\n10;1;2");Bad("1;3;2\n10;1;2");Bad("1;NaN;2\n10;1;2");Bad("1;1;Infinity\n10;1;2");Bad("1;1;2;4\n10;1;2");
 bool badHeader=false;try{ManufacturerCurve.Parse("I;tmin;tmax\n1;1;2\n2;1;2","x");}catch(InvalidDataException){badHeader=true;}A(badHeader,"header");
 var c=new Circuit{CableMm2=400};A(CurveMath.CableK(c)==103,"large copper PVC");c.Cable.Material=ConductorMaterial.Aluminium;A(CurveMath.CableK(c)==68,"large aluminium PVC");
 c.CableMm2=10;c.Cable.Material=ConductorMaterial.Copper;A(!CurveMath.CableSeconds(c,100).HasValue,"time domain above 5");
 A(Math.Abs(CurveMath.CableSeconds(c,1150).Value-1)<1e-12,"cable t");A(CurveMath.CableSeconds(c,1150/Math.Sqrt(5)).HasValue,"boundary 5s");
 c.Cable.Parallel=2;A(!CurveMath.CableSeconds(c,1150).HasValue,"parallel unknown");
 A(!CurveMath.CableSeconds(c,0).HasValue,"zero invalid");
 A(Math.Abs(u.At(10,true)-10)<1e-12&&Math.Abs(u.At(100,true)-1)<1e-12,"interpolation endpoint boundaries");A(Math.Abs(overlap.At(30,true)-10)<1e-12,"interior exact knot");var largeUp=new ManufacturerCurve{Source="Ensaio grande montante"};var largeDown=new ManufacturerCurve{Source="Ensaio grande jusante"};for(int i=1;i<=10000;i++){largeUp.Points.Add(new CurvePoint(i,10,20));largeDown.Points.Add(new CurvePoint(i,1,2));}var largeResult=CurveMath.Compare(largeUp,largeDown);A(largeResult.Status.StartsWith("Envelopes separados")&&largeResult.MinCurrent==1&&largeResult.MaxCurrent==10000,"large curve range with binary interpolation");
 Console.WriteLine(count+" curve checks passed");
 }
}
