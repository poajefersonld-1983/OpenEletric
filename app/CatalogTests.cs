using System;
using System.IO;
using System.Text;
using System.Linq;
using CircuitoStudio;
class CatalogTests {
static int n;static void Check(bool b){n++;if(!b)throw new Exception("Check "+n);}
static void PreserveOnInvalidExport(string csv){var valid=new CatalogData();valid.Items.Add(new Equipment{Reference="preservar"});CatalogData.ExportCsv(valid,csv);byte[] old=File.ReadAllBytes(csv);var invalid=new CatalogData();invalid.Items.Add(new Equipment());try{CatalogData.ExportCsv(invalid,csv);throw new Exception("Invalid export accepted");}catch(InvalidDataException){n++;}Check(File.ReadAllBytes(csv).SequenceEqual(old));}
static void Reject(string path,string text){File.WriteAllText(path,text,Encoding.UTF8);try{CatalogData.ImportCsv(path);}catch(InvalidDataException){n++;return;}throw new Exception("Accepted invalid CSV");}
static void Main(){string dir=Path.Combine(Path.GetTempPath(),"catalog-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);try{string csv=Path.Combine(dir,"a.csv"),json=Path.Combine(dir,"a.json");var d=new CatalogData();CatalogData.ExportCsv(d,csv);Check(CatalogData.ImportCsv(csv).Items.Count==0);d.Items.Add(new Equipment{Manufacturer="Teste",Reference="R;\"1",Description="Linha 1\r\nLinha 2",RatedA=12.5,Source="Manual"});CatalogData.ExportCsv(d,csv);var copy=CatalogData.ImportCsv(csv);Check(copy.Items[0].Reference=="R;\"1");Check(copy.Items[0].Description==d.Items[0].Description);d.Save(json);Check(CatalogData.Load(json).Items[0].RatedA==12.5);d.Save(json);Check(CatalogData.Load(json).Items.Count==1);string h=CatalogData.Header+"\r\n";File.WriteAllText(csv,h+"A;X;D;C;12,5;;;;;;S\r\n",Encoding.UTF8);Check(CatalogData.ImportCsv(csv).Items[0].RatedA==12.5);Reject(csv,h+"A;;D;C;;;;;;;S\n");Reject(csv,h+"A;X;D;C;-1;;;;;;S\n");Reject(csv,h+"A;X;D;C;NaN;;;;;;S\n");Reject(csv,h+"A;X;D;C;1.234,5;;;;;;S\n");Reject(csv,h+"A;X;D;C;;;;;;;S\nA;X;D;C;;;;;;;S\n");Reject(csv,h+"A;\"unclosed");Reject(csv,h+"A;X;\"D\"oops;C;;;;;;;S\n");Reject(csv,h+"A;X;D\"bad;C;;;;;;;S\n");Reject(csv,h+"A;X\n");PreserveOnInvalidExport(csv);Console.WriteLine("PASS: "+n+" catalog assertions");}finally{Directory.Delete(dir,true);}}
}
