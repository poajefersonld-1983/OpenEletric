using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;

namespace CircuitoStudio {
 public static class EmbeddedComponents {
  static readonly object gate=new object();static bool registered;
  static readonly Dictionary<string,Assembly> loaded=new Dictionary<string,Assembly>(StringComparer.OrdinalIgnoreCase);
  public static void Ensure(){lock(gate){if(registered)return;AppDomain.CurrentDomain.AssemblyResolve+=Resolve;registered=true;}}
  static Assembly Resolve(object sender,ResolveEventArgs args){string name=new AssemblyName(args.Name).Name;lock(gate){Assembly assembly;if(loaded.TryGetValue(name,out assembly))return assembly;using(var source=Assembly.GetExecutingAssembly().GetManifestResourceStream("CircuitoStudio."+name+".dll")){if(source==null)return null;using(var buffer=new MemoryStream()){source.CopyTo(buffer);assembly=Assembly.Load(buffer.ToArray());loaded.Add(name,assembly);return assembly;}}}}
  public static string LicenseText(){var result=new StringBuilder();var resources=Assembly.GetExecutingAssembly().GetManifestResourceNames();Array.Sort(resources,StringComparer.Ordinal);foreach(string resource in resources){if(!resource.StartsWith("Legal.",StringComparison.Ordinal))continue;using(var source=Assembly.GetExecutingAssembly().GetManifestResourceStream(resource)){if(source==null)throw new InvalidDataException("Licença incorporada ausente: "+resource);using(var reader=new StreamReader(source)){result.AppendLine(resource);result.AppendLine(reader.ReadToEnd());result.AppendLine();}}}return result.ToString();}
  public static void ShowLicenses(IWin32Window owner){using(var form=new Form{Text="Licenças dos componentes",Width=800,Height=650,StartPosition=FormStartPosition.CenterParent}){form.Controls.Add(new TextBox{Dock=DockStyle.Fill,ReadOnly=true,Multiline=true,ScrollBars=ScrollBars.Both,WordWrap=true,Text=LicenseText()});form.ShowDialog(owner);}}
 }
 public static class LegacyXls {
  public static List<string[]> ReadRows(string path){var info=new FileInfo(path);if(!info.Exists||info.Length>32*1024*1024)throw new InvalidDataException("Arquivo XLS inexistente ou maior que 32 MiB.");EmbeddedComponents.Ensure();try{return ReadCore(path);}catch(InvalidDataException){throw;}catch(Exception ex){if(ex is OutOfMemoryException)throw;throw new InvalidDataException("Não foi possível importar XLS. Verifique o formato, a integridade e a ausência de senha. "+ex.Message,ex);}}
  [MethodImpl(MethodImplOptions.NoInlining)] static List<string[]> ReadCore(string path){
   using(var stream=File.OpenRead(path))using(var reader=ExcelDataReader.ExcelReaderFactory.CreateBinaryReader(stream,new ExcelDataReader.ExcelReaderConfiguration{SinglePassMode=true,FallbackEncoding=Encoding.GetEncoding(1252)})){
    var rows=new List<string[]>();int headerWidth=0;while(reader.Read()){if(rows.Count>=100000)throw new InvalidDataException("A planilha excede 100000 linhas.");if(reader.FieldCount>64)throw new InvalidDataException("A planilha excede 64 colunas.");int width=Math.Max(headerWidth,reader.FieldCount);var row=new string[width];for(int i=0;i<width;i++){object value=i<reader.FieldCount?reader.GetValue(i):null;if(i<reader.FieldCount&&reader.GetCellError(i).HasValue)throw new InvalidDataException("A planilha contém erro na linha "+(rows.Count+1)+", coluna "+(i+1)+".");string text=value==null||value==DBNull.Value?"":Convert.ToString(value,CultureInfo.InvariantCulture);if(text.Length>20000)throw new InvalidDataException("Célula XLS excessivamente longa.");row[i]=text;}
     int used=row.Length;while(used>0&&row[used-1]=="")used--;if(rows.Count==0){headerWidth=used;Array.Resize(ref row,used);}else{if(used>headerWidth)throw new InvalidDataException("Há dados além do cabeçalho na linha "+(rows.Count+1)+".");Array.Resize(ref row,headerWidth);}rows.Add(row);
    }return rows;
   }
  }
 }
}
