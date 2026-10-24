using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace CircuitoStudio {
 public sealed partial class Project {
  string description1,description2,description3;
  [DataMember(Order=33),Category("Projeto"),DisplayName("Descrição — linha 1")] public string DescriptionLine1 {get{return description1??"";}set{description1=value;}}
  [DataMember(Order=34),Category("Projeto"),DisplayName("Descrição — linha 2")] public string DescriptionLine2 {get{return description2??"";}set{description2=value;}}
  [DataMember(Order=35),Category("Projeto"),DisplayName("Descrição — linha 3")] public string DescriptionLine3 {get{return description3??"";}set{description3=value;}}
  [Browsable(false)] public string[] DescriptionRows {get{return new[]{DescriptionLine1,DescriptionLine2,DescriptionLine3};}}
  internal void ValidateMetadata(){foreach(string line in DescriptionRows)if(line.Length>500||line.IndexOfAny(new[]{'\r','\n','\0'})>=0)throw new InvalidDataException("Cada linha de descrição aceita até 500 caracteres, sem quebra de linha. Use os três campos separados.");}
 }
}
