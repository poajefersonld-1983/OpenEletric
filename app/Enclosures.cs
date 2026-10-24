using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Security;
using System.Text;
using System.Windows.Forms;
namespace CircuitoStudio {
 [DataContract, TypeConverter(typeof(ExpandableObjectConverter))] public sealed class EnclosureSpec {
  [DataMember(Order=0),DisplayName("Largura (mm)")] public double WidthMm {get;set;}
  [DataMember(Order=1),DisplayName("Altura (mm)")] public double HeightMm {get;set;}
  [DataMember(Order=2),DisplayName("Trilhos")] public int Rows {get;set;}
  [DataMember(Order=3),DisplayName("Módulos por trilho")] public int ModulesPerRow {get;set;}
  [DataMember(Order=4),DisplayName("Passo do módulo (mm)")] public double ModulePitchMm {get;set;}
  [DataMember(Order=5),DisplayName("Passo dos trilhos (mm)")] public double RowPitchMm {get;set;}
  [DataMember(Order=6),DisplayName("Fabricante informado")] public string Manufacturer {get;set;}
  [DataMember(Order=7),DisplayName("Referência informada")] public string Reference {get;set;}
  public EnclosureSpec(){WidthMm=600;HeightMm=800;Rows=4;ModulesPerRow=24;ModulePitchMm=18;RowPitchMm=150;Manufacturer=Reference="";}
  public EnclosureSpec Copy(){return (EnclosureSpec)MemberwiseClone();}
  public override string ToString(){return WidthMm+" × "+HeightMm+" mm";}
 }
 public sealed partial class Board {
  private EnclosureSpec enclosure;
  [DataMember(Order=50),DisplayName("Invólucro / vista frontal"),Category("Montagem")] public EnclosureSpec Enclosure {get {return enclosure??(enclosure=new EnclosureSpec());} set {enclosure=value;}}
 }
 public sealed partial class Circuit {
  [DataMember(Order=60),DisplayName("Módulos DIN (0 = 2)"),Category("Vista frontal")] public int DinModules {get;set;}
  [DataMember(Order=61),DisplayName("Trilho DIN (0 = automático)"),Category("Vista frontal")] public int DinRow {get;set;}
  [DataMember(Order=62),DisplayName("Coluna DIN (0 = automático)"),Category("Vista frontal")] public int DinColumn {get;set;}
 }
 public static class Enclosures {
  public static EnclosureSpec Generic(int rows,int modulesPerRow){var e=new EnclosureSpec{Rows=rows,ModulesPerRow=modulesPerRow,WidthMm=modulesPerRow*18+80,HeightMm=(rows-1)*150+190,Manufacturer="",Reference="Geometria genérica "+rows+" × "+modulesPerRow};if(!SpecValid(e))throw new InvalidDataException("Dimensões genéricas inválidas.");return e;}
  public static void ApplySpec(Board b,EnclosureSpec spec){if(b==null||spec==null||!SpecValid(spec))throw new InvalidDataException("Invólucro inválido.");Project.Text(spec.Manufacturer,"fabricante do invólucro",false);Project.Text(spec.Reference,"referência do invólucro",false);b.Enclosure=spec.Copy();}
  public static bool AutoArrange(Board b){if(b==null||!SpecValid(b.Enclosure))throw new InvalidDataException("Corrija o invólucro antes de distribuir.");bool changed=false;foreach(var c in b.Circuits){if(c.DinRow!=0||c.DinColumn!=0)changed=true;c.DinRow=c.DinColumn=0;}return changed;}
  public sealed class Placement {public Circuit Circuit;public int Row,Column,Modules;public bool Placed;}
  static bool Positive(double x){return !Double.IsNaN(x)&&!Double.IsInfinity(x)&&x>0&&x<=100000;}
  internal static bool SpecValid(EnclosureSpec e){return Positive(e.WidthMm)&&Positive(e.HeightMm)&&Positive(e.ModulePitchMm)&&Positive(e.RowPitchMm)&&e.Rows>0&&e.Rows<=100&&e.ModulesPerRow>0&&e.ModulesPerRow<=1000;}
  public static List<Placement> Layout(Board b){
   List<Placement> result=new List<Placement>(); EnclosureSpec e=b.Enclosure;if(!SpecValid(e)||b.Circuits==null)return result;
   bool[,] used=new bool[e.Rows,e.ModulesPerRow];
   // Reserve manual placements before first-fit automatic packing, independent of list ordering.
   foreach(Circuit c in b.Circuits){Placement p=new Placement{Circuit=c,Modules=c.DinModules<=0?2:c.DinModules,Row=c.DinRow,Column=c.DinColumn};result.Add(p);
    if(c.DinRow>0&&c.DinColumn>0&&p.Modules<=e.ModulesPerRow&&p.Row<=e.Rows&&p.Column<=e.ModulesPerRow-p.Modules+1){p.Placed=true;for(int j=0;j<p.Modules;j++)used[p.Row-1,p.Column-1+j]=true;}}
   foreach(Placement p in result){if(p.Circuit.DinRow!=0||p.Circuit.DinColumn!=0)continue;if(p.Modules>e.ModulesPerRow)continue;
    for(int r=0;r<e.Rows&&!p.Placed;r++)for(int col=0;col<=e.ModulesPerRow-p.Modules&&!p.Placed;col++){bool free=true;for(int j=0;j<p.Modules;j++)if(used[r,col+j])free=false;if(!free)continue;p.Row=r+1;p.Column=col+1;p.Placed=true;for(int j=0;j<p.Modules;j++)used[r,col+j]=true;}}
   return result;
  }
  public static List<string> Validate(Board b){List<string>w=new List<string>();EnclosureSpec e=b.Enclosure;if(!SpecValid(e)){w.Add("Dimensões ou capacidade do invólucro inválidas (máximo 100 trilhos e 1000 módulos/trilho).");return w;}
   if(e.ModulesPerRow*e.ModulePitchMm>e.WidthMm-40)w.Add("A largura útil dos trilhos ultrapassa a largura do invólucro com margens de 20 mm.");
   if((e.Rows-1)*e.RowPitchMm+110>e.HeightMm-40)w.Add("Os trilhos/dispositivos ultrapassam a altura do invólucro com margens de 20 mm.");
   Dictionary<string,string> occupied=new Dictionary<string,string>();long total=0;
   foreach(Placement p in Layout(b)){total+=p.Modules;string label=p.Circuit.Id+" / "+p.Circuit.Name;
    if(p.Circuit.DinModules<0||p.Circuit.DinRow<0||p.Circuit.DinColumn<0)w.Add(label+": valores DIN negativos.");
    if(!p.Placed){w.Add(label+": posição inválida ou sem espaço; informe trilho e coluna juntos, ou ambos zero.");continue;}
    for(int j=0;j<p.Modules;j++){string key=p.Row+":"+(p.Column+j);if(occupied.ContainsKey(key))w.Add(label+": sobreposição no trilho "+p.Row+", coluna "+(p.Column+j)+" com "+occupied[key]+".");else occupied[key]=label;}}
   if(total>(long)e.Rows*e.ModulesPerRow)w.Add("Quantidade de módulos excede a capacidade total do quadro.");return w;
  }
  static string X(string s){return SecurityElement.Escape(s??"");}
  static string N(double d){return d.ToString("0.###",CultureInfo.InvariantCulture);}
  public static string Svg(Project project,Board b,string title){EnclosureSpec e=b.Enclosure;if(!SpecValid(e))throw new InvalidDataException("Corrija as dimensões do invólucro antes de exportar.");StringBuilder s=new StringBuilder();
   s.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\""+N(e.WidthMm)+"mm\" height=\""+N(e.HeightMm+120)+"mm\" viewBox=\"0 0 "+N(e.WidthMm)+" "+N(e.HeightMm+120)+"\"><title>"+X(title)+"</title><rect x=\"1\" y=\"1\" width=\""+N(e.WidthMm-2)+"\" height=\""+N(e.HeightMm-2)+"\" fill=\"#fafafa\" stroke=\"#333\"/>");
   double left=(e.WidthMm-e.ModulesPerRow*e.ModulePitchMm)/2;
   for(int r=0;r<e.Rows;r++){double y=70+r*e.RowPitchMm;s.Append("<path d=\"M "+N(left)+" "+N(y)+" h "+N(e.ModulesPerRow*e.ModulePitchMm)+"\" stroke=\"#999\" stroke-width=\"8\"/>");}
   foreach(Placement p in Layout(b))if(p.Placed){double x=left+(p.Column-1)*e.ModulePitchMm,y=30+(p.Row-1)*e.RowPitchMm;s.Append("<g><title>"+X(p.Circuit.Name)+"</title><rect x=\""+N(x)+"\" y=\""+N(y)+"\" width=\""+N(Math.Max(.1,p.Modules*e.ModulePitchMm-1))+"\" height=\"80\" fill=\"#dceaf7\" stroke=\"#345\"/><text x=\""+N(x+2)+"\" y=\""+N(y+48)+"\" font-size=\"11\">"+X(p.Circuit.Id)+"</text></g>");}
   s.Append("<text x=\"10\" y=\""+N(e.HeightMm+25)+"\" font-size=\"14\">"+X((project==null?"":project.Name)+" — "+b.Name)+"</text><text x=\"10\" y=\""+N(e.HeightMm+48)+"\" font-size=\"11\">Vista geométrica genérica; sem certificação térmica, IP ou montagem.</text><text x=\"10\" y=\""+N(e.HeightMm+70)+"\" font-size=\"11\">Avisos de ocupação: "+Validate(b).Count+". Confira no aplicativo antes de montar.</text></svg>");return s.ToString();}
 }
 public sealed class EnclosureForm:Form {
  readonly Project project;readonly Board board;readonly Action onChanged;readonly Panel drawing=new Panel();readonly TextBox warnings=new TextBox();readonly PropertyGrid grid=new PropertyGrid();public bool Changed{get;private set;}
  public void ApplySpec(EnclosureSpec spec){Enclosures.ApplySpec(board,spec);grid.SelectedObject=board.Enclosure;NotifyChange();}
  public void AutoArrange(){if(Enclosures.AutoArrange(board)){grid.Refresh();NotifyChange();}else RefreshView();}
  void NotifyChange(){Changed=true;RefreshView();if(onChanged!=null)onChanged();}
  public EnclosureForm(Project p,Board b,Action changed=null){if(p==null||b==null||!p.Boards.Contains(b))throw new InvalidDataException("Selecione um quadro do projeto.");project=p;board=b;onChanged=changed;Text="Vista frontal — "+b.Name;Width=1100;Height=800;StartPosition=FormStartPosition.CenterParent;
   ToolStrip tools=new ToolStrip();ToolStripButton edit=new ToolStripButton("Editar invólucro"),export=new ToolStripButton("Exportar SVG");tools.Items.Add(edit);tools.Items.Add(export);var catalogue=new ToolStripButton("Catálogo real / frontalDIN");tools.Items.Add(catalogue);catalogue.Click+=delegate{using(var form=new EnclosureCatalogForm(project,board,NotifyChange))form.ShowDialog(this);};edit.Click+=delegate{grid.SelectedObject=board.Enclosure;};export.Click+=delegate{try{using(SaveFileDialog d=new SaveFileDialog{Filter="Desenho SVG|*.svg",FileName="Vista frontal.svg"})if(d.ShowDialog(this)==DialogResult.OK)File.WriteAllText(d.FileName,(board.EnclosureCatalogSelection==null?Enclosures.Svg(project,board,Text):EnclosureCatalog.Svg(project,board,Text)),Encoding.UTF8);}catch(Exception ex){MessageBox.Show(this,ex.Message,"Exportação");}};
   ToolStripDropDownButton generic=new ToolStripDropDownButton("Selecionar geometria");foreach(int rows in new[]{1,2,3,4})foreach(int modules in new[]{12,24}){EnclosureSpec spec=Enclosures.Generic(rows,modules);generic.DropDownItems.Add(rows+" trilho(s) × "+modules+" módulos",null,delegate{ApplySpec(spec);});}tools.Items.Add(generic);ToolStripButton arrange=new ToolStripButton("Distribuir automaticamente");tools.Items.Add(arrange);arrange.Click+=delegate{AutoArrange();};
   SplitContainer split=new SplitContainer{Dock=DockStyle.Fill,Width=1000,SplitterDistance=700};drawing.Dock=DockStyle.Fill;drawing.AutoScroll=true;drawing.BackColor=Color.White;drawing.Paint+=PaintFront;split.Panel1.Controls.Add(drawing);grid.Dock=DockStyle.Fill;grid.SelectedObject=board.Enclosure;grid.PropertyValueChanged+=delegate{NotifyChange();};split.Panel2.Controls.Add(grid);
   ComboBox pick=new ComboBox{Dock=DockStyle.Top,DropDownStyle=ComboBoxStyle.DropDownList};pick.Items.Add("Invólucro");foreach(Circuit c in board.Circuits)pick.Items.Add(c);pick.SelectedIndex=0;pick.SelectedIndexChanged+=delegate{grid.SelectedObject=pick.SelectedIndex==0?(object)board.Enclosure:pick.SelectedItem;};split.Panel2.Controls.Add(pick);
   warnings.Dock=DockStyle.Bottom;warnings.Height=110;warnings.Multiline=true;warnings.ReadOnly=true;warnings.ScrollBars=ScrollBars.Vertical;Controls.Add(split);Controls.Add(warnings);Controls.Add(tools);tools.Dock=DockStyle.Top;RefreshView();}
  void RefreshView(){warnings.Text=(board.EnclosureCatalogSelection==null?"Vista geométrica genérica. Não verifica dissipação térmica, IP ou certificação de montagem.":EnclosureCatalog.Evaluate(project,board).Status+". Frontal esquemática; confirmar montagem/dissipação/IP.")+"\r\n"+String.Join("\r\n",Enclosures.Validate(board).ToArray());drawing.Invalidate();}
  void PaintFront(object sender,PaintEventArgs args){EnclosureSpec e=board.Enclosure;if(!Enclosures.SpecValid(e))return;float scale=.65f;drawing.AutoScrollMinSize=new Size((int)(e.WidthMm*scale)+40,(int)(e.HeightMm*scale)+40);Graphics g=args.Graphics;g.TranslateTransform(drawing.AutoScrollPosition.X+20,drawing.AutoScrollPosition.Y+20);g.ScaleTransform(scale,scale);g.DrawRectangle(Pens.Black,0,0,(float)e.WidthMm,(float)e.HeightMm);double left=(e.WidthMm-e.ModulesPerRow*e.ModulePitchMm)/2;
   using(Font font=new Font("Segoe UI",12)) {for(int r=0;r<Math.Min(100,Math.Max(0,e.Rows));r++){float y=(float)(70+r*e.RowPitchMm);g.DrawLine(Pens.Gray,(float)left,y,(float)(left+e.ModulesPerRow*e.ModulePitchMm),y);}foreach(Enclosures.Placement p in Enclosures.Layout(board))if(p.Placed){float x=(float)(left+(p.Column-1)*e.ModulePitchMm),y=(float)(30+(p.Row-1)*e.RowPitchMm),w=(float)Math.Max(4,p.Modules*e.ModulePitchMm-1);g.FillRectangle(Brushes.LightSteelBlue,x,y,w,80);g.DrawRectangle(Pens.DarkSlateGray,x,y,w,80);g.DrawString(p.Circuit.Id,font,Brushes.Black,new RectangleF(x+2,y+28,w-3,48));}}}
 }
}


