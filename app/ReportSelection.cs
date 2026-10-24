using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security;
using System.Text;
using System.Windows.Forms;
namespace CircuitoStudio {
 public static class ReportSelection {
  public static string Build(Project project,IEnumerable<string> circuitIds){
   if(circuitIds==null)throw new ArgumentNullException("circuitIds");
   var ids=new HashSet<string>(circuitIds,StringComparer.Ordinal);
   return EngineeringReport.Memory(project,Engineering.Analyze(project),ids);
  }
  public static void Save(Project project,IEnumerable<string> circuitIds,string path,string format){
   format=(format??"").ToLowerInvariant();if(format!="txt"&&format!="rtf"&&format!="html"&&format!="pdf")throw new InvalidDataException("Escolha TXT, RTF, HTML ou PDF.");
   string text=Build(project,circuitIds),content=text;Encoding encoding=new UTF8Encoding(true);
   if(format=="pdf"){ReportPdf.SaveText(text,project.Name,path);return;}
   if(format=="rtf"){content=ReportPrinting.Rtf(text);encoding=Encoding.ASCII;}
   if(format=="html")content="<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><title>Relatório selecionado — "+SecurityElement.Escape(project.Name)+"</title><style>body{font:14px Segoe UI,Arial;margin:24px;color:#173047}pre{font:12px Consolas,monospace;white-space:pre-wrap;overflow-wrap:anywhere}@page{size:A4 landscape;margin:14mm}</style></head><body><h1>Relatório por seleção</h1><pre>"+SecurityElement.Escape(text)+"</pre></body></html>";
   string full=Path.GetFullPath(path),temporary=full+"."+Guid.NewGuid().ToString("N")+".tmp";
   try{File.WriteAllText(temporary,content,encoding);if(File.Exists(full))File.Replace(temporary,full,null);else File.Move(temporary,full);}finally{if(File.Exists(temporary))File.Delete(temporary);}
  }
 }
 public sealed class ReportSelectionForm:Form {
  readonly Project project;readonly CheckedListBox items=new CheckedListBox();readonly TextBox preview=new TextBox();readonly List<Circuit> circuits;readonly Label scope=new Label();
  public string PreviewText {get{return preview.Text;}}
  public string[] SelectedIds {get{return items.CheckedIndices.Cast<int>().Select(index=>circuits[index].Id).ToArray();}}
  public void SelectCircuits(IEnumerable<string> ids){var chosen=new HashSet<string>(ids,StringComparer.Ordinal);if(chosen.Any(id=>!circuits.Any(c=>c.Id==id)))throw new InvalidDataException("Circuito não encontrado.");for(int i=0;i<circuits.Count;i++)items.SetItemChecked(i,chosen.Contains(circuits[i].Id));RefreshPreview();}
  public void RefreshPreview(){preview.Text=ReportSelection.Build(project,SelectedIds);scope.Text=SelectedIds.Length+" circuitos selecionados. Totais e condições elétricas do projeto completo.";}
  public ReportSelectionForm(Project p){if(p==null)throw new ArgumentNullException("p");p.Validate();project=p;circuits=p.Boards.SelectMany(b=>b.Circuits).ToList();Text="Relatório por seleção";Font=new Font("Segoe UI",10);ClientSize=new Size(1100,760);MinimumSize=new Size(850,600);StartPosition=FormStartPosition.CenterParent;
   var split=new SplitContainer{Dock=DockStyle.Fill,Width=1080,SplitterDistance=320};items.Dock=DockStyle.Fill;items.CheckOnClick=true;items.HorizontalScrollbar=true;foreach(var board in p.Boards)foreach(var circuit in board.Circuits)items.Items.Add(board.Name+" / "+circuit.Id+" — "+circuit.Name,true);split.Panel1.Controls.Add(items);preview.Dock=DockStyle.Fill;preview.Multiline=true;preview.ReadOnly=true;preview.WordWrap=false;preview.ScrollBars=ScrollBars.Both;preview.Font=new Font("Consolas",9);split.Panel2.Controls.Add(preview);
   var bar=new FlowLayoutPanel{Dock=DockStyle.Top,Height=100,Padding=new Padding(8)};Action<string,Action> button=(label,action)=>{var control=new Button{Text=label,AutoSize=true,Height=32};control.Click+=delegate{try{action();}catch(Exception ex){MessageBox.Show(this,ex.Message,"Relatório");}};bar.Controls.Add(control);};
   button("Todos",delegate{SelectCircuits(circuits.Select(c=>c.Id));});button("Nenhum",delegate{SelectCircuits(new string[0]);});button("Atualizar prévia",RefreshPreview);button("Salvar TXT",delegate{Save("txt");});button("Salvar RTF",delegate{Save("rtf");});button("Salvar HTML",delegate{Save("html");});button("Salvar PDF",delegate{Save("pdf");});button("Prévia / imprimir",delegate{RefreshPreview();ReportPrinting.PreviewText(preview.Text,project.Name,this);});scope.Dock=DockStyle.Bottom;scope.Height=45;scope.Padding=new Padding(8);Controls.Add(split);Controls.Add(scope);Controls.Add(bar);items.ItemCheck+=delegate{scope.Text="Seleção alterada. Atualize a prévia; salvar e imprimir usam a seleção atual.";};RefreshPreview();
  }
  void Save(string format){using(var dialog=new SaveFileDialog{Filter=format.ToUpperInvariant()+"|*."+format,FileName="relatorio-selecionado."+format})if(dialog.ShowDialog(this)==DialogResult.OK){RefreshPreview();ReportSelection.Save(project,SelectedIds,dialog.FileName,format);}}
 }
}
