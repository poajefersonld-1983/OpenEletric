using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.Globalization;
using System.Collections.Generic;
using System.Windows.Forms;
namespace CircuitoStudio {
 public static class ReportPrinting {
  public static List<string> WrapLines(string text,Font font,Graphics graphics,float width){
   if(text==null||font==null||graphics==null)throw new ArgumentNullException("text/font/graphics");
   if(Single.IsNaN(width)||Single.IsInfinity(width)||width<=0)throw new ArgumentOutOfRangeException("width");
   var result=new List<string>();
   foreach(string raw in text.Replace("\r\n","\n").Replace('\r','\n').Split('\n')){
    if(raw.Length==0){result.Add("");continue;}
    int[] boundaries=StringInfo.ParseCombiningCharacters(raw);int start=0;
    while(start<boundaries.Length){int low=1,high=Math.Min(2048,boundaries.Length-start),fit=0;
     while(low<=high){int candidate=low+(high-low)/2;int end=start+candidate<boundaries.Length?boundaries[start+candidate]:raw.Length;string chunk=raw.Substring(boundaries[start],end-boundaries[start]);
      if(graphics.MeasureString(chunk,font,Int32.MaxValue,StringFormat.GenericTypographic).Width<=width){fit=candidate;low=candidate+1;}else high=candidate-1;}
     if(fit==0)throw new InvalidOperationException("A largura disponível não comporta um caractere do relatório. Aumente a área imprimível ou reduza a fonte.");
     int cut=start+fit<boundaries.Length?boundaries[start+fit]:raw.Length;
     if(start+fit<boundaries.Length){int space=raw.LastIndexOf(' ',cut-1,cut-boundaries[start]);if(space>=boundaries[start]+(cut-boundaries[start])/2){int element=Array.BinarySearch(boundaries,space);if(element>start){fit=element-start;cut=space;}}}
     result.Add(raw.Substring(boundaries[start],cut-boundaries[start]));start+=fit;
     while(start<boundaries.Length&&raw[boundaries[start]]==' ')start++;
    }
   }
   return result;
  }
  public static string Rtf(string text){var s=new StringBuilder(@"{\rtf1\ansi\deff0{\fonttbl{\f0 Consolas;}}\uc1\f0\fs18 ");foreach(char c in text){if(c=='\r')continue;if(c=='\n')s.Append(@"\par ");else if(c=='\t')s.Append(@"\tab ");else if(c=='\\'||c=='{'||c=='}')s.Append('\\').Append(c);else if(c>127)s.Append(@"\u").Append(unchecked((short)c).ToString(CultureInfo.InvariantCulture)).Append('?');else if(c>=32)s.Append(c);}return s.Append('}').ToString();}
  public static void SaveRtf(Project p,string file){File.WriteAllText(file,Rtf(EngineeringReport.Memory(p,Engineering.Analyze(p))),Encoding.ASCII);}
  public static void Preview(Project p,IWin32Window owner){PreviewText(EngineeringReport.Memory(p,Engineering.Analyze(p)),p.Name,owner);}
  public static void PreviewText(string text,string title,IWin32Window owner){if(text==null)throw new ArgumentNullException("text");using(var doc=new PrintDocument())using(var font=new Font("Consolas",9))using(var dialog=new PrintDialog()){
   doc.DocumentName=title+" — OpenEletric";doc.DefaultPageSettings.Landscape=true;doc.DefaultPageSettings.Margins=new Margins(50,50,50,50);dialog.Document=doc;dialog.UseEXDialog=true;if(dialog.ShowDialog(owner)!=DialogResult.OK)return;
   List<string> lines=null;int index=0,page=0;doc.BeginPrint+=(s,e)=>{lines=null;index=0;page=0;};doc.PrintPage+=(s,e)=>{page++;float lineHeight=font.GetHeight(e.Graphics)+2;
    if(e.MarginBounds.Width<=0||e.MarginBounds.Height<lineHeight+25){e.Cancel=true;e.HasMorePages=false;return;}
    if(lines==null)lines=WrapLines(text,font,e.Graphics,e.MarginBounds.Width);
    float y=e.MarginBounds.Top;while(index<lines.Count&&y+lineHeight<e.MarginBounds.Bottom-25){e.Graphics.DrawString(lines[index++],font,Brushes.Black,e.MarginBounds.Left,y);y+=lineHeight;}e.Graphics.DrawString("OpenEletric · página "+page,font,Brushes.Gray,e.MarginBounds.Left,e.MarginBounds.Bottom-15);e.HasMorePages=index<lines.Count;
   };
   using(var preview=new PrintPreviewDialog{Document=doc,Width=1100,Height=800,UseAntiAlias=true})preview.ShowDialog(owner);
  }}
 }
 partial class Studio {
  void ExportRtf(){using(var d=new SaveFileDialog{Filter="Rich Text (*.rtf)|*.rtf",FileName=SafeName(project.Name)+"-relatorio.rtf",InitialDirectory=Folder()})if(d.ShowDialog(this)==DialogResult.OK)ReportPrinting.SaveRtf(project,d.FileName);}
 }
}
