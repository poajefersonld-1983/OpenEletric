"""Independent nominal industrial checks from project inputs; no calls to C#.
The separate validar-fluxo.py solves nodal KCL with SciPy for permanent grid cases.
"""
from pathlib import Path
import json,math,cmath,sys,hashlib
r=Path(sys.argv[1]);results=json.loads((r/'resultados.json').read_text());checks=0;errors=[];summary=[]
def check(ok,label):
 global checks
 checks+=1
 if not ok:raise AssertionError(label)
def near(a,b,label):
 check(a is not None and math.isclose(float(a),float(b),rel_tol=1e-9,abs_tol=1e-8),label)
def source(s):
 v=s['LineVoltage'];model=s['Model']
 if model==3 and s['UPSMode']==0:return None
 if model==1:return complex(s['GeneratorROhm'],s['GeneratorXdPercent']/100*v*v/(s['GeneratorRatedKva']*1000))
 if model==2:return complex(s['DefinedPhaseROhm'],s['DefinedPhaseXOhm'])
 uv=s['UpstreamVoltageV'] if s['CurrentBasis']==1 else v
 z=s['VoltageFactor']*uv/(3**.5*s['ShortCircuitKA']*1000)*(v/uv)**2
 out=cmath.rect(z,math.atan2(1,s['RXRatio']))
 if s['TransformerKva']>0:
  apparent=s['TransformerKva']*1000;zt=s['TransformerUkPercent']/100*v*v/apparent;rt=s['TransformerCopperLossKw']*1000*v*v/apparent**2;out+=complex(rt,math.sqrt(zt*zt-rt*rt))
 return out
for row in results:
 name=row['Name'];p=json.loads((r/(name+'.circuito')).read_text(encoding='utf-8-sig'));s=p['Source'];boards={b['Id']:b for b in p['Boards']};circuits={c['Id']:c for b in boards.values() for c in b['Circuits']};incoming={c['ChildBoardId']:c for c in circuits.values() if c['ChildBoardId']};owner={c['Id']:b['Id'] for b in boards.values() for c in b['Circuits']};observed={c['Id']:c for c in row['Circuits']};nominal=s['LineVoltage'];base=source(s)
 def load(c):
  if c['ChildBoardId']:return sum((load(x) for x in boards[c['ChildBoardId']]['Circuits']),0j)
  active=c['PowerKw'];motor=c['MotorStarting']
  if motor is not None:
   active=motor['ShaftKw']/motor['Efficiency'];near(c['PowerKw'],active,name+c['Id']+' shaft efficiency')
  reactive=c['ReactiveKvarOverride'] if c['ReactiveKvarOverride'] is not None else active*math.sqrt(1/c['PowerFactor']**2-1)
  return complex(active,reactive)*c['DemandFactor']
 terminal=[c for c in circuits.values() if not c['ChildBoardId']];total=sum((load(c) for c in terminal),0j);apparent=abs(total);near(row['Kw'],total.real,name+' total P');near(row['Kvar'],total.imag,name+' total Q');near(row['Kva'],apparent,name+' total S');near(row['PF'],total.real/apparent if apparent else 1,name+' PF');near(row['CompensationKvar'],max(0,total.imag-total.real*math.sqrt(1/s['TargetPowerFactor']**2-1)),name+' compensation')
 if base is not None:near(row['SourceR'],base.real,name+' source R');near(row['SourceX'],base.imag,name+' source X')
 else:check(row['SourceR'] is None and row['SourceX'] is None,name+' UPS limit not converted into impedance')
 phases=[0j,0j,0j];phase_known=True
 for c in terminal:
  power=load(c)*1000
  if c['Phases']==1:
   for k in range(3):phases[k]+=(power/3/(nominal/3**.5)).conjugate()
  else:
   if abs(c['Voltage']-nominal/3**.5)>max(1,nominal/3**.5*.01):phase_known=False
   phases[int(c['PhaseLabel'][1])-1]+=(power/c['Voltage']).conjugate()
 check(row['PhaseKnown']==phase_known,name+' phase applicability')
 if phase_known:
  for k in range(3):near(row['PhaseA'][k],abs(phases[k]),name+' phase nominal current'+str(k))
 drops={};zends={}
 def visit(bid,parent_drop,upstream):
  for c in boards[bid]['Circuits']:
   actual=observed[c['Id']];pq=load(c);k=3**.5 if c['Phases']==1 else 1;current=abs(pq)*1000/(c['Voltage']*k);a=c['Cable'];wire=complex(a['ResistanceOhmKm'],a['ReactanceOhmKm'])*c['LengthM']/1000/a['Parallel'];drop=(3**.5 if c['Phases']==1 else 2)*1000*(wire.real*pq.real+wire.imag*pq.imag)/(k*c['Voltage']);percent=100*drop/c['Voltage'];acc=parent_drop+percent;iz=a['AmpacityA']*a['TemperatureFactor']*a['GroupingFactor']*a['Parallel']
   for key,expected in [('P',pq.real),('Q',pq.imag),('I',current),('DropV',drop),('DropPercent',percent),('AccumulatedDropPercent',acc),('Iz',iz)]:near(actual[key],expected,name+c['Id']+' '+key)
   check(actual['Overload']==('OK' if current<=c['BreakerA']<=iz else 'Falha'),name+c['Id']+' overload boundary')
   if upstream is not None:
    near(actual['OriginKA'],s['VoltageFactor']*nominal/(3**.5*abs(upstream)*1000),name+c['Id']+' origin 3F fault')
    if c['Phases']==1:near(actual['EndKA'],s['VoltageFactor']*nominal/(3**.5*abs(upstream+wire)*1000),name+c['Id']+' terminal 3F fault')
    else:check(actual['EndKA'] is None,name+c['Id']+' mono no inferred phase-earth fault')
   else:check(actual['OriginKA'] is None and actual['EndKA'] is None,name+c['Id']+' UPS prospective fault pending')
   if c['ChildBoardId']:visit(c['ChildBoardId'],acc,None if upstream is None else upstream+wire)
 for b in boards:
  if b not in incoming:visit(b,0,base)
 summary.append(dict(case=name,kw=total.real,kvar=total.imag,kva=apparent,pf=total.real/apparent if apparent else 1,flow=row['FlowConverged'],checks_failed=row['ChecksFailed'],checks_pending=row['ChecksPending'],sha256=hashlib.sha256((r/(name+'.circuito')).read_bytes()).hexdigest()))
check(len(results)==10,'ten scenarios');before=next(x for x in results if x['Name'].startswith('05'));after=next(x for x in results if x['Name'].startswith('06'));near(after['PF'],.95,'capacitor target reached');near(after['Kw'],before['Kw'],'compensation preserves active power');check(after['Kva']<before['Kva'],'compensation reduces nominal apparent power');check(any(c['Overload']=='Falha' for x in results for c in x['Circuits']),'intentional failed overload case');check(any(c['Overload']=='OK' for x in results for c in x['Circuits']),'approved overload comparison case')
report=dict(status='PASS',checks=checks,cases=summary,scope='Synthetic industrial nominal fundamental cases; no standard or OEM certification. Separate nodal validation covers grid steady-state flows, not generator AVR or UPS control.')
(r/'verificacao-nominal-independente.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2))
