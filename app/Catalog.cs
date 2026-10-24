using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Windows.Forms;
namespace CircuitoStudio {
[DataContract] public sealed partial class Equipment {
[DataMember,DisplayName("Fabricante")] public string Manufacturer {get;set;}
[DataMember,DisplayName("Referência")] public string Reference {get;set;}
[DataMember,DisplayName("Descrição")] public string Description {get;set;}
[DataMember,DisplayName("Categoria")] public string Category {get;set;}
[DataMember,DisplayName("Fonte dos dados")] public string Source {get;set;}
[DataMember,DisplayName("Corrente nominal (A)")] public double RatedA {get;set;}
[DataMember,DisplayName("Interrupção (kA)")] public double BreakingKA {get;set;}
[DataMember,DisplayName("Corrente convencional I2 (A)")] public double TripI2A {get;set;}
[DataMember,DisplayName("Energia passante (A²s)")] public double LetThroughA2s {get;set;}
[DataMember,DisplayName("Disparo magnético (A)")] public double MagneticA {get;set;}
[DataMember,DisplayName("Sensibilidade DR (mA)")] public double RcdMilliA {get;set;}
public Equipment(){Manufacturer=Reference=Description=Category=Source="";}
public Equipment Copy(){return (Equipment)MemberwiseClone();}
public void Validate(){ string[] ss={Manufacturer,Reference,Description,Category,Source,Family,Model,DataVersion,DataSheet}; foreach(string s in ss) if(s==null||s.Length>2000||s.IndexOf('\0')>=0)throw new InvalidDataException("Texto inválido ou maior que 2000 caracteres."); if(String.IsNullOrWhiteSpace(Reference))throw new InvalidDataException("Informe a referência."); double[] ns={RatedA,BreakingKA,TripI2A,LetThroughA2s,MagneticA,RcdMilliA}; foreach(double n in ns)if(Double.IsNaN(n)||Double.IsInfinity(n)||n<0)throw new InvalidDataException("Valores devem ser números finitos não negativos; use zero para desconhecido."); }
}
[DataContract] public sealed class CatalogData {
public const string Header="Manufacturer;Reference;Description;Category;RatedA;BreakingKA;TripI2A;LetThroughA2s;MagneticA;RcdMilliA;Source";
public const string ExtendedHeader=Header+";Family;Model;DataVersion;DataSheet";
[DataMember] public List<Equipment> Items {get;set;}
public CatalogData(){Items=new List<Equipment>();}
public void Validate(){if(Items==null||Items.Count>100000)throw new InvalidDataException("Catálogo inválido.");var keys=new HashSet<string>(StringComparer.OrdinalIgnoreCase);foreach(Equipment e in Items){if(e==null)throw new InvalidDataException("Item inválido.");e.Validate();string key=e.Manufacturer.Trim()+"\0"+e.Reference.Trim();if(!keys.Add(key))throw new InvalidDataException("Referência duplicada: "+e.Manufacturer+" / "+e.Reference);}}
public static CatalogData Load(string file){using(var s=File.OpenRead(file)){if(s.Length>32*1024*1024)throw new InvalidDataException("Biblioteca maior que 32 MiB.");if(s.Length>=3){bool bom=s.ReadByte()==239&&s.ReadByte()==187&&s.ReadByte()==191;s.Position=bom?3:0;}var d=(CatalogData)new DataContractJsonSerializer(typeof(CatalogData)).ReadObject(s);if(d==null)throw new InvalidDataException("Biblioteca ausente.");d.Validate();return d;}}
public void Save(string file){Validate();string full=Path.GetFullPath(file);Directory.CreateDirectory(Path.GetDirectoryName(full));string tmp=full+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var s=new FileStream(tmp,FileMode.CreateNew,FileAccess.Write,FileShare.None)){new DataContractJsonSerializer(typeof(CatalogData)).WriteObject(s,this);if(s.Length>32*1024*1024)throw new InvalidDataException("Biblioteca excede 32 MiB; arquivo anterior preservado.");s.Flush(true);}if(File.Exists(full))File.Replace(tmp,full,full+".bak");else File.Move(tmp,full);}finally{if(File.Exists(tmp))File.Delete(tmp);}}
static double Number(string text){if(String.IsNullOrWhiteSpace(text))return 0; text=text.Trim(); if(text.IndexOf('.')>=0&&text.IndexOf(',')>=0)throw new InvalidDataException("Número ambíguo: "+text);double n; if(!Double.TryParse(text.Replace(',','.'),NumberStyles.AllowLeadingSign|NumberStyles.AllowDecimalPoint|NumberStyles.AllowExponent,CultureInfo.InvariantCulture,out n)||Double.IsNaN(n)||Double.IsInfinity(n)||n<0)throw new InvalidDataException("Número inválido: "+text);return n;}
internal static List<string[]> Rows(string text){var rows=new List<string[]>();var cells=new List<string>();var cell=new StringBuilder();bool quoted=false,closed=false,start=true;for(int i=0;i<text.Length;i++){char c=text[i];if(quoted){if(c=='"'){if(i+1<text.Length&&text[i+1]=='"'){cell.Append('"');i++;}else{quoted=false;closed=true;}}else cell.Append(c);continue;}if(c=='"'){if(!start||closed)throw new InvalidDataException("Aspas inválidas no CSV.");quoted=true;start=false;continue;}if(c==';'||c=='\r'||c=='\n'){cells.Add(cell.ToString());cell.Length=0;start=true;closed=false;if(c!=';'){if(c=='\r'&&i+1<text.Length&&text[i+1]=='\n')i++;rows.Add(cells.ToArray());cells.Clear();}continue;}if(closed)throw new InvalidDataException("Caracteres após aspas no CSV.");cell.Append(c);start=false;}if(quoted)throw new InvalidDataException("Campo com aspas não terminado.");if(cell.Length>0||cells.Count>0||closed){cells.Add(cell.ToString());rows.Add(cells.ToArray());}return rows;}
static CatalogData ImportRows(List<string[]> rows){if(rows.Count>100000)throw new InvalidDataException("A tabela excede 100000 linhas.");string header=rows.Count==0?"":String.Join(";",rows[0]);bool extended=header==ExtendedHeader;if(header!=Header&&!extended)throw new InvalidDataException("Cabeçalho incompatível. Exporte um modelo vazio.");int width=extended?15:11;var data=new CatalogData();for(int i=1;i<rows.Count;i++){string[] row=rows[i];if(row.Length!=width)throw new InvalidDataException("Registro "+(i+1)+": esperadas "+width+" colunas.");try{data.Items.Add(new Equipment{Manufacturer=row[0],Reference=row[1],Description=row[2],Category=row[3],RatedA=Number(row[4]),BreakingKA=Number(row[5]),TripI2A=Number(row[6]),LetThroughA2s=Number(row[7]),MagneticA=Number(row[8]),RcdMilliA=Number(row[9]),Source=row[10],Family=extended?row[11]:"",Model=extended?row[12]:"",DataVersion=extended?row[13]:"",DataSheet=extended?row[14]:""});}catch(Exception ex){throw new InvalidDataException("Registro "+(i+1)+": "+ex.Message,ex);}}data.Validate();return data;}
public static CatalogData ImportCsv(string file){if(new FileInfo(file).Length>32*1024*1024)throw new InvalidDataException("Arquivo CSV maior que 32 MiB.");return ImportRows(Rows(File.ReadAllText(file,Encoding.UTF8).TrimStart('\uFEFF')));}
public static CatalogData ImportXlsx(string file){return ImportRows(XlsxWorkbook.ReadRows(file));}
public static CatalogData ImportXls(string file){return ImportRows(LegacyXls.ReadRows(file));}
public static CatalogData ImportTable(string file){switch(Path.GetExtension(file).ToLowerInvariant()){case ".xls":return ImportXls(file);case ".xlsx":return ImportXlsx(file);case ".csv":return ImportCsv(file);default:throw new InvalidDataException("Escolha um arquivo CSV, XLS ou XLSX.");}}
static string Quote(string s){return "\""+s.Replace("\"","\"\"")+"\"";}
static List<string[]> ExportRows(CatalogData data){data.Validate();var rows=new List<string[]>{ExtendedHeader.Split(';')};foreach(var e in data.Items)rows.Add(new[]{e.Manufacturer,e.Reference,e.Description,e.Category,e.RatedA.ToString("R",CultureInfo.InvariantCulture),e.BreakingKA.ToString("R",CultureInfo.InvariantCulture),e.TripI2A.ToString("R",CultureInfo.InvariantCulture),e.LetThroughA2s.ToString("R",CultureInfo.InvariantCulture),e.MagneticA.ToString("R",CultureInfo.InvariantCulture),e.RcdMilliA.ToString("R",CultureInfo.InvariantCulture),e.Source,e.Family,e.Model,e.DataVersion,e.DataSheet});return rows;}
public static void ExportCsv(CatalogData data,string file){var rows=ExportRows(data);string full=Path.GetFullPath(file);Directory.CreateDirectory(Path.GetDirectoryName(full));string temp=full+"."+Guid.NewGuid().ToString("N")+".tmp";try{using(var w=new StreamWriter(temp,false,new UTF8Encoding(true))){foreach(var row in rows){string[] r=(string[])row.Clone();for(int i=0;i<r.Length;i++)r[i]=Quote(r[i]);w.WriteLine(String.Join(";",r));}}if(File.Exists(full))File.Replace(temp,full,null);else File.Move(temp,full);}finally{if(File.Exists(temp))File.Delete(temp);}}
public static void ExportXlsx(CatalogData data,string file){XlsxWorkbook.Write(file,ExportRows(data));}
}
public sealed partial class CatalogForm:Form {
CatalogData data; string file; DataGridView grid; TextBox search; bool dirty;
public Equipment SelectedEquipment {get;private set;}
public CatalogForm(string file):this(file,null,null){}
public CatalogForm(string file,Project project,Circuit circuit){this.file=file;string source=file;if(!File.Exists(source)&&Path.GetFileName(file)=="catalogo-v2.json"){string legacy=Path.Combine(Path.GetDirectoryName(file),"catalogo.json");if(File.Exists(legacy))source=legacy;}data=File.Exists(source)?CatalogData.Load(source):new CatalogData();InitializeBrowser(project,circuit);}
void Add(FlowLayoutPanel p,string label,Action action){var b=new Button{Text=label,AutoSize=true,BackColor=Color.FromArgb(22,134,139),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};b.Click+=delegate{try{action();}catch(Exception e){MessageBox.Show(this,e.Message,"Biblioteca",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};p.Controls.Add(b);}
Equipment Current(){return grid.CurrentRow==null?null:grid.CurrentRow.DataBoundItem as Equipment;}
void RefreshRows(){if(grid==null||search==null)return;var list=CatalogSearch.Filter(data,Query);grid.DataSource=new BindingList<Equipment>(list);RefreshDetails();}
void Edit(Equipment old){var item=old==null?new Equipment():old.Copy();using(var f=new Form{Text=old==null?"Novo equipamento":"Editar equipamento",Size=new Size(540,540),StartPosition=FormStartPosition.CenterParent}){var pg=new PropertyGrid{Dock=DockStyle.Fill,SelectedObject=item,HelpVisible=true};var b=new Button{Text="Confirmar",Dock=DockStyle.Bottom,Height=42};b.Click+=delegate{try{item.Validate();var candidate=new CatalogData();candidate.Items.AddRange(data.Items);if(old!=null)candidate.Items.Remove(old);candidate.Items.Add(item);candidate.Validate();data=candidate;dirty=true;RefreshRows();f.Close();}catch(Exception ex){MessageBox.Show(f,ex.Message);}};f.Controls.Add(pg);f.Controls.Add(b);f.ShowDialog(this);}}
void Delete(){var e=Current();if(e!=null&&MessageBox.Show(this,"Excluir "+e.Reference+"?","Biblioteca",MessageBoxButtons.YesNo)==DialogResult.Yes){data.Items.Remove(e);dirty=true;RefreshRows();}}
void Import(){using(var d=new OpenFileDialog{Filter="Planilhas (*.xls;*.xlsx;*.csv)|*.xls;*.xlsx;*.csv|Excel legado (*.xls)|*.xls|Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv"})if(d.ShowDialog(this)==DialogResult.OK){var imported=CatalogData.ImportTable(d.FileName);if(data.Items.Count>0&&MessageBox.Show(this,"Substituir a lista pelos "+imported.Items.Count+" itens importados?","Importar",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;data=imported;dirty=true;RefreshRows();}}
void Export(){ExportData(data,"equipamentos.xlsx");}
void ExportTemplate(){ExportData(new CatalogData(),"modelo-equipamentos.xlsx");}
void ExportData(CatalogData exported,string name){using(var d=new SaveFileDialog{Filter="Excel (*.xlsx)|*.xlsx|CSV (*.csv)|*.csv",FileName=name})if(d.ShowDialog(this)==DialogResult.OK){if(String.Equals(Path.GetExtension(d.FileName),".csv",StringComparison.OrdinalIgnoreCase))CatalogData.ExportCsv(exported,d.FileName);else CatalogData.ExportXlsx(exported,d.FileName);}}
void Save(){data.Save(file);dirty=false;MessageBox.Show(this,"Biblioteca salva.");}
void Use(){if(context==null)throw new InvalidOperationException("Selecione um circuito do projeto antes de aplicar dados nominais.");var e=Current();if(e==null)return;SelectedEquipment=e.Copy();DialogResult=DialogResult.OK;Close();}
}
}
