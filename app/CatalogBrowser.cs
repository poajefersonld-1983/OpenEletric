using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
namespace CircuitoStudio {
 public sealed partial class CatalogForm {
  readonly TextBox manufacturerFilter=new TextBox(),familyFilter=new TextBox(),modelFilter=new TextBox(),itemDetails=new TextBox();
  CircuitResult context;
  CatalogQuery Query {get{return new CatalogQuery{Text=search.Text,Manufacturer=manufacturerFilter.Text,Family=familyFilter.Text,Model=modelFilter.Text};}}
  public string ItemDetails {get{return itemDetails.Text;}}
  public int VisibleItemCount {get{return grid.Rows.Count;}}
  public void ApplyFilters(string manufacturer,string family,string model,string text){manufacturerFilter.Text=manufacturer??"";familyFilter.Text=family??"";modelFilter.Text=model??"";search.Text=text??"";RefreshRows();}
  void RefreshDetails(){itemDetails.Text=CatalogSearch.Describe(Current(),Query,context);}
  void InitializeBrowser(Project project,Circuit circuit){
   if(circuit!=null){if(project==null||!project.Boards.Any(board=>board.Circuits.Contains(circuit)))throw new InvalidDataException("O circuito não pertence ao projeto.");context=Engineering.Analyze(project).For(circuit);}
   Text="Biblioteca de equipamentos";Size=new Size(1200,820);MinimumSize=new Size(950,650);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);BackColor=Color.FromArgb(238,244,248);
   var actions=new FlowLayoutPanel{Dock=DockStyle.Top,Height=108,Padding=new Padding(10),BackColor=Color.FromArgb(20,39,60)};search=new TextBox{Width=220};search.TextChanged+=delegate{RefreshRows();};actions.Controls.Add(new Label{Text="Buscar",ForeColor=Color.White,AutoSize=true,Padding=new Padding(0,6,0,0)});actions.Controls.Add(search);Add(actions,"Importar planilha",Import);Add(actions,"Exportar XLSX/CSV",Export);Add(actions,"Modelo vazio",ExportTemplate);Add(actions,"Novo item",delegate{Edit(null);});Add(actions,"Editar",delegate{var selected=Current();if(selected!=null)Edit(selected);});Add(actions,"Excluir",Delete);Add(actions,"Salvar",Save);Add(actions,"Usar item",Use);
   var filters=new FlowLayoutPanel{Dock=DockStyle.Top,Height=60,Padding=new Padding(10)};
   foreach(var field in new[]{new {Name="Fabricante",Control=manufacturerFilter},new {Name="Família",Control=familyFilter},new {Name="Modelo",Control=modelFilter}}){filters.Controls.Add(new Label{Text=field.Name,AutoSize=true,Padding=new Padding(0,5,0,0)});field.Control.Width=160;field.Control.TextChanged+=delegate{RefreshRows();};filters.Controls.Add(field.Control);}
   var split=new SplitContainer{Dock=DockStyle.Fill,Orientation=Orientation.Horizontal,Height=500,SplitterDistance=290,Panel2MinSize=125,FixedPanel=FixedPanel.Panel2};grid=new DataGridView{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,AutoGenerateColumns=true,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,BackgroundColor=Color.White,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.DisplayedCells};grid.SelectionChanged+=delegate{RefreshDetails();};split.Panel1.Controls.Add(grid);itemDetails.Dock=DockStyle.Fill;itemDetails.Multiline=true;itemDetails.ReadOnly=true;itemDetails.ScrollBars=ScrollBars.Vertical;itemDetails.WordWrap=true;split.Panel2.Controls.Add(itemDetails);
   var note=new Label{Dock=DockStyle.Bottom,Height=74,Padding=new Padding(10),Text="Modelos de 15 colunas incluem família, modelo, versão e ficha; os modelos antigos de 11 colunas continuam legíveis. Fonte e ficha são referências informadas, sem abertura automática. Comparações são preliminares; confira os dados do fabricante."};Controls.Add(split);Controls.Add(note);Controls.Add(filters);Controls.Add(actions);RefreshRows();FormClosing+=delegate(object sender,FormClosingEventArgs args){if(dirty&&MessageBox.Show(this,"Existem alterações não salvas. Fechar sem salvar?","Biblioteca",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)args.Cancel=true;};
  }
 }
}
