using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
namespace CircuitoStudio {
 public sealed partial class Equipment {
  string family,model,dataVersion,dataSheet;
  [DataMember,DisplayName("Família")] public string Family {get{return family??"";}set{family=value;}}
  [DataMember,DisplayName("Modelo")] public string Model {get{return model??"";}set{model=value;}}
  [DataMember,DisplayName("Versão dos dados")] public string DataVersion {get{return dataVersion??"";}set{dataVersion=value;}}
  [DataMember,DisplayName("Ficha técnica / referência")] public string DataSheet {get{return dataSheet??"";}set{dataSheet=value;}}
 }
 public sealed class CatalogQuery {
  public string Text="",Manufacturer="",Family="",Model="";
 }
 public static class CatalogSearch {
  static bool Contains(string value,string search){return (value??"").IndexOf((search??"").Trim(),StringComparison.OrdinalIgnoreCase)>=0;}
  static string AllText(Equipment item){return String.Join(" ",new[]{item.Manufacturer,item.Reference,item.Description,item.Category,item.Family,item.Model,item.Source,item.DataVersion,item.DataSheet});}
  public static List<Equipment> Filter(CatalogData catalog,CatalogQuery query){if(catalog==null||query==null)throw new ArgumentNullException("catalog/query");catalog.Validate();return catalog.Items.Where(item=>Contains(item.Manufacturer,query.Manufacturer)&&Contains(item.Family,query.Family)&&Contains(item.Model,query.Model)&&(String.IsNullOrWhiteSpace(query.Text)||Contains(AllText(item),query.Text))).ToList();}
  public static string Describe(Equipment item,CatalogQuery query,CircuitResult context){
   if(item==null)return "Nenhum item selecionado. Cadastre ou importe uma biblioteca para consultar a ficha.";item.Validate();if(query==null)throw new ArgumentNullException("query");
   var text=new StringBuilder();text.AppendLine("FICHA DO ITEM — "+item.Manufacturer+" / "+item.Reference);text.AppendLine("Família: "+Known(item.Family)+" | Modelo: "+Known(item.Model));text.AppendLine("Categoria: "+Known(item.Category));text.AppendLine("Descrição: "+item.Description);text.AppendLine("Versão dos dados: "+Known(item.DataVersion));text.AppendLine("Fonte: "+Known(item.Source));text.AppendLine("Ficha técnica / referência: "+Known(item.DataSheet));text.AppendLine("Nominais: In="+N(item.RatedA)+" A; interrupção="+N(item.BreakingKA)+" kA; I2="+N(item.TripI2A)+" A; IΔn="+N(item.RcdMilliA)+" mA.");
   text.AppendLine("\r\nCRITÉRIOS DA CONSULTA");foreach(var pair in new[]{new[]{"Fabricante",query.Manufacturer,item.Manufacturer},new[]{"Família",query.Family,item.Family},new[]{"Modelo",query.Model,item.Model},new[]{"Busca geral",query.Text,AllText(item)}})if(!String.IsNullOrWhiteSpace(pair[1]))text.AppendLine(pair[0]+": "+(Contains(pair[2],pair[1])?"corresponde":"não corresponde")+" ao filtro “"+pair[1].Trim()+"”.");
   if(String.IsNullOrWhiteSpace(query.Manufacturer+query.Family+query.Model+query.Text))text.AppendLine("Nenhum filtro restringe a consulta.");
   text.AppendLine("\r\nCOMPARAÇÕES NOMINAIS COM O CIRCUITO");
   if(context==null)text.AppendLine("Sem circuito selecionado: não há comparação elétrica. A ficha permanece disponível para consulta.");
   else {text.AppendLine("Circuito "+context.Circuit.Id+" — "+context.Circuit.Name+"; valores calculados na rede completa.");Compare(text,"Ib ≤ In",item.RatedA>0?(bool?)(context.CurrentA<=item.RatedA):null,"Ib="+N(context.CurrentA)+" A; In="+N(item.RatedA)+" A");Compare(text,"In ≤ Iz",item.RatedA>0&&context.CorrectedAmpacityA.HasValue?(bool?)(item.RatedA<=context.CorrectedAmpacityA.Value):null,"Iz="+(context.CorrectedAmpacityA.HasValue?N(context.CorrectedAmpacityA.Value)+" A":"não informado"));Compare(text,"Interrupção ≥ Ik 3F aproximado na origem",item.BreakingKA>0&&context.FaultOriginKA.HasValue?(bool?)(item.BreakingKA>=context.FaultOriginKA.Value):null,"capacidade="+N(item.BreakingKA)+" kA; Ik="+(context.FaultOriginKA.HasValue?N(context.FaultOriginKA.Value)+" kA":"não disponível"));}
   if(String.IsNullOrWhiteSpace(item.Source)||String.IsNullOrWhiteSpace(item.DataVersion))text.AppendLine("Pendente: fonte e/ou versão dos dados não informadas.");
   text.AppendLine("Pré-análise de valores informados. Tensão, polos, categoria de emprego, Icu/Icn por tensão, coordenação e curto máximo normativo não são validados por esta consulta. O item não recebe aprovação global de seleção.");return text.ToString();
  }
  static void Compare(StringBuilder text,string rule,bool? pass,string detail){text.AppendLine((pass.HasValue?(pass.Value?"Atende nominalmente":"Não atende"):"Pendente")+": "+rule+" — "+detail+".");}
  static string N(double value){return value.ToString("G8",CultureInfo.GetCultureInfo("pt-BR"));}
  static string Known(string value){return String.IsNullOrWhiteSpace(value)?"não informado":value;}
 }
 public static class CatalogApplication {
  public static void Apply(Circuit circuit,Equipment item){
   if(circuit==null||circuit.Protection==null||item==null)throw new InvalidDataException("Circuito ou equipamento ausente.");item.Validate();Project.Range(item.RatedA,0,1e9,"corrente do equipamento");foreach(double value in new[]{item.BreakingKA,item.TripI2A,item.LetThroughA2s,item.MagneticA,item.RcdMilliA})Project.Range(value,0,1e15,"dados de proteção do equipamento");
   string record="Equipamento: "+item.Manufacturer+" "+item.Reference+"; família: "+item.Family+"; modelo: "+item.Model+"; versão: "+item.DataVersion+"; ficha: "+item.DataSheet+". Fonte: "+item.Source;
   string notes=String.IsNullOrEmpty(circuit.Notes)?record:circuit.Notes+"\r\n"+record;Project.Text(notes,"notas com referência do equipamento",false);
   circuit.Protection.BreakerSnapshot=null;circuit.BreakerA=item.RatedA;circuit.Protection.BreakingCapacityKA=item.BreakingKA;circuit.Protection.ConventionalTripA=item.TripI2A;circuit.Protection.LetThroughA2s=item.LetThroughA2s;circuit.Protection.MagneticTripA=item.MagneticA;circuit.Protection.RcdMilliA=item.RcdMilliA;circuit.Notes=notes;
  }
 }
}
