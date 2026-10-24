using System;
using System.Linq;
using System.IO;
namespace CircuitoStudio {
 public class PhaseBalanceResult {public double[] Before,After;public int Assigned,Locked;public double BeforePercent,AfterPercent;}
 public static class PhaseBalancing {
  static double Imbalance(double[] value){double average=value.Average();return average>0?100*(value.Max()-average)/average:0;}
  public static PhaseBalanceResult Run(Project project){
   project.Validate();double u0=project.Source.LineVoltage/Math.Sqrt(3);var list=project.Boards.SelectMany(b=>b.Circuits).Where(c=>c.ChildBoardId==""&&c.Phases==PhaseMode.Monofasico).ToList();
   if(project.Boards.Any(b=>Math.Abs(b.Voltage-project.Source.LineVoltage)>.001)||list.Any(c=>Math.Abs(c.Voltage-u0)/u0>.05))throw new InvalidDataException("Balanceamento limitado a cargas fase-neutro em rede de mesma tensão, com V próximo de Ulinha/√3.");
   if(list.Any(c=>c.PhaseLabel!="L1"&&c.PhaseLabel!="L2"&&c.PhaseLabel!="L3"))throw new InvalidDataException("Atribua L1, L2 ou L3 aos circuitos monofásicos antes do balanceamento.");
   var study=Engineering.Analyze(project);var result=new PhaseBalanceResult{Before=new double[3],After=new double[3]};
   foreach(var c in list){int i=c.PhaseLabel[1]-'1';result.Before[i]+=study.For(c).CurrentA;if(c.PhaseLocked){result.After[i]+=study.For(c).CurrentA;result.Locked++;}}
   foreach(var c in list.Where(c=>!c.PhaseLocked).OrderByDescending(c=>study.For(c).CurrentA)){int i=Array.IndexOf(result.After,result.After.Min());c.PhaseLabel="L"+(i+1);result.After[i]+=study.For(c).CurrentA;result.Assigned++;}
   result.BeforePercent=Imbalance(result.Before);result.AfterPercent=Imbalance(result.After);return result;
  }
 }
}
