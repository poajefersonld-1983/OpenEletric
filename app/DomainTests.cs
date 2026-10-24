using System;
using System.IO;
using CircuitoStudio;
internal static class DomainTests
{
    static void Check(bool condition,string name) { if(!condition) throw new Exception(name); }
    static void Reject(Action action,string name) { bool failed=false; try {action();} catch {failed=true;} Check(failed,name); }
    static void Near(double actual,double expected,string name) {Check(Math.Abs(actual-expected)<1e-9,name);}
    public static int Main()
    {
        string dir=Path.Combine(Path.GetTempPath(),"CircuitoStudio-tests-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        try {
            Circuit c=new Circuit {PowerKw=2.2,Voltage=220,PowerFactor=1,DemandFactor=1}; Near(c.DesignCurrentA,10,"single phase analytic");
            c.PowerFactor=0.8;c.DemandFactor=0.5;Near(c.DesignCurrentA,6.25,"demand and power factor");
            c.Phases=PhaseMode.Trifasico;c.Voltage=400;c.PowerKw=12;Near(c.DesignCurrentA,12000*0.5/(400*0.8*Math.Sqrt(3)),"three phase analytic");
            Project p=Project.CreateSample();p.Name="Acentuação elétrica"; string path=Path.Combine(dir,"projeto.json");p.Save(path);
            Project q=Project.Load(path);Check(q.Name==p.Name&&q.Boards[0].Circuits.Count==2,"round trip");Near(q.Boards[0].Circuits[0].DesignCurrentA,p.Boards[0].Circuits[0].DesignCurrentA,"round trip current");
            p.Name="Revisão";p.Save(path);Check(Project.Load(path+".bak").Name=="Acentuação elétrica","atomic replacement backup");
            string valid=File.ReadAllText(path);Check(valid.Contains("\"FileVersion\":2"),"new files use version 2");
            string legacy=valid.Replace("\"FileVersion\":2","\"FileVersion\":1");File.WriteAllText(path,legacy);q=Project.Load(path);Check(q.Name==p.Name,"version 1 remains readable");q.Save(path);Check(File.ReadAllText(path).Contains("\"FileVersion\":2"),"legacy save upgrades format");Check(File.ReadAllText(path+".bak")==legacy,"upgrade preserves original version 1 backup");
            File.WriteAllText(path,valid.Replace("\"FileVersion\":2","\"FileVersion\":5"));Reject(delegate {Project.Load(path);},"unknown version rejected");
            File.WriteAllText(path,valid.Replace("\"FileVersion\":2,",""));Reject(delegate {Project.Load(path);},"missing version rejected");
            File.WriteAllText(path,"{\"FileVersion\":1}");Reject(delegate {Project.Load(path);},"missing fields rejected");
            p=Project.CreateSample();p.Boards[0].Circuits[1].Id=p.Boards[0].Circuits[0].Id;Reject(p.Validate,"duplicate ID");
            p=Project.CreateSample();p.Boards[0].Circuits[0].PowerFactor=0;Reject(p.Validate,"zero power factor");
            p=Project.CreateSample();p.Boards[0].Circuits[0].PowerKw=Double.NaN;Reject(p.Validate,"NaN");
            p=Project.CreateSample();p.Boards[0].Circuits[0].Voltage=Double.PositiveInfinity;Reject(p.Validate,"infinity");
            p=Project.CreateSample();p.Boards[0].Circuits[0].Phases=(PhaseMode)5;Reject(p.Validate,"phase enum");
            p=Project.CreateSample();p.Boards[0].Circuits[0].PowerKw=-1;Reject(delegate {p.Save(path);},"invalid save rejected");Check(File.ReadAllText(path)=="{\"FileVersion\":1}","invalid save preserves destination");
            p=Project.CreateSample();p.Save(path);byte[] protectedFile=File.ReadAllBytes(path),protectedBackup=File.ReadAllBytes(path+".bak");string longName=new string('x',90000);for(int i=0;i<400;i++)p.Boards[0].Circuits.Add(new Circuit{Id="BIG"+i,Name=longName});bool tooLarge=false;try{p.Save(path);}catch(InvalidDataException ex){tooLarge=ex.Message.Contains("32 MiB");}Check(tooLarge,"oversized save rejected explicitly");Check(System.Linq.Enumerable.SequenceEqual(protectedFile,File.ReadAllBytes(path))&&System.Linq.Enumerable.SequenceEqual(protectedBackup,File.ReadAllBytes(path+".bak")),"oversized save preserves original and backup");Check(Directory.GetFiles(dir,"*.tmp").Length==0,"oversized save removes temporary file");
            Console.WriteLine("PASS: analytic currents, Unicode round trip, atomic backup, version, size limit and invalid-data rejection."); return 0;
        } catch(Exception ex) {Console.Error.WriteLine(ex);return 1;} finally {Directory.Delete(dir,true);}
    }
}
