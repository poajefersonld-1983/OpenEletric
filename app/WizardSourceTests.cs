using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using CircuitoStudio;
internal static class WizardSourceTests {
 static int n;static void Check(bool ok,string label){n++;if(!ok)throw new Exception(label);}
 static T Field<T>(object o,string name){return (T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(o);}
 static void Set(object o,string name,object value){o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).SetValue(o,value);}
 static object Call(object o,string name){return o.GetType().GetMethod(name,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);}
 static void Capture(ProjectWizard f,string name){if(Environment.GetEnvironmentVariable("CIRCUITO_QA_VISUAL")!="1")return;f.StartPosition=FormStartPosition.Manual;f.Location=new Point(10,10);f.ShowInTaskbar=false;f.Show();Application.DoEvents();using(var b=new Bitmap(f.ClientSize.Width,f.ClientSize.Height)){using(var g=Graphics.FromImage(b))g.CopyFromScreen(f.PointToScreen(Point.Empty),Point.Empty,f.ClientSize);string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"referencias-assistente");Directory.CreateDirectory(dir);b.Save(Path.Combine(dir,name+".png"));}f.Hide();}
 [STAThread] public static int Main(){string directory=Path.Combine(Path.GetTempPath(),"wizard-source-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);try{
  foreach(string mode in new[]{"BT","MTBT","GeradorBT","GeradorMT","Impedancia","UPSInversor","UPSBypass"})using(var wizard=new ProjectWizard()){
   wizard.CreateControl();Field<TextBox>(wizard,"name").Text="Projeto "+mode+" ç Ω";Check((bool)Call(wizard,"CommitStep"),mode+" metadata committed");var draft=Field<Project>(wizard,"draft");
   if(mode=="MTBT"){draft.Source.CurrentBasis=SourceCurrentBasis.RedeMT;draft.Source.UpstreamVoltageV=20000;draft.Source.TransformerKva=630;draft.Source.TransformerUkPercent=6;draft.Source.TransformerCopperLossKw=6.3;draft.Source.ShortCircuitKA=12.5;draft.Source.LineVoltage=400;}
   else if(mode.StartsWith("Gerador")){draft.Source.Model=SourceModel.GeradorSincrono;draft.Source.GeneratorRatedKva=1000;draft.Source.GeneratorXdPercent=20;draft.Source.LineVoltage=mode=="GeradorMT"?11000:400;draft.Source.NetworkReference="Dados de ensaio do assistente";}
   else if(mode=="Impedancia"){draft.Source.Model=SourceModel.ImpedanciaDefinida;draft.Source.LineVoltage=400;draft.Source.DefinedPhaseROhm=.1;draft.Source.DefinedPhaseXOhm=.2;draft.Source.DefinedNeutralROhm=.05;draft.Source.DefinedPEROhm=.03;}
   else if(mode.StartsWith("UPS")){draft.Source.Model=SourceModel.UPS;draft.Source.LineVoltage=400;draft.Source.UPSRatedKva=40;draft.Source.UPSCurrentLimitKA=.18;draft.Source.UPSCurrentLimitSeconds=.2;draft.Source.UPSMode=mode=="UPSBypass"?UPSOperatingMode.Bypass:UPSOperatingMode.Inversor;if(mode=="UPSBypass")draft.Source.ShortCircuitKA=10;}
   else draft.Source.ShortCircuitKA=10;
   Field<WizardSourceEditor>(wizard,"sourceEditor").Reload();Set(wizard,"step",1);Check((bool)Call(wizard,"CommitStep"),mode+" source validated");Call(wizard,"ShowStep");if(mode=="GeradorMT")Capture(wizard,"gerador-alimentacao");if(mode=="UPSInversor")Capture(wizard,"ups-inversor-alimentacao");if(mode=="Impedancia")Capture(wizard,"impedancia-alimentacao");
   Field<NumericUpDown>(wizard,"circuits").Value=3;Field<ComboBox>(wizard,"phases").SelectedIndex=1;Set(wizard,"step",2);Call(wizard,"ShowStep");string summary=Field<TextBox>(wizard,"summary").Text;
   Check(summary.Contains(draft.Source.Model.ToString()),mode+" summary identifies source model");if(mode.StartsWith("Gerador"))Check(summary.Contains("xd''")&&!summary.Contains("Icc a montante"),"generator review does not show utility current");
   if(mode=="GeradorMT")Capture(wizard,"gerador-revisao");Call(wizard,"BuildResult");wizard.Result.Validate();var result=wizard.Result;
   Check(result.Boards.Count==1&&result.Boards[0].Circuits.Count==3,mode+" valid initial topology");Check(result.Boards[0].Voltage==draft.Source.LineVoltage&&result.Boards[0].Circuits[0].Voltage==draft.Source.LineVoltage,mode+" circuit board source voltage consistent");
   var before=Engineering.Analyze(result);Check(before.For(result.Boards[0].Circuits[0]).FaultOriginKA.HasValue==(mode!="UPSInversor"),mode+" source integrated without inventing inverter fault current");
   string path=Path.Combine(directory,mode+".circuito");result.Save(path);var restored=Project.Load(path);Check(restored.Name==result.Name&&restored.Source.Model==result.Source.Model&&restored.Boards[0].Circuits.Count==3,mode+" assistant result persistent");
  }
  using(var f=new ProjectWizard()){var p=Field<Project>(f,"draft");p.Source.Model=SourceModel.GeradorSincrono;p.Source.LineVoltage=400;p.Source.GeneratorRatedKva=500;p.Source.GeneratorXdPercent=30;Field<NumericUpDown>(f,"circuits").Value=3;Set(f,"step",2);Call(f,"BuildResult");f.Result.Validate();Check(Math.Abs(f.Result.Boards[0].Circuits[0].Voltage-230.9)<.01,"single phase generator circuits use phase-neutral voltage");Check(f.Result.Boards[0].Circuits[2].PhaseLabel=="L3","single phase initial phase distribution");}
  using(var f=new ProjectWizard()){
   var editor=Field<WizardSourceEditor>(f,"sourceEditor");var values=Field<System.Collections.Generic.Dictionary<string,TextBox>>(editor,"numbers");var source=Field<Project>(f,"draft").Source;
   Check(values["PF"].Text==.95.ToString("R",System.Globalization.CultureInfo.CurrentCulture),"editor uses readable roundtrip number text");
   values["Voltage"].Text="400";values["Factor"].Text="1,05";values["PF"].Text="0,9";values["Drop"].Text="3,5";editor.Commit();Check(source.LineVoltage==400&&source.VoltageFactor==1.05&&source.TargetPowerFactor==.9&&source.MaxDropPercent==3.5,"editor commits units and decimal comma without losing limits");
   values["Voltage"].Text="NaN";bool rejected=false;try{editor.Commit();}catch(InvalidDataException){rejected=true;}Check(rejected&&f.Result==null,"invalid source text rejected before producing project");values["Voltage"].Text="400";
   source.ShortCircuitKA=10;source.TransformerKva=630;editor.Reload();Field<ComboBox>(editor,"model").SelectedItem=SourceModel.GeradorSincrono;values["KvaG"].Text="500";values["Xd"].Text="30";editor.Commit();Check(source.Model==SourceModel.GeradorSincrono&&source.ShortCircuitKA==0&&source.TransformerKva==0,"choosing generator replaces utility source without accidental parallel connection");
   Check(!Field<System.Collections.Generic.Dictionary<string,Control[]>>(editor,"rows")["Basis"][1].Visible,"utility-only fields hidden in generator mode");
  }
  Console.WriteLine("PASS: "+n+" source wizard assertions including utility, generator, defined impedance and UPS scenarios");return 0;
 }catch(Exception e){Console.Error.WriteLine(e);return 1;}finally{Directory.Delete(directory,true);}}
}
