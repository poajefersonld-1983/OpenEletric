using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using CircuitoStudio;
internal static class ReportPrintingTests {
 static int count;
 static void Check(bool value,string label){count++;if(!value)throw new Exception(label);}
 static void Reject(Action action,string label){bool rejected=false;try{action();}catch(ArgumentException){rejected=true;}Check(rejected,label);}
 public static int Main(){try{using(var image=new Bitmap(1000,800))using(var graphics=Graphics.FromImage(image))using(var font=new Font("Consolas",9)){
  string unicode="Instalação 北方 — a\u0301rvore Ω 😀 "+new string('界',100);
  var wrapped=ReportPrinting.WrapLines(unicode,font,graphics,130);
  Check(wrapped.Count>2,"long Unicode text wraps");
  Check(wrapped.All(line=>graphics.MeasureString(line,font,Int32.MaxValue,StringFormat.GenericTypographic).Width<=130.01f),"every Unicode line respects measured width");
  Check(String.Concat(wrapped).Replace(" ","")==unicode.Replace(" ",""),"wrapping retains every non-space character");
  Check(wrapped.All(line=>line.Length==0||(!Char.IsLowSurrogate(line[0])&&!Char.IsHighSurrogate(line[line.Length-1]))),"surrogate pairs stay together");
  Check(wrapped.All(line=>line.Length==0||CharUnicodeInfo.GetUnicodeCategory(line,0)!=UnicodeCategory.NonSpacingMark),"combining marks stay with base character");
  var breaks=ReportPrinting.WrapLines("A\rB\r\n\nC\n",font,graphics,500);Check(breaks.SequenceEqual(new[]{"A","B","","C",""}),"CR LF and blank lines preserved");
  Check(ReportPrinting.WrapLines("",font,graphics,500).SequenceEqual(new[]{""}),"empty report produces one line");
  var longWord=new string('W',50000);var words=ReportPrinting.WrapLines(longWord,font,graphics,210);Check(String.Concat(words)==longWord,"very long unbroken field retained without truncation");Check(words.All(line=>graphics.MeasureString(line,font,Int32.MaxValue,StringFormat.GenericTypographic).Width<=210.01f),"long field stays inside margin");
  Reject(delegate{ReportPrinting.WrapLines("A",font,graphics,0);},"zero width rejected");Reject(delegate{ReportPrinting.WrapLines("A",font,graphics,Single.NaN);},"NaN width rejected");
  bool tooSmall=false;try{ReportPrinting.WrapLines("W",font,graphics,.01f);}catch(InvalidOperationException){tooSmall=true;}Check(tooSmall,"impossible character width rejected instead of looping");
  var p=Project.CreateSample();p.Source.ShortCircuitKA=10;string memory=EngineeringReport.Memory(p,Engineering.Analyze(p));var report=ReportPrinting.WrapLines(memory,font,graphics,700);Check(report.Count>20&&report.All(line=>graphics.MeasureString(line,font,Int32.MaxValue,StringFormat.GenericTypographic).Width<=700.01f),"actual engineering memory fits printable width");
  Console.WriteLine("PASS: "+count+" print-layout assertions without printer access");return 0;
 }}catch(Exception ex){Console.Error.WriteLine(ex);return 1;}}
}
