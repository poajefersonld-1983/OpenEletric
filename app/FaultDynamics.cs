using System;
namespace CircuitoStudio {
 public sealed class FaultDynamics {
  public double SymmetricKA {get;private set;}
  public double FrequencyHz {get;private set;}
  public double PowerFactor {get;private set;}
  public double Kappa {get;private set;}
  public double PeakKA {get;private set;}
  public double? TimeConstantSeconds {get;private set;}
  public bool Undamped {get;private set;}
  double phi;bool purelyResistive;
  private FaultDynamics(){}
  public static FaultDynamics Calculate(double resistanceOhm,double reactanceOhm,double frequencyHz,double symmetricKA){
   Project.Range(resistanceOhm,0,2e15,"R equivalente do curto");Project.Range(reactanceOhm,0,2e15,"X equivalente do curto");Project.Range(frequencyHz,1,1000,"frequência do curto");Project.Range(symmetricKA,0,1e15,"corrente simétrica de curto");
   if(resistanceOhm==0&&reactanceOhm==0)throw new System.IO.InvalidDataException("Impedância equivalente nula.");
   double scale=Math.Max(resistanceOhm,reactanceOhm),z=scale*Math.Sqrt(Math.Pow(resistanceOhm/scale,2)+Math.Pow(reactanceOhm/scale,2));
   var result=new FaultDynamics{SymmetricKA=symmetricKA,FrequencyHz=frequencyHz,PowerFactor=resistanceOhm/z,phi=Math.Atan2(reactanceOhm,resistanceOhm),purelyResistive=reactanceOhm==0,Undamped=resistanceOhm==0};
   result.Kappa=reactanceOhm==0?1.02:1.02+.98*Math.Exp(-3*resistanceOhm/reactanceOhm);
   result.PeakKA=Math.Sqrt(2)*result.Kappa*symmetricKA;
   if(resistanceOhm>0){double tau=(reactanceOhm/resistanceOhm)/(2*Math.PI*frequencyHz);if(Double.IsInfinity(tau))throw new System.IO.InvalidDataException("R/X extremo: constante de tempo fora da faixa numérica.");result.TimeConstantSeconds=tau;}
   return result;
  }
  double Decay(double seconds){if(purelyResistive)return 0;if(seconds==0)return 1;return Undamped?1:Math.Exp(-seconds/TimeConstantSeconds.Value);}
  public double DcEnvelopeKA(double seconds){Project.Range(seconds,0,1e9,"tempo após a falta");return Math.Sqrt(2)*SymmetricKA*Decay(seconds);}
  public double InstantaneousKA(double seconds,double closingVoltageAngleDegrees){
   Project.Range(seconds,0,1e9,"tempo após a falta");Project.Range(closingVoltageAngleDegrees,-1e15,1e15,"ângulo de fechamento");
   double angle=(closingVoltageAngleDegrees%360)*Math.PI/180;
   // Constant sinusoidal voltage and a series R-L equivalent, with zero pre-fault current.
   return Math.Sqrt(2)*SymmetricKA*(Math.Sin(2*Math.PI*FrequencyHz*seconds+angle-phi)-Math.Sin(angle-phi)*Decay(seconds));
  }
 }
}
