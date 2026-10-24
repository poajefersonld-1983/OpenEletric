using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;

namespace CircuitoStudio {
 [DataContract] public enum DiagramSymbol { [EnumMember] Automatico,[EnumMember] Carga,[EnumMember] Motor,[EnumMember] Lampada,[EnumMember] Tomada,[EnumMember] Capacitor,[EnumMember] Gerador,[EnumMember] Transformador,[EnumMember] Inversor }
 [DataContract] public enum DiagramTheme { [EnumMember] Colorido,[EnumMember] Monocromatico }
 public sealed partial class Project {
  [DataMember(Order=32),Category("Unifilar"),DisplayName("Estilo do desenho")] public DiagramTheme DrawingTheme {get;set;}
 }
 public sealed partial class Circuit {
  string diagramLabel;
  [DataMember(Order=84),Category("Unifilar"),DisplayName("Etiqueta personalizada"),Description("Texto gráfico. Vazio usa ID e nome do circuito; não modifica os dados elétricos.")] public string DiagramLabel {get{return diagramLabel??"";}set{diagramLabel=value;}}
  [DataMember(Order=85),Category("Unifilar"),DisplayName("Símbolo gráfico"),Description("Símbolos conceituais. A seleção modifica apenas o desenho, sem mudar o tipo de carga ou seus cálculos.")] public DiagramSymbol DrawingSymbol {get;set;}
 }
 public static class CircuitVisuals {
  sealed class Geometry {public List<float[]> Lines=new List<float[]>(),Circles=new List<float[]>();public string Glyph="";}
  public static string Caption(Circuit c){return String.IsNullOrWhiteSpace(c.DiagramLabel)?c.Id+" · "+c.Name:c.DiagramLabel;}
  public static Color Ink(Project p){return p!=null&&p.DrawingTheme==DiagramTheme.Monocromatico?Color.Black:Color.FromArgb(34,57,79);}
  public static Color Accent(Project p){return p!=null&&p.DrawingTheme==DiagramTheme.Monocromatico?Color.Black:Color.FromArgb(0,118,131);}
  public static DiagramSymbol Resolve(Circuit c){if(c.DrawingSymbol!=DiagramSymbol.Automatico)return c.DrawingSymbol;string kind=(c.Kind??"").ToLowerInvariant();if(kind.Contains("inversor"))return DiagramSymbol.Inversor;if(kind.Contains("motor"))return DiagramSymbol.Motor;if(kind.Contains("capacitor")||c.ReactiveKvarOverride<0)return DiagramSymbol.Capacitor;if(kind.Contains("ilumina")||kind.Contains("lâmpada"))return DiagramSymbol.Lampada;if(kind.Contains("tomada"))return DiagramSymbol.Tomada;if(kind.Contains("gerador"))return DiagramSymbol.Gerador;if(kind.Contains("transformador"))return DiagramSymbol.Transformador;return DiagramSymbol.Carga;}
  static Geometry Shape(DiagramSymbol symbol){var g=new Geometry();g.Circles.Add(new float[]{0,0,15});switch(symbol){case DiagramSymbol.Lampada:g.Lines.Add(new float[]{-10,-10,10,10});g.Lines.Add(new float[]{-10,10,10,-10});break;case DiagramSymbol.Tomada:g.Circles.Add(new float[]{-5,-3,2});g.Circles.Add(new float[]{5,-3,2});g.Lines.Add(new float[]{-6,7,6,7});break;case DiagramSymbol.Capacitor:g.Lines.Add(new float[]{-9,-3,9,-3});g.Lines.Add(new float[]{-9,3,9,3});g.Lines.Add(new float[]{0,-15,0,-3});g.Lines.Add(new float[]{0,3,0,15});break;case DiagramSymbol.Transformador:g.Circles.Add(new float[]{-5,0,7});g.Circles.Add(new float[]{5,0,7});break;case DiagramSymbol.Motor:g.Glyph="M";break;case DiagramSymbol.Gerador:g.Glyph="G";break;case DiagramSymbol.Inversor:g.Glyph="V";break;default:g.Glyph="~";break;}return g;}
  public static void Draw(Graphics graphics,Circuit c,float x,float y,Pen pen,Brush glyph,Font font){var shape=Shape(Resolve(c));foreach(var circle in shape.Circles)graphics.DrawEllipse(pen,x+circle[0]-circle[2],y+circle[1]-circle[2],2*circle[2],2*circle[2]);foreach(var line in shape.Lines)graphics.DrawLine(pen,x+line[0],y+line[1],x+line[2],y+line[3]);if(shape.Glyph!="")using(var format=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center})graphics.DrawString(shape.Glyph,font,glyph,new RectangleF(x-15,y-15,30,30),format);}
  internal static void Dxf(DxfDrawing drawing,Circuit circuit,double x,double y){var shape=Shape(Resolve(circuit));foreach(var circle in shape.Circles)drawing.Circle(x+circle[0],-(y+circle[1]),circle[2],"SYMBOLS");foreach(var line in shape.Lines)drawing.Line(x+line[0],-(y+line[1]),x+line[2],-(y+line[3]),"SYMBOLS");if(shape.Glyph!="")drawing.Text(x-5,-(y+5),12,shape.Glyph,"SYMBOLS");}
  public static void Svg(StringBuilder output,Circuit c,double x,double y,Project project){var shape=Shape(Resolve(c));string ink=ColorTranslator.ToHtml(Ink(project)),accent=ColorTranslator.ToHtml(Accent(project));output.Append("<g class=\"load-symbol\" data-symbol=\"").Append(Resolve(c)).Append("\" fill=\"none\" stroke=\"").Append(ink).Append("\" stroke-width=\"2\">");foreach(var circle in shape.Circles)output.AppendFormat(CultureInfo.InvariantCulture,"<circle cx=\"{0}\" cy=\"{1}\" r=\"{2}\"/>",x+circle[0],y+circle[1],circle[2]);foreach(var line in shape.Lines)output.AppendFormat(CultureInfo.InvariantCulture,"<path d=\"M {0} {1} L {2} {3}\"/>",x+line[0],y+line[1],x+line[2],y+line[3]);if(shape.Glyph!="")output.AppendFormat(CultureInfo.InvariantCulture,"<text x=\"{0}\" y=\"{1}\" text-anchor=\"middle\" dominant-baseline=\"central\" font-size=\"16\" stroke=\"none\" fill=\"{2}\">{3}</text>",x,y,accent,shape.Glyph);output.Append("</g>");}
 }
}
