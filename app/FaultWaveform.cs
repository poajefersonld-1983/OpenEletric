using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
namespace CircuitoStudio {
 public static class FaultWaveform {
  public const string Header="Tempo_s;CorrenteInstantanea_kA;EnvelopeCCmax_kA;Frequencia_Hz;IkSimetrico_kA;IpEstimado_kA;AnguloFechamento_graus;FPcc;Kappa;Tau_s;Metodo";
  public static int Intervals(FaultDynamics model,double durationSeconds){if(model==null)throw new InvalidDataException("Dinâmica de curto indisponível.");Project.Range(durationSeconds,.001,5,"duração da forma de onda");return (int)Math.Min(50000,Math.Max(200,Math.Ceiling(durationSeconds*model.FrequencyHz*100)));}
  static string N(double number){return number.ToString("R",CultureInfo.InvariantCulture);}
  public static void ExportCsv(FaultDynamics model,double durationSeconds,double angleDegrees,string path){
   int intervals=Intervals(model,durationSeconds);model.InstantaneousKA(0,angleDegrees);
   string full=Path.GetFullPath(path),temporary=full+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{using(var writer=new StreamWriter(temporary,false,new UTF8Encoding(true))){writer.WriteLine(Header);for(int i=0;i<=intervals;i++){double t=durationSeconds*i/intervals;writer.WriteLine(String.Join(";",new[]{N(t),N(model.InstantaneousKA(t,angleDegrees)),N(model.DcEnvelopeKA(t)),N(model.FrequencyHz),N(model.SymmetricKA),N(model.PeakKA),N(angleDegrees),N(model.PowerFactor),N(model.Kappa),model.TimeConstantSeconds.HasValue?N(model.TimeConstantSeconds.Value):"","RL_RADIAL_SEM_LIMITACAO"}));}}
    if(File.Exists(full))File.Replace(temporary,full,null);else File.Move(temporary,full);
   }finally{if(File.Exists(temporary))File.Delete(temporary);}
  }
 }
 public sealed class FaultWaveformForm:Form {
  readonly CircuitResult result;readonly ComboBox location=new ComboBox();readonly NumericUpDown duration=new NumericUpDown(),angle=new NumericUpDown();readonly Label summary=new Label();readonly Panel drawing=new Panel();readonly Button export=new Button();
  public FaultDynamics SelectedModel {get{return location.SelectedIndex==1?result.FaultEndDynamics:result.FaultOriginDynamics;}}
  public FaultWaveformForm(Project project,Board board,Circuit circuit){
   if(project==null||board==null||circuit==null||!project.Boards.Contains(board)||!board.Circuits.Contains(circuit))throw new InvalidDataException("Selecione um circuito do projeto.");
   result=Engineering.Analyze(project).For(circuit);Text="Forma de onda de curto — "+circuit.Id;Font=new Font("Segoe UI",10);ClientSize=new Size(1000,670);MinimumSize=new Size(780,570);StartPosition=FormStartPosition.CenterParent;
   var controls=new FlowLayoutPanel{Dock=DockStyle.Top,Height=86,Padding=new Padding(10),AutoSize=false};location.DropDownStyle=ComboBoxStyle.DropDownList;location.Width=225;location.Items.AddRange(new object[]{"Barramento de origem (3F)","Fim do circuito (3F)"});location.SelectedIndex=0;duration.Minimum=1;duration.Maximum=5000;duration.Value=100;duration.Width=85;angle.Minimum=-180;angle.Maximum=180;angle.DecimalPlaces=1;angle.Width=85;export.Text="Exportar CSV";export.Width=120;export.Height=30;
   foreach(Control item in new Control[]{location,new Label{Text="Duração (ms)",AutoSize=true,Padding=new Padding(6)},duration,new Label{Text="Ângulo (°)",AutoSize=true,Padding=new Padding(6)},angle,export})controls.Controls.Add(item);
   summary.Dock=DockStyle.Top;summary.Height=78;summary.Padding=new Padding(12,4,12,4);summary.AutoEllipsis=true;
   var note=new Label{Dock=DockStyle.Bottom,Height=64,Padding=new Padding(12),Text="Azul: corrente instantânea para o ângulo escolhido. Laranja: envelope CC máximo, independente desse ângulo. Modelo radial R-L com componente simétrica constante, sem limitação por proteção ou dinâmica de máquinas; não representa energia passante ou tempo de desligamento."};
   drawing.Dock=DockStyle.Fill;drawing.BackColor=Color.White;drawing.Paint+=Draw;drawing.Resize+=delegate{drawing.Invalidate();};Controls.Add(drawing);Controls.Add(summary);Controls.Add(controls);Controls.Add(note);
   location.SelectedIndexChanged+=delegate{RefreshModel();};duration.ValueChanged+=delegate{RefreshModel();};angle.ValueChanged+=delegate{RefreshModel();};export.Click+=delegate{try{using(var dialog=new SaveFileDialog{Filter="Forma de onda CSV|*.csv",FileName="curto-RL.csv"})if(dialog.ShowDialog(this)==DialogResult.OK)FaultWaveform.ExportCsv(SelectedModel,(double)duration.Value/1000,(double)angle.Value,dialog.FileName);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Exportação da forma de onda");}};RefreshModel();
  }
  void RefreshModel(){var model=SelectedModel;export.Enabled=model!=null;summary.Text=model==null?"Dinâmica indisponível neste ponto. Confira alimentação, impedâncias e tipo do circuito; o fim monofásico não utiliza o modelo de curto 3F.":"Ik simétrico: "+model.SymmetricKA.ToString("F3")+" kA   |   pico estimado: "+model.PeakKA.ToString("F3")+" kA   |   FPcc: "+model.PowerFactor.ToString("F4")+"\r\nFrequência: "+model.FrequencyHz+" Hz   |   tau: "+(model.Undamped?"sem amortecimento (R=0)":(model.TimeConstantSeconds.Value*1000).ToString("F3")+" ms")+". O pico estimado é independente do ângulo escolhido para a forma de onda.";drawing.Invalidate();}
  void Draw(object sender,PaintEventArgs args){var model=SelectedModel;if(model==null||drawing.Width<150||drawing.Height<100)return;double seconds=(double)duration.Value/1000,closing=(double)angle.Value;int count=FaultWaveform.Intervals(model,seconds);var wave=new PointF[count+1];var envelope=new PointF[count+1];double bound=Math.Max(1e-9,Math.Sqrt(2)*model.SymmetricKA*2);RectangleF plot=new RectangleF(76,24,drawing.Width-100,drawing.Height-65);float middle=plot.Top+plot.Height/2;args.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
   for(int i=0;i<=count;i++){double t=seconds*i/count;float x=plot.Left+plot.Width*i/count;wave[i]=new PointF(x,middle-(float)(model.InstantaneousKA(t,closing)/bound)*plot.Height/2);envelope[i]=new PointF(x,middle-(float)(model.DcEnvelopeKA(t)/bound)*plot.Height/2);}
   args.Graphics.DrawRectangle(Pens.LightGray,plot.X,plot.Y,plot.Width,plot.Height);args.Graphics.DrawLine(Pens.Gray,plot.Left,middle,plot.Right,middle);using(var blue=new Pen(Color.RoyalBlue,1.4f))using(var orange=new Pen(Color.DarkOrange,1.4f)){args.Graphics.DrawLines(blue,wave);args.Graphics.DrawLines(orange,envelope);}args.Graphics.DrawString(bound.ToString("G4")+" kA",Font,Brushes.DimGray,4,plot.Top);args.Graphics.DrawString("0",Font,Brushes.DimGray,40,middle);args.Graphics.DrawString((-bound).ToString("G4"),Font,Brushes.DimGray,4,plot.Bottom-18);args.Graphics.DrawString("0 ms",Font,Brushes.DimGray,plot.Left,plot.Bottom+8);args.Graphics.DrawString(duration.Value+" ms",Font,Brushes.DimGray,plot.Right-70,plot.Bottom+8);
  }
 }
}
