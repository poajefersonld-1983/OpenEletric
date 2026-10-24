using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
namespace CircuitoStudio {
 // Drawing interchange only. No imported entity becomes an electrical circuit.
 public sealed class DxfEntity {
  public string Type, Layer, Text; public double X,Y,X2,Y2,Radius,Height,Rotation;
 }
 public sealed class DxfInspection {
  internal byte[] Original;
  public string Version,EncodingName;public List<DxfEntity> Entities=new List<DxfEntity>();
  public Dictionary<string,int> Unsupported=new Dictionary<string,int>();
  public bool CompletePreview {get{return Unsupported.Count==0;}}
  public string Summary {get{return "DXF "+Version+" / "+EncodingName+"; "+Entities.Count+" entidades 2D na prévia. "+(CompletePreview?"Prévia geométrica LINE/CIRCLE/TEXT; estilos de fonte, cores, espessuras e visibilidade por camada não reproduzidos.":"PRÉVIA INCOMPLETA: "+String.Join(", ",Unsupported.Select(x=>x.Key+"="+x.Value).ToArray()))+"\r\nReexportação preserva os bytes originais, inclusive entidades não desenhadas e metadados. Não incorpora desenho ao projeto nem recupera circuitos, conexões elétricas ou cálculos. Inspeção AMB disponível separadamente.";}}
  public void SaveOriginal(string path){DrawingDxf.Atomic(path,Original);}
 }
 public sealed class DxfDrawing {
  public readonly List<DxfEntity> Entities=new List<DxfEntity>();
  public void Line(double x,double y,double x2,double y2,string layer){Entities.Add(new DxfEntity{Type="LINE",X=x,Y=y,X2=x2,Y2=y2,Layer=layer});}
  public void Circle(double x,double y,double radius,string layer){Entities.Add(new DxfEntity{Type="CIRCLE",X=x,Y=y,Radius=radius,Layer=layer});}
  public void Text(double x,double y,double height,string text,string layer){Entities.Add(new DxfEntity{Type="TEXT",X=x,Y=y,Height=height,Text=text,Layer=layer});}
  public void Rect(double x,double y,double width,double height,string layer){Line(x,y,x+width,y,layer);Line(x+width,y,x+width,y+height,layer);Line(x+width,y+height,x,y+height,layer);Line(x,y+height,x,y,layer);}
 }
 public static class DrawingDxf {
  public const string Scope="DXF R12 ASCII; unifilar conceitual, sem circuito elétrico recuperável; frontal esquemática, sem fabricação/3D/térmica/IP. Inspeção AMB disponível separadamente.";
  const int MaxBytes=32*1024*1024,MaxEntities=100000;
  static readonly CultureInfo Inv=CultureInfo.InvariantCulture;
  static bool Finite(double d){return !Double.IsNaN(d)&&!Double.IsInfinity(d)&&Math.Abs(d)<=1e9;}
  static void Number(double d){if(!Finite(d))throw new InvalidDataException("Coordenada/valor DXF inválido ou fora de ±1e9.");}
  static string N(double d){Number(d);return d.ToString("R",Inv);}
  static string Escape(string s){
   if(s==null)throw new InvalidDataException("Texto DXF ausente.");
   try{new UTF8Encoding(false,true).GetByteCount(s);}catch(EncoderFallbackException ex){throw new InvalidDataException("Texto DXF contém Unicode inválido.",ex);}var b=new StringBuilder();foreach(char c in s){if(Char.IsSurrogate(c))throw new InvalidDataException("Exportação R12 limitada a caracteres BMP; caractere suplementar não suportado, destino preservado.");if(c<32||c==127)throw new InvalidDataException("Texto DXF contém quebra de linha ou controle.");if(c>126||c=='\\'||c=='%')b.Append("\\U+").Append(((int)c).ToString("X4",Inv));else b.Append(c);}
   if(b.Length>255)throw new InvalidDataException("Texto DXF R12 excede 255 bytes codificados. Reduza o texto; destino preservado.");return b.ToString();
  }
  static string Unescape(string s){return Regex.Replace(s,@"\\U\+([0-9a-fA-F]{4})",m=>((char)Int32.Parse(m.Groups[1].Value,NumberStyles.HexNumber,Inv)).ToString());}
  internal static void Atomic(string path,byte[] bytes){
   if(bytes==null||bytes.Length>MaxBytes)throw new InvalidDataException("DXF ausente ou excede 32 MiB.");
   string full=Path.GetFullPath(path),temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{using(var stream=new FileStream(temp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){stream.Write(bytes,0,bytes.Length);stream.Flush(true);}if(File.Exists(full))File.Replace(temp,full,full+".bak");else File.Move(temp,full);}finally{if(File.Exists(temp))File.Delete(temp);}
  }
  public static void Save(DxfDrawing drawing,string path){
   if(drawing==null||drawing.Entities.Count>MaxEntities)throw new InvalidDataException("Desenho ausente ou excede 100000 entidades.");
   var s=new StringBuilder();Action<int,string> pair=(code,value)=>{s.Append(code.ToString(Inv)).Append("\r\n").Append(value).Append("\r\n");if(s.Length>MaxBytes)throw new InvalidDataException("DXF excede 32 MiB; destino preservado.");};
   pair(0,"SECTION");pair(2,"HEADER");pair(9,"$ACADVER");pair(1,"AC1009");pair(9,"$DWGCODEPAGE");pair(3,"ANSI_1252");pair(9,"$LUNITS");pair(70,"2");pair(9,"$LUPREC");pair(70,"3");pair(0,"ENDSEC");
   pair(0,"SECTION");pair(2,"TABLES");pair(0,"TABLE");pair(2,"LTYPE");pair(70,"1");pair(0,"LTYPE");pair(2,"CONTINUOUS");pair(70,"0");pair(3,"Solid line");pair(72,"65");pair(73,"0");pair(40,"0");pair(0,"ENDTAB");
   var layers=drawing.Entities.Select(x=>x.Layer??"0").Concat(new[]{"0"}).Distinct().ToList();
   pair(0,"TABLE");pair(2,"LAYER");pair(70,layers.Count.ToString(Inv));foreach(string layer in layers){if(!Regex.IsMatch(layer,@"^[A-Za-z0-9_-]{1,31}$"))throw new InvalidDataException("Nome de camada DXF inválido.");pair(0,"LAYER");pair(2,layer);pair(70,"0");pair(62,"7");pair(6,"CONTINUOUS");}pair(0,"ENDTAB");
   pair(0,"TABLE");pair(2,"STYLE");pair(70,"1");pair(0,"STYLE");pair(2,"STANDARD");pair(70,"0");pair(40,"0");pair(41,"1");pair(50,"0");pair(71,"0");pair(42,"2.5");pair(3,"txt");pair(4,"");pair(0,"ENDTAB");pair(0,"ENDSEC");pair(0,"SECTION");pair(2,"BLOCKS");pair(0,"ENDSEC");pair(0,"SECTION");pair(2,"ENTITIES");
   foreach(var e in drawing.Entities){if(e.Type!="LINE"&&e.Type!="CIRCLE"&&e.Type!="TEXT")throw new InvalidDataException("Entidade de exportação DXF não suportada.");pair(0,e.Type);pair(8,e.Layer??"0");pair(10,N(e.X));pair(20,N(e.Y));pair(30,"0");if(e.Type=="LINE"){pair(11,N(e.X2));pair(21,N(e.Y2));pair(31,"0");}if(e.Type=="CIRCLE"){if(e.Radius<=0)throw new InvalidDataException("Raio DXF deve ser positivo.");pair(40,N(e.Radius));}if(e.Type=="TEXT"){if(e.Height<=0)throw new InvalidDataException("Altura DXF deve ser positiva.");pair(40,N(e.Height));pair(1,Escape(e.Text));pair(50,N(e.Rotation));pair(7,"STANDARD");}}
   pair(0,"ENDSEC");pair(0,"EOF");Atomic(path,Encoding.ASCII.GetBytes(s.ToString()));
  }
  sealed class Pair{public int Code;public string Value;}
  static string Value(List<Pair> pairs,int code,string fallback){var found=pairs.Where(x=>x.Code==code).ToList();if(found.Count>1)throw new InvalidDataException("Código DXF duplicado na geometria 2D: "+code);return found.Count==0?fallback:found[0].Value;}
  static double Numeric(List<Pair> p,int code,double fallback,bool required=false){string s=Value(p,code,null);if(s==null){if(required)throw new InvalidDataException("Código DXF obrigatório ausente: "+code);return fallback;}double d;if(!Double.TryParse(s,NumberStyles.Float,Inv,out d)||!Finite(d))throw new InvalidDataException("Número DXF inválido: "+code);return d;}
  static void Unsupported(DxfInspection doc,string kind){if(!doc.Unsupported.ContainsKey(kind))doc.Unsupported[kind]=0;doc.Unsupported[kind]++;}
  public static DxfInspection Inspect(string path){
   if(!String.Equals(Path.GetExtension(path),".dxf",StringComparison.OrdinalIgnoreCase))throw new InvalidDataException("Selecione DXF. AMB não suportado; não renomeie o arquivo.");
   if(new FileInfo(path).Length>MaxBytes)throw new InvalidDataException("DXF excede 32 MiB.");byte[] bytes=File.ReadAllBytes(path);if(bytes.Any(x=>x==0)||Encoding.ASCII.GetString(bytes.Take(22).ToArray()).StartsWith("AutoCAD Binary DXF"))throw new InvalidDataException("DXF binário não suportado.");
   // Version marker is ASCII even when text values use a legacy code page or UTF-8.
   string ascii=Encoding.ASCII.GetString(bytes);var version=Regex.Match(ascii,@"\$ACADVER\s*\r?\n\s*1\s*\r?\n(AC\d{4})");if(!version.Success)throw new InvalidDataException("Cabeçalho $ACADVER ausente.");
   string rev=version.Groups[1].Value;if(!new[]{"AC1009","AC1012","AC1014","AC1015","AC1018","AC1021","AC1024","AC1027","AC1032"}.Contains(rev))throw new InvalidDataException("Versão DXF não reconhecida: "+rev);
   bool utf8=Int32.Parse(rev.Substring(2),Inv)>=1021;Encoding enc=utf8?(Encoding)new UTF8Encoding(false,true):Encoding.GetEncoding(1252,EncoderFallback.ExceptionFallback,DecoderFallback.ExceptionFallback);
   if(!utf8){var cp=Regex.Match(ascii,@"\$DWGCODEPAGE\s*\r?\n\s*3\s*\r?\n([^\r\n]+)");if(cp.Success&&cp.Groups[1].Value.Trim()!="ANSI_1252")throw new InvalidDataException("Página de código DXF não suportada nesta revisão: "+cp.Groups[1].Value.Trim());}
   string text;try{text=enc.GetString(bytes);}catch(DecoderFallbackException ex){throw new InvalidDataException("Codificação DXF inválida.",ex);}var lines=text.Replace("\r\n","\n").Split('\n').ToList();if(lines.Count>0&&lines[lines.Count-1]=="")lines.RemoveAt(lines.Count-1);if(lines.Count%2!=0)throw new InvalidDataException("DXF com pares incompletos.");var pairs=new List<Pair>();for(int i=0;i<lines.Count;i+=2){int code;if(!Int32.TryParse(lines[i].Trim().TrimStart('\uFEFF'),NumberStyles.Integer,Inv,out code)||code<0||code>1071)throw new InvalidDataException("Código DXF inválido na linha "+(i+1));pairs.Add(new Pair{Code=code,Value=lines[i+1]});}
   var doc=new DxfInspection{Original=bytes,Version=rev,EncodingName=utf8?"UTF-8":"Windows-1252"};string section=null;bool eof=false,seenEntities=false;int count=0;
   for(int i=0;i<pairs.Count;){var p=pairs[i];if(p.Code!=0){i++;continue;}string type=p.Value.Trim();if(type=="SECTION"){if(section!=null||i+1>=pairs.Count||pairs[i+1].Code!=2)throw new InvalidDataException("Seção DXF inválida.");section=pairs[i+1].Value.Trim();if(section=="ENTITIES"){if(seenEntities)throw new InvalidDataException("Seção ENTITIES duplicada.");seenEntities=true;}i+=2;continue;}if(type=="ENDSEC"){if(section==null)throw new InvalidDataException("ENDSEC sem seção.");section=null;i++;continue;}if(type=="EOF"){if(section!=null||i!=pairs.Count-1)throw new InvalidDataException("EOF DXF inválido.");eof=true;break;}
    int end=i+1;while(end<pairs.Count&&pairs[end].Code!=0)end++;if(section==null)throw new InvalidDataException("Entidade fora de seção DXF.");if(section=="ENTITIES"){
     if(++count>MaxEntities)throw new InvalidDataException("DXF excede 100000 entidades.");var fields=pairs.GetRange(i+1,end-i-1);
     if(type!="LINE"&&type!="CIRCLE"&&type!="TEXT")Unsupported(doc,type);
     else{
      double z=Numeric(fields,30,0),z2=Numeric(fields,31,0),nx=Numeric(fields,210,0),ny=Numeric(fields,220,0),nz=Numeric(fields,230,1);bool transformed=z!=0||z2!=0||nx!=0||ny!=0||nz!=1||Numeric(fields,67,0)!=0||Numeric(fields,60,0)!=0||Numeric(fields,39,0)!=0;
      if(type=="TEXT")transformed|=Numeric(fields,72,0)!=0||Numeric(fields,73,0)!=0||Numeric(fields,71,0)!=0||Numeric(fields,41,1)!=1||Numeric(fields,51,0)!=0;
      if(transformed)Unsupported(doc,type+" (3D/papel/oculto/alinhamento especial)");
      else{var e=new DxfEntity{Type=type,Layer=Value(fields,8,"0"),X=Numeric(fields,10,0,true),Y=Numeric(fields,20,0,true)};if(type=="LINE"){e.X2=Numeric(fields,11,0,true);e.Y2=Numeric(fields,21,0,true);}if(type=="CIRCLE"){e.Radius=Numeric(fields,40,0,true);if(e.Radius<=0)throw new InvalidDataException("Raio DXF inválido.");}if(type=="TEXT"){e.Height=Numeric(fields,40,0,true);if(e.Height<=0)throw new InvalidDataException("Altura DXF inválida.");e.Text=Unescape(Value(fields,1,""));e.Rotation=Numeric(fields,50,0);}doc.Entities.Add(e);}
     }
    }i=end;
   }
   if(!eof||!seenEntities)throw new InvalidDataException("DXF truncado ou sem seção ENTITIES.");return doc;
  }
  public static DxfDrawing Unifilar(Project project,Board board){
   project.Validate();if(board==null||!project.Boards.Contains(board))throw new InvalidDataException("Quadro fora do projeto.");var d=new DxfDrawing();var study=Engineering.Analyze(project);double width=Math.Max(920,board.Circuits.Select(c=>c.DiagramX+220).DefaultIfEmpty(0).Max());
   d.Text(26,-35,20,project.Name,"LABELS");d.Text(26,-65,13,board.Name+" / "+board.Voltage+" V / "+project.Frequency+" Hz","LABELS");
   for(int i=0;i<board.Circuits.Count;i++){var c=board.Circuits[i];var result=study.For(c);int row=i/4,col=i%4;double bus=115+row*310,x=c.DiagramX>0?c.DiagramX:95+col*218,y=c.DiagramY>0?c.DiagramY:bus;if(col==0)d.Line(72,-bus,width-60,-bus,"BUS");d.Line(x,-bus,x,-(y+30),"WIRES");d.Line(x,-(y+33),x+15,-(y+54),"PROTECTION");d.Line(x,-(y+66),x,-(y+90),"WIRES");d.Circle(x,-(y+33),3,"PROTECTION");d.Circle(x,-(y+63),3,"PROTECTION");CircuitVisuals.Dxf(d,c,x,y+105);string caption=CircuitVisuals.Caption(c);d.Text(x-20,-(y+155),13,caption,"LABELS");d.Text(x-20,-(y+178),11,c.Kind+" / "+(c.Phases==PhaseMode.Trifasico?"3F":"1F"),"LABELS");d.Text(x-20,-(y+201),11,result.DemandKw.ToString("0.###",Inv)+" kW / "+result.ReactiveKvar.ToString("0.###",Inv)+" kvar","RESULTS");d.Text(x-20,-(y+224),12,"Nominal "+result.CurrentA.ToString("0.###",Inv)+" A / "+c.Voltage+" V","RESULTS");d.Text(x-20,-(y+247),11,c.CableMm2+" mm2 / DJ "+c.BreakerA+" A","RESULTS");if(c.ChildBoardId!="")d.Text(x-20,-(y+267),10,"Quadro "+c.ChildBoardId,"LABELS");}
   d.Text(26,-(460+((board.Circuits.Count+3)/4)*310),9,"Unidades graficas abstratas; geometria conceitual. Sem conectividade eletrica recuperavel.","NOTES");return d;
  }
  public static DxfDrawing Frontal(Project project,Board board){
   var result=EnclosureCatalog.Evaluate(project,board);var spec=board.Enclosure;if(!Enclosures.SpecValid(spec))throw new InvalidDataException("Geometria frontal inválida.");var d=new DxfDrawing();d.Rect(0,0,spec.WidthMm,-spec.HeightMm,"ENCLOSURE");double left=(spec.WidthMm-spec.ModulesPerRow*spec.ModulePitchMm)/2;
   for(int r=0;r<spec.Rows;r++)d.Line(left,-(70+r*spec.RowPitchMm),left+spec.ModulesPerRow*spec.ModulePitchMm,-(70+r*spec.RowPitchMm),"RAILS");
   foreach(var place in Enclosures.Layout(board))if(place.Placed){double x=left+(place.Column-1)*spec.ModulePitchMm,y=30+(place.Row-1)*spec.RowPitchMm;d.Rect(x,-y,place.Modules*spec.ModulePitchMm-1,-80,"DEVICES");d.Text(x+2,-(y+48),8,place.Circuit.Id,"LABELS");}
   d.Text(10,-(spec.HeightMm+25),7,spec.Manufacturer+" / "+spec.Reference,"LABELS");d.Text(10,-(spec.HeightMm+42),5,"Unidade de desenho = mm; frontal esquematica; sem cotas OEM de origem/furacao.","NOTES");d.Text(10,-(spec.HeightMm+55),5,result.Accepted?"Ocupacao verificada no escopo. Sem prova termica/IP/3D.":"PENDENTE: "+result.Issues.Count+" avisos. Verifique ocupacao no projeto.","NOTES");return d;
  }
 }
}
