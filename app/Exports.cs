using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace CircuitoStudio
{
    public static class Exports
    {
        private static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-BR");
        private static string N(double value) { return value.ToString("0.##", Pt); }
        private static string E(string value) { return (value ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;").Replace("'", "&#39;"); }
        private static string Q(string value)
        {
            value = value ?? "";
            string trimmed = value.TrimStart();
            if (trimmed.Length > 0 && ("=+-@".IndexOf(trimmed[0]) >= 0 || value[0] == '\t' || value[0] == '\r' || value[0] == '\n')) value = "'" + value;
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        }
        private static void Row(StringBuilder s, params string[] cells)
        {
            for (int i = 0; i < cells.Length; i++) { if (i > 0) s.Append(';'); s.Append(Q(cells[i])); } s.AppendLine();
        }
        private static string Phase(Circuit c) { return c.Phases == PhaseMode.Trifasico ? "Trifásico" : "Monofásico"; }

        public static void Csv(Project p, string path)
        {
            Study study = Engineering.Analyze(p);
            StringBuilder s = new StringBuilder();
            Row(s, "OpenEletric — exportação de dados");
            Row(s, "Projeto", p.Name, "Cliente", p.Client, "Local", p.Site, "Responsável", p.Designer);
            Row(s, "Frequência (Hz)", N(p.Frequency), "Aterramento", p.Earthing, "Demanda total (kW)", N(study.TotalKw));
            Row(s, "Observações do projeto", p.Notes);
            Row(s, "Descrição — linha 1", p.DescriptionLine1);
            Row(s, "Descrição — linha 2", p.DescriptionLine2);
            Row(s, "Descrição — linha 3", p.DescriptionLine3);
            Row(s, "Quadro ID", "Quadro", "Tensão do quadro (V)", "Circuito ID", "Circuito", "Tipo", "Fases", "Tensão (V)", "Potência instalada (kW)", "Fator de potência", "Fator de demanda", "Demanda (kW)", "Reativo (kvar)", "Quadro alimentado ID", "Corrente de projeto (A)", "Comprimento (m)", "Seção informada (mm²)", "Disjuntor informado (A)", "Observações");
            foreach (Board b in p.Boards) foreach (Circuit c in b.Circuits)
                Row(s, b.Id, b.Name, N(b.Voltage), c.Id, c.Name, c.Kind, Phase(c), N(c.Voltage), N(c.PowerKw), N(c.PowerFactor), N(c.DemandFactor), N(study.For(c).DemandKw), N(study.For(c).ReactiveKvar), c.ChildBoardId, N(study.For(c).CurrentA), N(c.LengthM), N(c.CableMm2), N(c.BreakerA), c.Notes);
            Row(s, "Hipóteses", "Demanda e correntes conforme estudo do modelo radial: alimentadores agregam cargas a jusante; o total considera apenas raízes, sem dupla contagem. Reativo negativo indica carga capacitiva. I = |P+jQ| × 1000 / V (monofásico) ou / (√3 × V) (trifásico). Seções e proteções são dados de referência; consulte os estudos e suas pendências.");
            File.WriteAllText(path, s.ToString(), new UTF8Encoding(true));
        }
        public static void Html(Project p, string path)
        {
            Study study = Engineering.Analyze(p);
            StringBuilder s = new StringBuilder();
            s.Append("<!doctype html><html lang=\"pt-BR\"><head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"><title>").Append(E(p.Name)).Append(" — OpenEletric</title><style>body{font:14px Segoe UI,Arial,sans-serif;color:#173047;background:#edf2f7;margin:0}main{max-width:1250px;margin:32px auto;background:white;padding:36px;border-radius:12px}h1{font-size:30px;margin:8px 0}h2{font-size:21px;border-bottom:2px solid #16a398;padding-bottom:12px;margin-top:34px}.brand{color:#09867d;letter-spacing:2px;font-size:12px;font-weight:bold}.meta{display:grid;grid-template-columns:repeat(2,1fr);gap:12px;margin:24px 0}.meta div{padding:12px;background:#f3f7fa;border-radius:6px}small{color:#526777}table{border-collapse:collapse;width:100%;font-size:11px}th{background:#173047;color:white;text-align:left}td,th{padding:9px 6px;border-bottom:1px solid #dce5ed;vertical-align:top}tr:nth-child(even){background:#f6f9fb}.note{white-space:pre-wrap;overflow-wrap:anywhere}.assumptions{padding:18px;background:#fff8e8;border-left:4px solid #cf9c31;line-height:1.6;margin-top:28px}@media print{body{background:white}main{margin:0;padding:0;border-radius:0;max-width:none}thead{display:table-header-group}tr{break-inside:avoid}.meta div{background:white;border:1px solid #ddd}@page{size:A4 landscape;margin:12mm}}</style></head><body><main><div class=\"brand\">OPENELETRIC · RELATÓRIO DE PROJETO</div><h1>").Append(E(p.Name)).Append("</h1><div class=\"meta\">");
            Meta(s,"Cliente",p.Client); Meta(s,"Local",p.Site); Meta(s,"Responsável",p.Designer); Meta(s,"Alimentação",N(p.Frequency)+" Hz · "+p.Earthing); Meta(s,"Demanda total",N(study.TotalKw)+" kW"); Meta(s,"Observações",p.Notes);Meta(s,"Descrição (linhas 1–3)",String.Join("\n",p.DescriptionRows)); s.Append("</div>");
            foreach (Board b in p.Boards)
            {
                double total=0; foreach(Circuit c in b.Circuits) total+=study.For(c).DemandKw;
                s.Append("<h2>").Append(E(b.Name)).Append(" <small>· ").Append(E(b.Id)).Append(" · ").Append(N(b.Voltage)).Append(" V · ").Append(N(total)).Append(" kW de demanda</small></h2><table><thead><tr>");
                string[] heads={"ID / circuito","Tipo / fases","Tensão (V)","Potência (kW)","FP / FD","Demanda (kW)","Reativo (kvar)","Quadro destino","Corrente (A)","Comp. (m)","Seção (mm²)","Disj. (A)","Observações"}; foreach(string h in heads)s.Append("<th>").Append(E(h)).Append("</th>"); s.Append("</tr></thead><tbody>");
                foreach(Circuit c in b.Circuits){s.Append("<tr>"); string[] cells={c.Id+" · "+c.Name,c.Kind+" · "+Phase(c),N(c.Voltage),N(c.PowerKw),N(c.PowerFactor)+" / "+N(c.DemandFactor),N(study.For(c).DemandKw),N(study.For(c).ReactiveKvar),c.ChildBoardId,N(study.For(c).CurrentA),N(c.LengthM),N(c.CableMm2),N(c.BreakerA),c.Notes}; foreach(string cell in cells)s.Append("<td class=\"note\">").Append(E(cell)).Append("</td>"); s.Append("</tr>");}
                s.Append("</tbody></table>");
            }
            s.Append("<div class=\"assumptions\"><strong>Hipóteses e limites</strong><br>FP = fator de potência; FD = fator de demanda. Alimentadores agregam a demanda e o reativo das cargas a jusante; o total considera somente raízes, sem dupla contagem. Reativo negativo indica carga capacitiva. A corrente usa a potência aparente |P+jQ| e a tensão informada, com √3 para circuitos trifásicos. Seções e disjuntores são dados de referência; consulte os estudos de capacidade, queda de tensão, curto-circuito e proteção, incluindo suas pendências e limitações. Este relatório não substitui a validação por profissional habilitado.</div></main></body></html>");
            File.WriteAllText(path,s.ToString(),new UTF8Encoding(false));
        }
        private static void Meta(StringBuilder s,string label,string value){s.Append("<div><small>").Append(E(label)).Append("</small><br><strong class=\"note\">").Append(E(value)).Append("</strong></div>");}
        private static void Text(StringBuilder s,int x,int y,string value,int size,string color)
        {s.Append("<text x=\"").Append(x).Append("\" y=\"").Append(y).Append("\" font-size=\"").Append(size).Append("\" fill=\"").Append(color).Append("\">").Append(E(value)).Append("</text>");}
        private static void Line(StringBuilder s,int x1,int y1,int x2,int y2){s.AppendFormat(CultureInfo.InvariantCulture,"<path d=\"M {0} {1} L {2} {3}\" fill=\"none\" stroke=\"#173047\" stroke-width=\"2\"/>",x1,y1,x2,y2);}
        public static void Svg(Project p,Board b,string path){DiagramSvg.Save(p,b,path);}
        private static IEnumerable<string> Wrap(string value,int limit)
        {
            value=(value??"").Replace('\r',' ').Replace('\n',' ');
            int max=limit*2;if(value.Length>max)value=value.Substring(0,max-1)+"…";
            for(int start=0;start<value.Length;start+=limit)yield return value.Substring(start,Math.Min(limit,value.Length-start));
        }
    }
}
