using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace CircuitoStudio {
 public static class ProjectEditing {
  static void RequireBoard(Project p,Board b){if(b==null||!p.Boards.Contains(b))throw new InvalidOperationException("O quadro não pertence ao projeto.");}
  static void RequireCircuit(Project p,Board b,Circuit c){RequireBoard(p,b);if(c==null||!b.Circuits.Contains(c))throw new InvalidOperationException("O circuito não pertence ao quadro selecionado.");}
  static string NewId(Project p,string prefix){var ids=new HashSet<string>(p.Boards.Select(b=>b.Id).Concat(p.Boards.SelectMany(b=>b.Circuits).Select(c=>c.Id)),StringComparer.Ordinal);int index=1;while(ids.Contains(prefix+index))index++;return prefix+index;}
  public static Board AddBoard(Project p){p.Validate();if(p.Boards.Count>=10000)throw new InvalidOperationException("O projeto atingiu o limite de quadros.");string id=NewId(p,"Q");var b=new Board{Id=id,Name="Quadro "+id.Substring(1),Voltage=p.Source.LineVoltage};p.Boards.Add(b);return b;}
  public static Circuit AddCircuit(Project p,Board b){p.Validate();RequireBoard(p,b);if(b.Circuits.Count>=10000)throw new InvalidOperationException("O quadro atingiu o limite de circuitos.");var c=new Circuit{Id=NewId(p,"C"),Name="Novo circuito",Kind="Carga",Phases=PhaseMode.Monofasico,Voltage=Math.Round(b.Voltage/Math.Sqrt(3),1),PowerKw=1,PowerFactor=1,DemandFactor=1,LengthM=10};if(c.Voltage<1)c.Voltage=b.Voltage;b.Circuits.Add(c);return c;}
  public static void RenameBoard(Project p,Board b,string name){p.Validate();RequireBoard(p,b);Project.Text(name,"nome do quadro",true);b.Name=name;}
  public static void RenameCircuit(Project p,Board b,Circuit c,string name){p.Validate();RequireCircuit(p,b,c);Project.Text(name,"nome do circuito",true);c.Name=name;}
  public static int RemoveBoard(Project p,Board b){p.Validate();RequireBoard(p,b);int incoming=0;foreach(var other in p.Boards)if(other!=b)incoming+=other.Circuits.RemoveAll(c=>c.ChildBoardId==b.Id);p.Boards.Remove(b);return incoming;}
  public static void RemoveCircuit(Project p,Board b,Circuit c){p.Validate();RequireCircuit(p,b,c);b.Circuits.Remove(c);}
  public static Circuit DuplicateCircuit(Project p,Board b,Circuit source){p.Validate();RequireCircuit(p,b,source);if(source.ChildBoardId!="")throw new InvalidOperationException("O circuito alimenta outro quadro. Crie um novo alimentador para evitar duplicar a conexão.");if(b.Circuits.Count>=10000)throw new InvalidOperationException("O quadro atingiu o limite de circuitos.");Circuit copy;using(var stream=new MemoryStream()){var serializer=new DataContractJsonSerializer(typeof(Circuit));serializer.WriteObject(stream,source);stream.Position=0;copy=(Circuit)serializer.ReadObject(stream);}copy.Id=NewId(p,"C");if(copy.LoopFault!=null&&copy.LoopFault.SecondCircuitId==source.Id)copy.LoopFault.SecondCircuitId=copy.Id;copy.Name=source.Name+" (cópia)";if(copy.Name.Length>100000)throw new InvalidOperationException("Nome do circuito excede o limite após duplicação.");copy.ChildBoardId="";copy.DiagramX=copy.DiagramY=0;copy.DinRow=copy.DinColumn=0;b.Circuits.Add(copy);return copy;}
 }
}
