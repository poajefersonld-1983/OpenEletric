using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;
using System.Windows.Forms;

namespace CircuitoStudio {
 [DataContract] public sealed class DriveMotorData {
  [DataMember,Category("Inversor"),DisplayName("Fabricante / referência")] public string DriveReference {get;set;}
  [DataMember,Category("Inversor"),DisplayName("Corrente nominal de entrada (A)"),Description("Dado RMS de placa do inversor. É distinto da corrente nominal de saída e da corrente estimada Ib do circuito.")] public double InputRatedA {get;set;}
  [DataMember,Category("Inversor"),DisplayName("Corrente nominal de saída (A)")] public double OutputRatedA {get;set;}
  [DataMember,Category("Inversor"),DisplayName("Tensão nominal de saída (V)")] public double OutputVoltageV {get;set;}
  [DataMember,Category("Motor associado"),DisplayName("Fabricante / referência do motor")] public string MotorReference {get;set;}
  [DataMember,Category("Motor associado"),DisplayName("Corrente nominal do motor (A)")] public double MotorRatedA {get;set;}
  [DataMember,Category("Motor associado"),DisplayName("Tensão nominal do motor (V)")] public double MotorVoltageV {get;set;}
  [DataMember,Category("Motor associado"),DisplayName("Potência nominal no eixo (kW)")] public double MotorShaftKw {get;set;}
  [DataMember,Category("Rastreabilidade"),DisplayName("Fonte / revisão dos dados")] public string Source {get;set;}
  public DriveMotorData(){DriveReference=MotorReference=Source="";}
  public DriveMotorData Copy(){return (DriveMotorData)MemberwiseClone();}
  public string Describe(Circuit circuit){return String.Format(CultureInfo.GetCultureInfo("pt-BR"),"\r\nCONJUNTO INVERSOR/MOTOR — {0}\r\nInversor: {1}; entrada de placa {2:N3} A; saída de placa {3:N3} A / {4:N3} V.\r\nMotor associado: {5}; placa {6:N3} A / {7:N3} V; potência mecânica no eixo {8:N3} kW.\r\nCarga na rede: P elétrica de entrada do circuito {9:N3} kW. A potência mecânica do motor não é adicionada à demanda. Ib é uma estimativa senoidal e não usa a corrente de saída do motor.\r\nFonte dos dados: {10}\r\nO cabo do circuito pertence à entrada. Cabo PWM do motor, harmônicas, sobrecarga e seleção completa do conjunto permanecem pendentes.",circuit.Id,DriveReference,InputRatedA,OutputRatedA,OutputVoltageV,MotorReference,MotorRatedA,MotorVoltageV,MotorShaftKw,circuit.PowerKw,Source);}
  public void Validate(){foreach(string s in new[]{DriveReference,MotorReference,Source})if(String.IsNullOrWhiteSpace(s)||s.Length>2000||s.IndexOf('\0')>=0)throw new InvalidDataException("Informe referências e fonte dos dados (máximo 2000 caracteres).");foreach(double n in new[]{InputRatedA,OutputRatedA,MotorRatedA})Project.Range(n,.000001,1e9,"corrente nominal do conjunto inversor/motor");foreach(double n in new[]{OutputVoltageV,MotorVoltageV})Project.Range(n,1,1e6,"tensão do conjunto inversor/motor");Project.Range(MotorShaftKw,.000001,1e9,"potência nominal no eixo");}
 }
 public sealed partial class Circuit {
  [DataMember(Order=83),Browsable(false)] public DriveMotorData DriveMotor {get;set;}
 }
 public sealed class DriveMotorForm:Form {
  public bool Changed {get;private set;}
  public DriveMotorForm(Circuit circuit){
   if(circuit==null||circuit.ChildBoardId!="")throw new InvalidOperationException("Selecione um circuito terminal para associar o inversor e o motor.");
   Text="Inversor e motor associado";Size=new Size(720,690);MinimumSize=new Size(620,560);StartPosition=FormStartPosition.CenterParent;Font=new Font("Segoe UI",10);
   var data=circuit.DriveMotor==null?new DriveMotorData():circuit.DriveMotor.Copy();var pg=new PropertyGrid{Dock=DockStyle.Fill,SelectedObject=data,ToolbarVisible=false,HelpVisible=true};
   var note=new Label{Dock=DockStyle.Top,Height=200,Padding=new Padding(12),Text=circuit.Id+" · "+circuit.Name+"\r\nEntrada do inversor: "+circuit.Voltage+" V / "+circuit.Phases+"; P elétrica de entrada "+circuit.PowerKw+" kW.\r\nEdite tensão, potência de entrada e FP na categoria Carga do circuito. A potência do eixo do motor é um dado separado. Os cabos deste circuito estão a montante do inversor.\r\nAs correntes de placa são limites nominais; Ib continua sendo a estimativa senoidal pela carga. Dados de harmônicas, sobrecarga e cabo entre inversor/motor exigem estudo próprio."};
   var buttons=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(8),FlowDirection=FlowDirection.RightToLeft};var apply=new Button{Text="Aplicar conjunto",AutoSize=true};var cancel=new Button{Text="Cancelar",AutoSize=true,DialogResult=DialogResult.Cancel};var remove=new Button{Text="Remover associação",AutoSize=true,Enabled=circuit.DriveMotor!=null};
   apply.Click+=delegate{try{data.Validate();circuit.MotorStarting=null;circuit.DriveMotor=data;circuit.Kind="Inversor + motor";Changed=true;DialogResult=DialogResult.OK;}catch(Exception e){MessageBox.Show(this,e.Message,"Confira os dados",MessageBoxButtons.OK,MessageBoxIcon.Warning);}};
   remove.Click+=delegate{circuit.DriveMotor=null;if(circuit.Kind=="Inversor + motor")circuit.Kind="Carga";Changed=true;DialogResult=DialogResult.OK;};buttons.Controls.Add(apply);buttons.Controls.Add(cancel);buttons.Controls.Add(remove);CancelButton=cancel;Controls.Add(pg);Controls.Add(note);Controls.Add(buttons);
  }
 }
 partial class Studio {
  void ConfigureDriveMotor(){using(var f=new DriveMotorForm(circuit)){if(f.ShowDialog(this)==DialogResult.OK&&f.Changed){Change();tabs.SelectedTab=studyPage;}}}
 }
}
