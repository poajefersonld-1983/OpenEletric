using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Xml.Linq;
using CircuitoStudio;
class XlsxTests {
 static int n;
 static void Check(bool b){n++;if(!b)throw new Exception("Check "+n);}
 static void Reject(Action f){try{f();}catch(InvalidDataException){n++;return;}throw new Exception("Invalid XLSX accepted");}
 static void EditXml(string path,string name,Action<XDocument> edit){using(var stream=File.Open(path,FileMode.Open,FileAccess.ReadWrite))using(var zip=new ZipArchive(stream,ZipArchiveMode.Update)){var entry=zip.GetEntry(name);XDocument xml;using(var source=entry.Open())xml=XDocument.Load(source);edit(xml);entry.Delete();using(var output=zip.CreateEntry(name).Open())xml.Save(output);}}
 static void Main(){string dir=Path.Combine(Path.GetTempPath(),"xlsx-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);try{
  string x=Path.Combine(dir,"roundtrip.xlsx");var rows=new List<string[]>{new[]{"Coluna 1","Coluna 2","Coluna 3"},new[]{"ação, çã; \"aspas\"\nlinha","=2+2",""}};XlsxWorkbook.Write(x,rows);var read=XlsxWorkbook.ReadRows(x);Check(read.Count==2&&read[0].Length==3);Check(read[1][0]==rows[1][0]&&read[1][1]=="=2+2"&&read[1][2]=="");
  var catalog=new CatalogData();catalog.Items.Add(new Equipment{Manufacturer="Fabricante",Reference="R-1",Description="Descrição\ncom linha",Category="Proteção",RatedA=12.5,Source="Manual"});CatalogData.ExportXlsx(catalog,x);var catalogCopy=CatalogData.ImportXlsx(x);Check(catalogCopy.Items.Count==1&&catalogCopy.Items[0].Description==catalog.Items[0].Description&&catalogCopy.Items[0].RatedA==12.5);using(var fs=File.OpenRead(x))using(var zip=new ZipArchive(fs,ZipArchiveMode.Read)){var sheet=zip.GetEntry("xl/worksheets/sheet1.xml");using(var stream=sheet.Open()){var xml=XDocument.Load(stream);XNamespace ns="http://schemas.openxmlformats.org/spreadsheetml/2006/main";Check(xml.Descendants(ns+"c").First(c=>(string)c.Attribute("r")=="E2").Attribute("t")==null);}}CatalogData.ExportXlsx(new CatalogData(),x);Check(CatalogData.ImportXlsx(x).Items.Count==0);
  var cables=new List<CableOption>{new CableOption{Name="Cabo ação",Material="Cu",Insulation="XLPE",Installation="B1",SectionMm2=10,IzA=55.5,ResistanceOhmKm=1.83,ReactanceOhmKm=.08,Reference="Tabela"}};CableSelection.ExportXlsx(cables,x);var cableCopy=CableSelection.ImportXlsx(x);Check(cableCopy.Count==1&&cableCopy[0].Name==cables[0].Name&&Math.Abs(cableCopy[0].IzA-55.5)<1e-9);CableSelection.ExportXlsx(new List<CableOption>(),x);Check(CableSelection.ImportXlsx(x).Count==0);
  CableSelection.ExportXlsx(cables,x);byte[] existing=File.ReadAllBytes(x);Reject(delegate{CableSelection.ExportXlsx(new List<CableOption>{cables[0],cables[0]},x);});Check(File.ReadAllBytes(x).SequenceEqual(existing));
  CatalogData.ExportXlsx(catalog,x);EditXml(x,"xl/_rels/workbook.xml.rels",xml=>xml.Root.Elements().Single().SetAttributeValue("Target","/xl/worksheets/sheet1.xml"));Check(CatalogData.ImportXlsx(x).Items.Count==1);
  XNamespace main="http://schemas.openxmlformats.org/spreadsheetml/2006/main";EditXml(x,"xl/worksheets/sheet1.xml",xml=>xml.Descendants(main+"c").Single(c=>(string)c.Attribute("r")=="K2").Remove());Check(CatalogData.ImportXlsx(x).Items[0].Source=="");EditXml(x,"xl/worksheets/sheet1.xml",xml=>{var cell=xml.Descendants(main+"c").Single(c=>(string)c.Attribute("r")=="E2");cell.SetAttributeValue("t","e");cell.RemoveNodes();cell.Add(new XElement(main+"v","#DIV/0!"));});Reject(delegate{CatalogData.ImportXlsx(x);});
  CatalogData.ExportXlsx(catalog,x);EditXml(x,"xl/worksheets/sheet1.xml",xml=>{var cell=xml.Descendants(main+"c").Single(c=>(string)c.Attribute("r")=="E2");cell.Parent.Add(new XElement(cell));});Reject(delegate{CatalogData.ImportXlsx(x);});
  Reject(delegate{XlsxWorkbook.Write(x,new List<string[]>{new[]{"RatedA"},new[]{"NaN"}});});XlsxWorkbook.Write(x,new List<string[]>{new[]{"wrong"}});Reject(delegate{CatalogData.ImportXlsx(x);});Reject(delegate{CableSelection.ImportXlsx(x);});
  Console.WriteLine("PASS: "+n+" XLSX assertions");
 }finally{Directory.Delete(dir,true);}}
}
