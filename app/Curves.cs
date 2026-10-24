using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.Serialization;
using System.ComponentModel;
namespace CircuitoStudio {
 [DataContract] public class CurvePoint {[DataMember] public double CurrentA;[DataMember] public double MinSeconds;[DataMember] public double MaxSeconds;public CurvePoint(double i,double min,double max){CurrentA=i;MinSeconds=min;MaxSeconds=max;}}
 [DataContract] public class ManufacturerCurve {
  [DataMember] public string Source;[DataMember] public List<CurvePoint> Points=new List<CurvePoint>();
  public void Validate(){if(String.IsNullOrWhiteSpace(Source)||Source.Length>2000||Source.IndexOf('\0')>=0||Points==null||Points.Count<2||Points.Count>100000)throw new InvalidDataException("Curva exige fonte identificada e de 2 a 100000 pontos.");double previous=0;foreach(var p in Points){if(p==null)throw new InvalidDataException("Ponto de curva ausente.");foreach(double v in new[]{p.CurrentA,p.MinSeconds,p.MaxSeconds})Project.Range(v,Double.Epsilon,Double.MaxValue,"ponto de curva");if(p.CurrentA<=previous||p.MinSeconds>p.MaxSeconds)throw new InvalidDataException("Curva exige correntes crescentes e tempo mínimo ≤ máximo.");previous=p.CurrentA;}}
  public ManufacturerCurve Copy(){Validate();var copy=new ManufacturerCurve{Source=Source};foreach(var p in Points)copy.Points.Add(new CurvePoint(p.CurrentA,p.MinSeconds,p.MaxSeconds));return copy;}
  public static ManufacturerCurve Load(string path){if(new FileInfo(path).Length>32*1024*1024)throw new InvalidDataException("Arquivo de curva maior que 32 MiB.");using(var r=new StreamReader(path))return Parse(r.ReadToEnd(),Path.GetFileName(path));}
  public static ManufacturerCurve Parse(string csv,string source){
   if(String.IsNullOrWhiteSpace(csv))throw new InvalidDataException("CSV vazio.");
   var c=new ManufacturerCurve{Source=source};string[] lines=csv.Replace("\r","").Split('\n');bool header=false;int line=0;
   foreach(string raw in lines){line++;if(String.IsNullOrWhiteSpace(raw))continue;string[] cols=raw.Trim().Split(';');
    if(!header){if(cols.Length!=3||cols[0].Trim().Trim('\uFEFF')!="CurrentA"||cols[1].Trim()!="MinSeconds"||cols[2].Trim()!="MaxSeconds")throw new InvalidDataException("Cabeçalho esperado: CurrentA;MinSeconds;MaxSeconds");header=true;continue;}
    double i,min,max;if(cols.Length!=3||!Number(cols[0],out i)||!Number(cols[1],out min)||!Number(cols[2],out max)||min>max)throw new InvalidDataException("Linha "+line+": números positivos, mínimo ≤ máximo; use ponto decimal.");
    if(c.Points.Count>0&&i<=c.Points[c.Points.Count-1].CurrentA)throw new InvalidDataException("Correntes devem ser estritamente crescentes.");
    c.Points.Add(new CurvePoint(i,min,max));if(c.Points.Count>100000)throw new InvalidDataException("CSV excessivamente grande.");
   }
   c.Validate();return c;
  }
  static bool Number(string s,out double n){return double.TryParse(s.Trim(),NumberStyles.Float,CultureInfo.InvariantCulture,out n)&&!Double.IsInfinity(n)&&!Double.IsNaN(n)&&n>0;}
  public double At(double current,bool minimum){
   if(Double.IsNaN(current)||current<Points[0].CurrentA||current>Points[Points.Count-1].CurrentA)throw new ArgumentOutOfRangeException("current");
   int lo=1,hi=Points.Count-1;while(lo<hi){int mid=lo+(hi-lo)/2;if(current<=Points[mid].CurrentA)hi=mid;else lo=mid+1;}var a=Points[lo-1];var b=Points[lo];double u=(Math.Log(current)-Math.Log(a.CurrentA))/(Math.Log(b.CurrentA)-Math.Log(a.CurrentA));double y1=minimum?a.MinSeconds:a.MaxSeconds,y2=minimum?b.MinSeconds:b.MaxSeconds;return Math.Exp(Math.Log(y1)+u*(Math.Log(y2)-Math.Log(y1)));
  }
 }
 [DataContract] public sealed class CurveStudyData {
  [DataMember] public ManufacturerCurve Upstream {get;set;}
  [DataMember] public ManufacturerCurve Downstream {get;set;}
  public void Validate(){if(Upstream!=null)Upstream.Validate();if(Downstream!=null)Downstream.Validate();}
  public CurveStudyData Copy(){Validate();return new CurveStudyData{Upstream=Upstream==null?null:Upstream.Copy(),Downstream=Downstream==null?null:Downstream.Copy()};}
 }
 public sealed partial class Circuit {
  [DataMember(Order=86),Browsable(false)] public CurveStudyData CurveStudy {get;set;}
 }
 public class CurveComparison {public string Status,Detail;public double MinCurrent,MaxCurrent;}
 public static class CurveMath {
  public static CurveComparison Compare(ManufacturerCurve upstream,ManufacturerCurve downstream){
   if(upstream==null||downstream==null)return new CurveComparison{Status="Dados insuficientes",Detail="Importe as duas curvas de fabricantes."};
   upstream.Validate();downstream.Validate();
   double lo=Math.Max(upstream.Points[0].CurrentA,downstream.Points[0].CurrentA),hi=Math.Min(upstream.Points[upstream.Points.Count-1].CurrentA,downstream.Points[downstream.Points.Count-1].CurrentA);
   if(hi<=lo)return new CurveComparison{Status="Dados insuficientes",Detail="Não há intervalo de corrente compartilhado."};
   var xs=new SortedSet<double>();xs.Add(lo);xs.Add(hi);foreach(var p in upstream.Points)if(p.CurrentA>lo&&p.CurrentA<hi)xs.Add(p.CurrentA);foreach(var p in downstream.Points)if(p.CurrentA>lo&&p.CurrentA<hi)xs.Add(p.CurrentA);
   bool separated=true;foreach(double x in xs)if(upstream.At(x,true)<=downstream.At(x,false)){separated=false;break;}
   return new CurveComparison{MinCurrent=lo,MaxCurrent=hi,Status=separated?"Envelopes separados na faixa informada":"Sobreposição na faixa",Detail=lo.ToString("G5")+"–"+hi.ToString("G5")+" A. Interpolação linear entre pontos em escala log–log; verificação de todos os vértices dos dois envelopes. Não comprova seletividade certificada. Zona limitadora de corrente, energia passante e proteção de retaguarda desconhecidas."};
  }
  public static double CableK(Circuit c){return c.Cable.Material==ConductorMaterial.Copper?(c.Cable.Insulation==CableInsulation.PVC?(c.CableMm2>300?103:115):143):(c.Cable.Insulation==CableInsulation.PVC?(c.CableMm2>300?68:76):94);}
  public static double? CableSeconds(Circuit c,double current){if(c==null||c.Cable==null||c.CableMm2<=0||c.Cable.Parallel!=1||current<=0||Double.IsInfinity(current)||Double.IsNaN(current))return null;double t=Math.Pow(CableK(c)*c.CableMm2/current,2);return t>0&&t<=5*(1+1e-12)?(double?)Math.Min(t,5):null;}
 }
 public class CurvesForm:Form {
  ManufacturerCurve upstream,downstream;Circuit circuit;double load;Panel plot;Label sources,status;const string Notice="Curvas importadas pelo usuário; confirme modelo, tensão e condições do fabricante. Gráfico e comparação matemáticos, sem certificação de seletividade. Cabo: t=k²S²/I² somente até 5 s, um condutor por fase; PVC 70/160 °C (140 °C acima de 300 mm²), XLPE 90/250 °C. Confirmar condições térmicas.";
  readonly Action onChanged;
  public CurvesForm(Project project,Board board,Circuit selected,Action onChanged=null){
   if(selected!=null&&(project==null||board==null||!project.Boards.Contains(board)||!board.Circuits.Contains(selected)))throw new InvalidOperationException("O circuito de curvas deve pertencer ao quadro e projeto selecionados.");
   this.onChanged=onChanged;if(selected!=null&&selected.CurveStudy!=null){var saved=selected.CurveStudy.Copy();upstream=saved.Upstream;downstream=saved.Downstream;}
   Text="Curvas tempo × corrente";Width=1050;Height=760;StartPosition=FormStartPosition.CenterParent;circuit=selected;
   try{if(selected!=null)load=Engineering.Analyze(project).For(selected).CurrentA;}catch(Exception){}
   var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=42};var up=new Button{Text="Importar montante",Width=160};var down=new Button{Text="Importar jusante",Width=160};var export=new Button{Text="Exportar PNG",Width=130};bar.Controls.AddRange(new Control[]{up,down,export});
   up.Click+=delegate{Import(true);};down.Click+=delegate{Import(false);};export.Click+=delegate{using(var d=new SaveFileDialog{Filter="PNG|*.png",FileName="curvas.png"})if(d.ShowDialog()==DialogResult.OK){using(var image=new Bitmap(1400,1000))using(var g=Graphics.FromImage(image)){Draw(g,new Rectangle(0,0,image.Width,image.Height));image.Save(d.FileName,System.Drawing.Imaging.ImageFormat.Png);}}};
   sources=new Label{Dock=DockStyle.Top,Height=45,Text="Montante: não informado | Jusante: não informado"};
   status=new Label{Dock=DockStyle.Bottom,Height=75,Padding=new Padding(8),Text="Importe CSV: CurrentA;MinSeconds;MaxSeconds. Ponto decimal, correntes crescentes."};
   var note=new Label{Dock=DockStyle.Bottom,Height=70,Padding=new Padding(8),Text=Notice};
   plot=new Panel{Dock=DockStyle.Fill,BackColor=Color.White};plot.Paint+=delegate(object sender,PaintEventArgs e){Draw(e.Graphics,plot.ClientRectangle);};plot.Resize+=delegate{plot.Invalidate();};
   Controls.Add(plot);Controls.Add(sources);Controls.Add(bar);Controls.Add(status);Controls.Add(note);
   RefreshCurveData();
  }
  public void ApplyCurve(bool isUp,ManufacturerCurve curve){if(circuit==null)throw new InvalidOperationException("Selecione um circuito para anexar as curvas.");if(curve==null)throw new InvalidDataException("Curva ausente.");curve.Validate();var candidate=circuit.CurveStudy==null?new CurveStudyData():circuit.CurveStudy.Copy();if(isUp)candidate.Upstream=curve.Copy();else candidate.Downstream=curve.Copy();candidate.Validate();circuit.CurveStudy=candidate;upstream=candidate.Upstream;downstream=candidate.Downstream;RefreshCurveData();if(onChanged!=null)onChanged();}
  void RefreshCurveData(){sources.Text="Montante: "+(upstream==null?"não informado":upstream.Source)+" | Jusante: "+(downstream==null?"não informado":downstream.Source);var result=CurveMath.Compare(upstream,downstream);status.Text=result.Status+". "+result.Detail+" Curvas anexadas ao circuito e salvas com o projeto; CSV: CurrentA;MinSeconds;MaxSeconds.";plot.Invalidate();}
  void Import(bool isUp){using(var d=new OpenFileDialog{Filter="CSV|*.csv|Todos|*.*"})if(d.ShowDialog()==DialogResult.OK)try{ApplyCurve(isUp,ManufacturerCurve.Load(d.FileName));}catch(Exception ex){MessageBox.Show(this,ex.Message,"CSV inválido",MessageBoxButtons.OK,MessageBoxIcon.Warning);}}
  void Draw(Graphics g,Rectangle bounds){
   g.Clear(Color.White);g.SmoothingMode=SmoothingMode.AntiAlias;if(bounds.Width<200||bounds.Height<200)return;
   var area=new Rectangle(bounds.X+80,bounds.Y+55,bounds.Width-115,bounds.Height-160);
   double minI=1,maxI=10000,minT=.001,maxT=1000;
   foreach(var curve in new[]{upstream,downstream})if(curve!=null)foreach(var p in curve.Points){minI=Math.Min(minI,p.CurrentA);maxI=Math.Max(maxI,p.CurrentA);minT=Math.Min(minT,p.MinSeconds);maxT=Math.Max(maxT,p.MaxSeconds);}
   if(load>0){minI=Math.Min(minI,load/2);maxI=Math.Max(maxI,load*2);}
   double x0=Math.Floor(Math.Log10(minI)),x1=Math.Ceiling(Math.Log10(maxI)),y0=Math.Floor(Math.Log10(minT)),y1=Math.Ceiling(Math.Log10(maxT));
   Func<double,double,PointF> pos=delegate(double i,double t){return new PointF((float)(area.Left+(Math.Log10(i)-x0)/(x1-x0)*area.Width),(float)(area.Bottom-(Math.Log10(t)-y0)/(y1-y0)*area.Height));};
   using(var font=new Font("Segoe UI",8))using(var title=new Font("Segoe UI",11,FontStyle.Bold))using(var grid=new Pen(Color.Gainsboro)){
    g.DrawString("Tempo × corrente — envelopes fornecidos pelo usuário",title,Brushes.Black,bounds.Left+15,bounds.Top+10);
    for(double x=x0;x<=x1;x++){var a=pos(Math.Pow(10,x),Math.Pow(10,y0));g.DrawLine(grid,a.X,area.Top,a.X,area.Bottom);g.DrawString(Math.Pow(10,x).ToString("G3"),font,Brushes.Black,a.X-10,area.Bottom+5);}
    for(double y=y0;y<=y1;y++){var a=pos(Math.Pow(10,x0),Math.Pow(10,y));g.DrawLine(grid,area.Left,a.Y,area.Right,a.Y);g.DrawString(Math.Pow(10,y).ToString("G3"),font,Brushes.Black,bounds.Left+5,a.Y-6);}
    g.DrawRectangle(Pens.Black,area);g.DrawString("Corrente (A)",font,Brushes.Black,area.Left+area.Width/2,area.Bottom+23);g.DrawString("Tempo (s)",font,Brushes.Black,bounds.Left+5,area.Top-20);
    DrawCurve(g,upstream,Color.Firebrick,pos);DrawCurve(g,downstream,Color.RoyalBlue,pos);
    if(load>0){float x=pos(load,1).X;using(var pen=new Pen(Color.DarkGreen,1)){pen.DashStyle=DashStyle.Dash;g.DrawLine(pen,x,area.Top,x,area.Bottom);}g.DrawString("Carga "+load.ToString("F2")+" A",font,Brushes.DarkGreen,x+3,area.Top+3);}
    if(circuit!=null){PointF? last=null;using(var pen=new Pen(Color.DarkOrange,2)){for(int n=0;n<=500;n++){double i=Math.Pow(10,x0+(x1-x0)*n/500);double? t=CurveMath.CableSeconds(circuit,i);if(t.HasValue&&t.Value>=Math.Pow(10,y0)&&t.Value<=Math.Pow(10,y1)){var pt=pos(i,t.Value);if(last.HasValue)g.DrawLine(pen,last.Value,pt);last=pt;}else last=null;}}}
    string legend="Vermelho: montante "+(upstream==null?"—":upstream.Source)+" | Azul: jusante "+(downstream==null?"—":downstream.Source)+" | Laranja: limite adiabático cabo ≤5s";
    g.DrawString(legend,font,Brushes.Black,area.Left,area.Bottom+43);
    g.DrawString("Interpolação log–log nos dados importados. Sem certificação; não cobre limitação de corrente nem retaguarda.",font,Brushes.DimGray,area.Left,area.Bottom+62);
   }
  }
  static void DrawCurve(Graphics g,ManufacturerCurve c,Color color,Func<double,double,PointF> pos){if(c==null)return;using(var pen=new Pen(color,2)){for(int n=1;n<c.Points.Count;n++){var a=c.Points[n-1];var b=c.Points[n];g.DrawLine(pen,pos(a.CurrentA,a.MinSeconds),pos(b.CurrentA,b.MinSeconds));g.DrawLine(pen,pos(a.CurrentA,a.MaxSeconds),pos(b.CurrentA,b.MaxSeconds));}}}
 }
}
