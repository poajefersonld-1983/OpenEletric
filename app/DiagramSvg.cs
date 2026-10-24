using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Security;
namespace CircuitoStudio {
 public static class DiagramSvg {
  static string E(string value){return SecurityElement.Escape(value??"");}
  static string N(double value){return value.ToString("0.##",CultureInfo.GetCultureInfo("pt-BR"));}
  static void Text(StringBuilder s,double x,double y,string value,int size){s.AppendFormat(CultureInfo.InvariantCulture,"<text x=\"{0}\" y=\"{1}\" font-size=\"{2}\">{3}</text>",x,y,size,E(value));}
  static void Line(StringBuilder s,double x1,double y1,double x2,double y2){s.AppendFormat(CultureInfo.InvariantCulture,"<path d=\"M {0} {1} L {2} {3}\"/>",x1,y1,x2,y2);}
  public static void Save(Project project,Board board,string file){
   project.Validate();var study=Engineering.Analyze(project);int width=Math.Max(920,(int)board.Circuits.Select(c=>c.DiagramX+220).DefaultIfEmpty(0).Max()),height=Math.Max(Math.Max(450,145+((board.Circuits.Count+3)/4)*310),(int)board.Circuits.Select(c=>c.DiagramY+310).DefaultIfEmpty(0).Max());
   var s=new StringBuilder();s.AppendFormat(CultureInfo.InvariantCulture,"<?xml version=\"1.0\" encoding=\"utf-8\"?><svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 {0} {1}\"><title>{2}</title><desc>Diagrama conceitual independente; posições persistidas; símbolos genéricos sem certificação.</desc><rect width=\"100%\" height=\"100%\" fill=\"white\"/><g fill=\"#173047\" font-family=\"Segoe UI,Arial,sans-serif\">",width,height,E(project.Name+" — "+board.Name));
   Text(s,26,35,project.Name,20);Text(s,26,65,board.Name+" · "+board.Voltage+" V · "+project.Frequency+" Hz · "+project.Earthing,13);
   for(int i=0;i<board.Circuits.Count;i++){
    var c=board.Circuits[i];var result=study.For(c);int row=i/4,col=i%4;double bus=115+row*310,x=c.DiagramX>0?c.DiagramX:95+col*218,y=c.DiagramY>0?c.DiagramY:bus;
    s.Append("<g data-circuit-id=\"").Append(E(c.Id)).Append("\"><title>").Append(E(CircuitVisuals.Caption(c))).Append("</title>");s.Append("<g stroke=\"#173047\" stroke-width=\"2\" fill=\"none\">");if(col==0){Line(s,72,bus,width-60,bus);s.Append("</g>");Text(s,72,bus-15,"Barramento · "+board.Name+" · trecho "+(row+1),11);s.Append("<g stroke=\"#173047\" stroke-width=\"2\" fill=\"none\">");}Line(s,x,bus,x,y+30);Line(s,x,y+33,x+15,y+54);Line(s,x,y+66,x,y+90);s.AppendFormat(CultureInfo.InvariantCulture,"<circle cx=\"{0}\" cy=\"{1}\" r=\"3\"/><circle cx=\"{0}\" cy=\"{2}\" r=\"3\"/>",x,y+33,y+63);s.Append("</g>");CircuitVisuals.Svg(s,c,x,y+105,project);

    s.AppendFormat(CultureInfo.InvariantCulture,"<rect x=\"{0}\" y=\"{1}\" width=\"198\" height=\"142\" fill=\"#f3f7fa\"/>",x-30,y+132);string name=CircuitVisuals.Caption(c);if(name.Length>27)name=name.Substring(0,26)+"…";Text(s,x-20,y+155,name,13);Text(s,x-20,y+178,c.Kind+" · "+(c.Phases==PhaseMode.Trifasico?"3 fases":"1 fase"),11);Text(s,x-20,y+201,N(result.DemandKw)+" kW · "+N(result.ReactiveKvar)+" kvar",11);Text(s,x-20,y+224,(study.NetworkFlow==null?"":"Nom. ")+N(result.CurrentA)+" A · "+N(c.Voltage)+" V",12);Text(s,x-20,y+247,N(c.CableMm2)+" mm² · DJ "+N(c.BreakerA)+" A",11);if(c.ChildBoardId!="")Text(s,x-20,y+267,"→ quadro "+c.ChildBoardId,10);s.Append("</g>");
   }
   Text(s,26,height-20,(study.NetworkFlow==null?"":"Valores nominais; fluxo por fase em Ferramentas e Memória. ")+"Posições e dados do projeto; diagramas não substituem verificações elétricas. Confira Estudos e Memória.",11);s.Append("</g></svg>");string svg=s.ToString().Replace("stroke=\"#173047\"","stroke=\""+System.Drawing.ColorTranslator.ToHtml(CircuitVisuals.Ink(project))+"\"").Replace("fill=\"#173047\"","fill=\""+System.Drawing.ColorTranslator.ToHtml(CircuitVisuals.Ink(project))+"\"");if(project.DrawingTheme==DiagramTheme.Monocromatico)svg=svg.Replace("fill=\"#f3f7fa\"","fill=\"white\"");File.WriteAllText(file,svg,new UTF8Encoding(false));
  }
 }
}
