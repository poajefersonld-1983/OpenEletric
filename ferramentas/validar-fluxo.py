from pathlib import Path
import sys,json,csv,math,cmath
qa=Path(__file__).resolve().parent;sys.path.insert(0,str(qa/'py-libs'))
import scipy
import numpy as np
from scipy.optimize import root
r=Path(sys.argv[1]);n=0

def check(ok,label=''):
 global n
 assert ok,label;n+=1

def near(a,b,label):check(abs(complex(a)-complex(b))<=2e-5*max(1,abs(b)),label)
def solve_case(name):
 p=json.loads((r/(name+'.circuito')).read_text(encoding='utf-8-sig'));cfg=p['FlowSettings'];circuits={c['Id']:c for b in p['Boards'] for c in b['Circuits']};owner={c['Id']:b['Id'] for b in p['Boards'] for c in b['Circuits']};incoming={c['ChildBoardId']:c['Id'] for c in circuits.values() if c['ChildBoardId']};nominal=cfg['NominalLineVoltageV'];source=np.array([nominal/math.sqrt(3)*cfg['SourceVoltageFactor']*cmath.exp(-2j*math.pi*i/3) for i in range(3)]+[0j]);rows={row['Circuit']:row for row in csv.DictReader((r/(name+'.csv')).open(),delimiter=';')}
 # This benchmark solves nodal KCL with MINPACK, independent of backward/forward sweeps.
 # Use nonzero impedance cases; ideal analytic examples are validated by closed forms in C#.
 nodes={('root',i):i for i in range(4)};wire={};load_edges={}
 def first(c):return int(c['PhaseLabel'][1])-1
 def isneutral(c):
  if c['ChildBoardId']:return any(isneutral(x) for x in circuits.values() if owner[x['Id']]==c['ChildBoardId'])
  return c['FlowLoad']['Connection'] in (1,3)
 def parent(c,k):return (incoming[owner[c['Id']]],k) if owner[c['Id']] in incoming else ('root',k)
 for c in circuits.values():
  if c['ChildBoardId']:active=[0,1,2]+([3] if isneutral(c) else [])
  elif c['FlowLoad']['Connection']==1:active=[0,1,2,3]
  elif c['FlowLoad']['Connection']==2:active=[0,1,2]
  elif c['FlowLoad']['Connection']==3:active=[first(c),3]
  else:active=[first(c),c['FlowLoad']['SecondPhase']]
  for k in active:nodes[(c['Id'],k)]=len(nodes)
  if not c['ChildBoardId']:
   kind=c['FlowLoad']['Connection'];a=first(c) if kind in (3,4) else 0
   pairs=[(i,3) for i in range(3)] if kind==1 else [(0,1),(1,2),(2,0)] if kind==2 else [(a,3 if kind==3 else c['FlowLoad']['SecondPhase'])];load_edges[c['Id']]=pairs
 dim=len(nodes);Y=np.zeros((dim,dim),dtype=complex);inj=np.zeros(dim,dtype=complex)
 def stamp(a,b,z):
  check(abs(z)>0,'nodal benchmark needs finite nonzero branch Z');y=1/z;Y[a,a]+=y;Y[b,b]+=y;Y[a,b]-=y;Y[b,a]-=y
 zs=complex(cfg['SourcePhaseROhm'],cfg['SourcePhaseXOhm']);zn=complex(cfg['SourceNeutralROhm'],cfg['SourceNeutralXOhm']);check(cfg['NeutralModel']==1)
 for k in range(4):
  z=zs if k<3 else zn;check(abs(z)>0);Y[k,k]+=1/z;inj[k]+=source[k]/z
 for c in circuits.values():
  for key,index in list(nodes.items()):
   if key[0]!=c['Id']:continue
   k=key[1]
   if k<3:z=complex(c['Cable']['ResistanceOhmKm'],c['Cable']['ReactanceOhmKm'])*c['LengthM']/1000
   else:
    rating=c['ReturnSizing']['Neutral'] if c['Conductors']['HasNeutral'] else c['ReturnSizing']['Protective'];z=complex(rating['ResistanceOhmKm'],rating['ReactanceOhmKm'])*c['LengthM']/1000
   wire[key]=z;stamp(index,nodes[parent(c,k)],z)
 def load_current(v):
  load=np.zeros(dim,dtype=complex)
  for cid,pairs in load_edges.items():
   c=circuits[cid];kw=c['PowerKw']*c['DemandFactor'];q=(c['ReactiveKvarOverride'] if c['ReactiveKvarOverride'] is not None else c['PowerKw']*math.tan(math.acos(c['PowerFactor'])))*c['DemandFactor'];s=complex(kw,q)*1000/len(pairs);vn=c['Voltage']/math.sqrt(3) if c['FlowLoad']['Connection']==1 else c['Voltage']
   for a,b in pairs:
    ia,ib=nodes[(cid,a)],nodes[(cid,b)];voltage=v[ia]-v[ib];i=np.conj(s/voltage) if c['FlowLoad']['Model']==0 else np.conj(s)/(vn*vn)*voltage;load[ia]+=i;load[ib]-=i
  return load
 def unpack(x):return x[:dim]+1j*x[dim:]
 def residual(x):
  z=Y@unpack(x)-inj+load_current(unpack(x));return np.r_[z.real,z.imag]
 v0=np.array([source[k] for cid,k in nodes]);solution=root(residual,np.r_[v0.real,v0.imag],method='hybr',options={'xtol':1e-11});check(solution.success,name+' Newton convergence: '+solution.message);check(np.max(np.abs(residual(solution.x)))<1e-7,name+' KCL residual');v=unpack(solution.x);loss=0j;load=0j
 total=np.array([(source[k]-v[k])/(zs if k<3 else zn) for k in range(4)]);supply=sum(source[k]*np.conj(total[k]) for k in range(4));loss+=sum((zs if k<3 else zn)*abs(total[k])**2 for k in range(4))
 for cid,c in circuits.items():
  row=rows[cid];current=np.zeros(4,dtype=complex);end=np.array([v[nodes[(cid,k)]] if (cid,k) in nodes else v[nodes[parent(c,k)]] if parent(c,k) in nodes else 0j for k in range(4)]);branch_loss=0j
  for k in range(4):
   if (cid,k) in nodes:
    current[k]=(v[nodes[parent(c,k)]]-v[nodes[(cid,k)]])/wire[(cid,k)];branch_loss+=wire[(cid,k)]*abs(current[k])**2;near(complex(float(row[('V1','V2','V3','VN')[k]+'Re']),float(row[('V1','V2','V3','VN')[k]+'Im'])),end[k],name+cid+' V'+str(k))
   near(complex(float(row[('I1','I2','I3','IN')[k]+'Re']),float(row[('I1','I2','I3','IN')[k]+'Im'])),current[k],name+cid+' I'+str(k))
  loss+=branch_loss;near(row['LossW'],branch_loss.real,name+cid+' lossW');near(row['LossVar'],branch_loss.imag,name+cid+' lossVar')
  if c['ChildBoardId']:pairs=[(i,3) for i in range(3)] if isneutral(c) else [(0,1),(1,2),(2,0)]
  else:pairs=load_edges[cid];load+=sum(end[k]*np.conj(current[k]) for k in range(4))
  nominal_leg=c['Voltage']/math.sqrt(3) if (c['ChildBoardId'] and isneutral(c)) or (not c['ChildBoardId'] and c['FlowLoad']['Connection']==1) else c['Voltage'];operating=[abs(end[a]-end[b]) for a,b in pairs]
  for i,value in enumerate(operating):near(row['Operating'+str(i+1)+'V'],value,name+cid+' operating')
  near(row['NominalLegV'],nominal_leg,name+cid+' nominal');near(row['DropPercent'],max(100*abs(nominal_leg*cfg['SourceVoltageFactor']-x)/nominal_leg for x in operating),name+cid+' drop');near(row['MinimumPu'],min(operating)/nominal_leg,name+cid+' minPU');near(row['MaximumPu'],max(operating)/nominal_leg,name+cid+' maxPU')
 totals=list(csv.DictReader((r/(name+'-totals.csv')).open(),delimiter=';'))[0]
 for prefix,value in [('Source',supply),('Load',load),('Loss',loss)]:near(totals[prefix+'W'],value.real,name+prefix+' W');near(totals[prefix+'Var'],value.imag,name+prefix+' Var')
 check(abs(supply-load-loss)<1e-6,name+' independent power conservation')
 return {'case':name,'nodes':dim,'newton_evaluations':solution.nfev,'kcl_residual_A':float(np.max(np.abs(residual(solution.x))))}
names=sys.argv[2:] or ['single-LN','single-impedance','shared-source-two-roots','mixed-three-level','mixed-wye-neutral'];results=[solve_case(name) for name in names];report={'status':'PASS','comparacoes':n,'cases':results,'solver':'SciPy optimize.root hybr / nodal current injection; independent of C#backward-forward sweeps','numpy':np.__version__,'scipy':scipy.__version__};(r/'verificacao-independente.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(report)
