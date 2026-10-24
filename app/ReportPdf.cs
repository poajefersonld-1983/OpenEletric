extern alias PdfEngine;
using PdfSharp = PdfEngine::PdfSharp;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using PdfEngine::PdfSharp.Drawing;
using PdfEngine::PdfSharp.Pdf;
namespace CircuitoStudio {
 public static class ReportPdf {
  public static int SaveText(string text,string title,string path){if(text==null)throw new ArgumentNullException("text");try{text=text.Normalize(NormalizationForm.FormC);}catch(ArgumentException ex){throw new InvalidDataException("Texto Unicode inválido para o PDF.",ex);}EmbeddedComponents.Ensure();return WriteCore(text,title,path);}
  sealed class FontChoice {
   public string Name;public System.Windows.Media.GlyphTypeface Glyphs;public XFont Font;
   public XFont Get(){return Font??(Font=new XFont(Name,9,XFontStyleEx.Regular,new XPdfFontOptions(PdfFontEncoding.Unicode)));}
  }
  sealed class Piece {public string Text;public XFont Font;public double Width;}
  static FontChoice Choose(List<FontChoice> fonts,string value){
   foreach(var font in fonts){bool available=true;for(int i=0;i<value.Length;i++){int point=Char.ConvertToUtf32(value,i);if(Char.IsHighSurrogate(value[i]))i++;if(!font.Glyphs.CharacterToGlyphMap.ContainsKey(point)){available=false;break;}}if(available){try{font.Get();return font;}catch(InvalidOperationException){}}}
   throw new InvalidDataException("Nenhuma fonte instalada compatível com o caractere U+"+Char.ConvertToUtf32(value,0).ToString("X")+". O arquivo anterior foi preservado.");
  }
  static void DrawLine(XGraphics graphics,List<Piece> line,double y){double x=36;foreach(var piece in line){graphics.DrawString(piece.Text,piece.Font,XBrushes.Black,new XPoint(x,y));x+=piece.Width;}}
  static void NormalizeUnicodeMaps(string path){
   string repaired=path+".unicode.tmp";try{using(var document=PdfSharp.Pdf.IO.PdfReader.Open(path,PdfSharp.Pdf.IO.PdfDocumentOpenMode.Modify)){bool changed=false;
    foreach(var item in document.Internals.GetAllObjects()){var dictionary=item as PdfDictionary;if(dictionary==null||dictionary.Stream==null)continue;byte[] bytes=dictionary.Stream.UnfilteredValue;string content=Encoding.ASCII.GetString(bytes);if(!content.Contains("begincmap"))continue;
     string corrected=Regex.Replace(content,@"\d+\s+beginbfrange(.*?)endbfrange",delegate(Match block){var entries=new List<string>();bool hasExtended=false;foreach(Match entry in Regex.Matches(block.Groups[1].Value,@"<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>\s*<([0-9A-Fa-f]+)>")){string target=entry.Groups[3].Value;if(target.Length!=5&&target.Length!=6){entries.Add(entry.Value);continue;}hasExtended=true;int first=Int32.Parse(entry.Groups[1].Value,NumberStyles.HexNumber),last=Int32.Parse(entry.Groups[2].Value,NumberStyles.HexNumber),point=Int32.Parse(target,NumberStyles.HexNumber);if(last<first||last-first>65535)throw new InvalidDataException("Mapa Unicode inválido.");for(int glyph=first;glyph<=last;glyph++){string value=Char.ConvertFromUtf32(point+glyph-first);var hex=new StringBuilder();foreach(char unit in value)hex.Append(((int)unit).ToString("X4"));entries.Add("<"+glyph.ToString("X4")+"><"+glyph.ToString("X4")+"><"+hex+">");}}return hasExtended?entries.Count+" beginbfrange\n"+String.Join("\n",entries)+"\nendbfrange":block.Value;},RegexOptions.Singleline);
     if(corrected!=content){dictionary.Elements.Remove("/Filter");dictionary.Elements.Remove("/DecodeParms");dictionary.Stream.Value=Encoding.ASCII.GetBytes(corrected);changed=true;}
    }
    if(changed)document.Save(repaired);
   }if(File.Exists(repaired)){File.Delete(path);File.Move(repaired,path);}}finally{if(File.Exists(repaired))File.Delete(repaired);}
  }
  [MethodImpl(MethodImplOptions.NoInlining)] static int WriteCore(string text,string title,string path){
   var fonts=new List<FontChoice>();foreach(string name in new[]{"Consolas","Segoe UI","Segoe UI Symbol","Microsoft YaHei","Microsoft JhengHei","SimSun","Yu Gothic","Malgun Gothic","Segoe UI Emoji","Cambria Math","DejaVu Sans Mono","DejaVu Sans","Noto Sans","Droid Sans Fallback","Noto Sans Symbols2"}){System.Windows.Media.GlyphTypeface face;var typeface=new System.Windows.Media.Typeface(name);if(typeface.TryGetGlyphTypeface(out face))fonts.Add(new FontChoice{Name=name,Glyphs=face});}
   if(fonts.Count==0)throw new InvalidDataException("Nenhuma fonte disponível para gerar o PDF.");
   string full=Path.GetFullPath(path),temporary=full+"."+Guid.NewGuid().ToString("N")+".tmp";int pages=0;XGraphics graphics=null;double y=0;double width=0,height=0;
   try{using(var document=new PdfDocument()){
    document.Info.Title=title??"OpenEletric";document.Info.Creator="OpenEletric";document.Info.Subject="Memória de cálculo e hipóteses do projeto";
    Action page=delegate{if(graphics!=null)graphics.Dispose();if(pages>=10000)throw new InvalidDataException("Relatório excede 10000 páginas.");var sheet=document.AddPage();sheet.Size=PdfSharp.PageSize.A4;sheet.Orientation=PdfSharp.PageOrientation.Landscape;graphics=XGraphics.FromPdfPage(sheet);pages++;width=sheet.Width.Point-72;height=sheet.Height.Point-48;y=48;graphics.DrawString("OpenEletric - "+pages,fonts[0].Get(),XBrushes.Gray,new XPoint(36,sheet.Height.Point-24));};
    page();
    foreach(string raw in text.Replace("\r\n","\n").Replace('\r','\n').Replace("\t","    ").Split('\n')){
     var line=new List<Piece>();double used=0;var elements=StringInfo.GetTextElementEnumerator(raw);
     while(elements.MoveNext()){string value=elements.GetTextElement();var font=Choose(fonts,value).Get();double advance=graphics.MeasureString(value,font).Width;if(advance>width)throw new InvalidDataException("Caractere excede a largura da página.");while(used+advance>width&&line.Count>0){int split=line.FindLastIndex(piece=>piece.Text==" ");List<Piece> remainder;List<Piece> output;if(split>line.Count/2){output=line.GetRange(0,split);remainder=line.GetRange(split+1,line.Count-split-1);}else{output=line;remainder=new List<Piece>();}DrawLine(graphics,output,y);y+=15;line=remainder;used=0;foreach(var part in line)used+=part.Width;if(y>height)page();}line.Add(new Piece{Text=value,Font=font,Width=advance});used+=advance;}
     if(y>height)page();DrawLine(graphics,line,y);y+=15;
    }
    if(graphics!=null){graphics.Dispose();graphics=null;}document.Save(temporary);
   }NormalizeUnicodeMaps(temporary);if(File.Exists(full))File.Replace(temporary,full,null);else File.Move(temporary,full);return pages;
   }finally{if(graphics!=null)graphics.Dispose();if(File.Exists(temporary))File.Delete(temporary);}
  }
 }
}
