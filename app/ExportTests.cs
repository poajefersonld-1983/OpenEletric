using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using CircuitoStudio;

internal static class ExportTests
{
    static int checks;
    static void Check(bool pass,string message) { checks++; if(!pass) throw new Exception(message); }
    public static void Main()
    {
        string output=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"verificacao"); Directory.CreateDirectory(output);
        Project sample=Project.CreateSample(); sample.Name="Edifício Horizonte"; sample.Client="Cliente demonstração"; sample.Site="São Paulo"; sample.Designer="Equipe de projeto"; sample.Earthing="TN-S";
        Exports.Html(sample,Path.Combine(output,"relatorio-exemplo.html")); Exports.Csv(sample,Path.Combine(output,"dados-exemplo.csv")); Exports.Svg(sample,sample.Boards[0],Path.Combine(output,"unifilar-exemplo.svg"));
        Project p=new Project(); p.Name="<script>alert(1)</script> & teste"; p.Client="=HYPERLINK(\"https://example.invalid\")"; p.Notes="A;B \"aspas\"\nsegunda linha";
        for(int bi=0;bi<3;bi++) { Board b=new Board {Id="Q"+bi,Name="Quadro "+bi}; for(int ci=0;ci<13;ci++)b.Circuits.Add(new Circuit{Id="Q"+bi+"C"+ci,Name="Carga "+ci,Kind="<img src=x onerror=alert(1)>",Notes="@SUM(1;2)",PowerKw=2.5,PowerFactor=0.8,DemandFactor=0.7}); p.Boards.Add(b); }
        p.Validate(); string htmlPath=Path.Combine(output,"escape.html"),csvPath=Path.Combine(output,"escape.csv"),svgPath=Path.Combine(output,"multiplas-linhas.svg");
        Exports.Html(p,htmlPath);Exports.Csv(p,csvPath);Exports.Svg(p,p.Boards[0],svgPath);
        string html=File.ReadAllText(htmlPath),csv=File.ReadAllText(csvPath);
        Check(!html.Contains("<script>")&&!html.Contains("<img src="),"HTML não escapou markup");Check(html.Contains("&lt;script&gt;")&&html.Contains("&lt;img"),"Conteúdo escapado ausente");
        foreach(Board b in p.Boards)foreach(Circuit c in b.Circuits)Check(html.Contains(c.Id+" · "),"Circuito ausente no HTML: "+c.Id);
        Check(csv.Contains("\"'=HYPERLINK("),"Fórmula no cliente não neutralizada");Check(csv.Contains("\"'@SUM(1;2)\""),"Fórmula de circuito não neutralizada");Check(csv.Contains("\"A;B \"\"aspas\"\"\nsegunda linha\""),"Aspas, delimitador ou quebra de linha incorretos");
        byte[] bytes=File.ReadAllBytes(csvPath);Check(bytes[0]==239&&bytes[1]==187&&bytes[2]==191,"CSV sem BOM UTF8");Check(csv.Contains("\"2,5\""),"CSV não usa decimal pt-BR");
        XDocument xml=XDocument.Load(svgPath);XNamespace ns="http://www.w3.org/2000/svg";Check(xml.Root.Name==ns+"svg","Raiz SVG inválida");Check(xml.Descendants(ns+"circle").Count(x=>(string)x.Attribute("r")=="15")==13,"Contagem de cargas SVG errada");
        string[] texts=xml.Descendants(ns+"text").Select(x=>x.Value).ToArray();foreach(Circuit c in p.Boards[0].Circuits)Check(texts.Any(t=>t.StartsWith(c.Id+" · ")),"Circuito ausente no SVG: "+c.Id);
        Check(texts.Count(t=>t.StartsWith("Barramento · "))==4,"Treze circuitos precisam de quatro trechos");Check(!xml.Descendants(ns+"script").Any(),"Markup ativo no SVG");
        Check(xml.Descendants(ns+"text").Where(x=>x.Value.Contains("Q0C12 · ")).All(x=>Int32.Parse(x.Attribute("y").Value)>800),"Última linha SVG na posição errada");
        Project empty=new Project();Board emptyBoard=new Board();empty.Boards.Add(emptyBoard);Exports.Svg(empty,emptyBoard,Path.Combine(output,"vazio.svg"));XDocument.Load(Path.Combine(output,"vazio.svg"));Check(true,"Quadro vazio");
        Project radial=new Project();Board root=new Board{Id="ROOT",Name="Raiz",Voltage=380};Board child=new Board{Id="CHILD",Name="Filho",Voltage=380};radial.Boards.Add(root);radial.Boards.Add(child);
        Circuit feeder=new Circuit{Id="FEED",Name="Alimentador",ChildBoardId="CHILD",Phases=PhaseMode.Trifasico,Voltage=380,PowerKw=99};root.Circuits.Add(feeder);
        child.Circuits.Add(new Circuit{Id="LOAD",Name="Carga",PowerKw=7,PowerFactor=1,Voltage=380,Phases=PhaseMode.Trifasico});
        Circuit cap=new Circuit{Id="CAP",Name="Capacitor",PowerKw=0,ReactiveKvarOverride=-10,PowerFactor=1,Voltage=380,Phases=PhaseMode.Trifasico};child.Circuits.Add(cap);
        string rh=Path.Combine(output,"radial.html"),rc=Path.Combine(output,"radial.csv"),rs=Path.Combine(output,"capacitor.svg");Exports.Html(radial,rh);Exports.Csv(radial,rc);Exports.Svg(radial,child,rs);
        string radialHtml=File.ReadAllText(rh),radialCsv=File.ReadAllText(rc);XDocument capSvg=XDocument.Load(rs);
        Check(radialCsv.Contains("\"Demanda total (kW)\";\"7\""),"Total radial duplicou demanda de alimentador");
        Check(radialHtml.Contains("7 kW de demanda"),"Quadro raiz não agrega demanda do filho");
        Check(radialCsv.Contains("\"Reativo (kvar)\"")&&radialCsv.Contains("\"Quadro alimentado ID\""),"Colunas de reativo/destino ausentes");
        Check(radialCsv.Contains("\"15,19\""),"Corrente de capacitor no CSV deveria ser 15,19 A");
        Check(radialHtml.Contains("15,19")&&radialHtml.Contains("-10"),"HTML perdeu corrente ou sinal capacitivo");
        Check(capSvg.Descendants(ns+"text").Any(x=>x.Value.Contains("15,19 A")),"SVG perdeu corrente capacitiva");
        Check(capSvg.Descendants(ns+"text").Any(x=>x.Value.Contains("-10 kvar")),"SVG perdeu sinal reativo");
        Check(!radialHtml.Contains("sem verificação de capacidade"),"Prosa obsoleta contradiz estudos");
        Console.WriteLine("PASS: "+checks+" verificações. Artefatos: "+output);
    }
}
