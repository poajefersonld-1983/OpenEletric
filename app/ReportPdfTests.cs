extern alias PdfEngine;
using PdfSharp = PdfEngine::PdfSharp;
using System;
using System.IO;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using CircuitoStudio;
internal static class ReportPdfTests {
 static int n;static void Check(bool value,string label){n++;if(!value)throw new Exception(label);}
 [MethodImpl(MethodImplOptions.NoInlining)] static int Pages(string path){using(var doc=PdfSharp.Pdf.IO.PdfReader.Open(path,PdfSharp.Pdf.IO.PdfDocumentOpenMode.Import))return doc.PageCount;}
 [STAThread] public static int Main(string[] args){if(args.Length==2&&args[0]=="--isolated"){ReportPdf.SaveText("Projeto elétrico Ω 北 😀\nTeste isolado", "Isolado",args[1]);Console.WriteLine("ISOLATED PDF PASS");return 0;}string directory=Path.Combine(Path.GetTempPath(),"pdf-tests-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);try{
  string file=Path.Combine(directory,"unicode.pdf");int pages=ReportPdf.SaveText("Projeto elétrico Ω 北 😀\nLinha com a\u0301rvore e tolerância ≤ 5%", "Teste Unicode",file);Check(pages==1&&Pages(file)==1,"Unicode PDF generated and reopened");Check(System.Text.Encoding.ASCII.GetString(File.ReadAllBytes(file),0,5)=="%PDF-","actual PDF header");
  string longText=String.Join("\n",new string[180]).Replace("\n","Texto com acentuação Ω e limite ≤ 5%.\n");pages=ReportPdf.SaveText(longText,"Paginação",file);Check(pages>3&&Pages(file)==pages,"multi-page report reopens with correct page count");
  var p=Project.CreateSample();p.Name="Projeto elétrico Ω 北";p.Source.ShortCircuitKA=10;string selected=Path.Combine(directory,"selected.pdf");ReportSelection.Save(p,new[]{"C1"},selected,"pdf");Check(Pages(selected)>0,"selected report saves directly as PDF");
  byte[] before=File.ReadAllBytes(selected);bool rejected=false;try{ReportSelection.Save(p,new[]{"unknown"},selected,"pdf");}catch(InvalidDataException){rejected=true;}Check(rejected&&System.Linq.Enumerable.SequenceEqual(before,File.ReadAllBytes(selected)),"invalid selection preserves existing PDF");Check(Directory.GetFiles(directory,"*.tmp").Length==0,"no temporary PDF left");
  rejected=false;try{ReportPdf.SaveText("Caractere privado: "+Char.ConvertFromUtf32(0xF0000),"Inválido",selected);}catch(InvalidDataException){rejected=true;}Check(rejected&&System.Linq.Enumerable.SequenceEqual(before,File.ReadAllBytes(selected)),"unsupported glyph explicitly rejected without destroying destination");Check(Directory.GetFiles(directory,"*.tmp").Length==0,"failed glyph render removes temporary output");
  string isolated=Path.Combine(directory,"isolated");Directory.CreateDirectory(isolated);string runner=Path.Combine(isolated,"ReportPdfTests.exe");File.Copy(typeof(ReportPdfTests).Assembly.Location,runner);Check(Directory.GetFiles(isolated).Length==1,"isolated folder initially contains executable only");using(var process=Process.Start(new ProcessStartInfo(runner,"--isolated \""+Path.Combine(isolated,"output.pdf")+"\""){UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true,WorkingDirectory=isolated})){if(!process.WaitForExit(30000)){process.Kill();throw new Exception("Isolated PDF generation timeout");}string output=process.StandardOutput.ReadToEnd(),error=process.StandardError.ReadToEnd();Check(process.ExitCode==0&&output.Contains("ISOLATED PDF PASS"),"PDF works without external DLLs: "+error);}
  string artifacts=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"referencias-pdf");Directory.CreateDirectory(artifacts);ReportPdf.SaveText("Projeto elétrico Ω 北 😀\nDescrição: a\u0301rvore, tolerância ≤ 5%, corrente 12,345 A.\nTexto com chaves { } e sinal < >.","Teste Unicode",Path.Combine(artifacts,"unicode.pdf"));ReportSelection.Save(p,new[]{"C1"},Path.Combine(artifacts,"selected.pdf"),"pdf");File.Copy(file,Path.Combine(artifacts,"pagination.pdf"),true);Check(EmbeddedComponents.LicenseText().Contains("2001-2026 empira"),"PDFsharp MIT license embedded");Console.WriteLine("PASS: "+n+" PDF assertions including isolated executable");return 0;
 }catch(Exception ex){Console.Error.WriteLine(ex);return 1;}finally{Directory.Delete(directory,true);}}
}
