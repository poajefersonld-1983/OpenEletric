"""Independent direct three-phase MNA; no sequence Thevenin/fault equations used."""
from pathlib import Path
import sys,json,csv,cmath,math
qa=Path(__file__).resolve().parent;sys.path.insert(0,str(qa/'py-libs'))
import numpy as np
import scipy
from scipy.linalg import solve
r=Path(sys.argv[1]);count=0

def check(ok,label):
 global count
 assert ok,label;count+=1

def near(a,b,label):check(abs(a-b)<=2e-7*max(1,abs(b)),label)

def case(name):
 p=json.loads((r/(name+'.circuito')).read_text(encoding='utf-8-sig'));profile=p['FaultExtremes'];rows=list(csv.DictReader((r/(name+'.csv')).open(),delimiter=';'));circuits={c['Id']:c for b in p['Boards'] for c in b['Circuits'] if c['Phases']==1};owner={c['Id']:b['Id'] for b in p['Boards'] for c in b['Circuits']};nodes={b['Id']:i for i,b in enumerate(p['Boards'])};ends={}
 for cid,c in circuits.items():
  if c['ChildBoardId']:ends[cid]=nodes[c['ChildBoardId']]
  else:ends[cid]=len(nodes);nodes['end:'+cid]=len(nodes)
 n=len(nodes)*3; a=cmath.exp(2j*math.pi/3);transform=np.array([[1,1,1],[1,a*a,a],[1,a,a*a]],complex);inv=np.linalg.inv(transform);maxres=0
 def yabc(item):
  z1=complex(item['R1Ohm'],item['X1Ohm']);z2=complex(item['R2Ohm'],item['X2Ohm']);z0=z1 # arbitrary finite zero-sequence, irrelevant for 3F/LL with symmetric EMFs.
  return transform@np.diag([1/z0,1/z1,1/z2])@inv
 def indices(node):return np.arange(node*3,node*3+3)
 for scenario,cfg in [('Mínimo declarado',profile['Minimum']),('Máximo declarado',profile['Maximum'])]:
  Y=np.zeros((n,n),complex);rhs=np.zeros(n,complex);E=profile['LineVoltageV']/math.sqrt(3)*cfg['VoltageFactor']*np.array([1,a*a,a]);sourceitems=[]
  for branch in cfg['Branches']:
   cid=branch['CircuitId'];i=indices(nodes[owner[cid]]);j=indices(ends[cid]);y=yabc(branch)
   Y[np.ix_(i,i)]+=y;Y[np.ix_(j,j)]+=y;Y[np.ix_(i,j)]-=y;Y[np.ix_(j,i)]-=y
  for source in cfg['Sources']:
   if not source['Active']:continue
   i=indices(nodes[source['BoardId']]);y=yabc(source);Y[np.ix_(i,i)]+=y;rhs[i]+=y@E;sourceitems.append((source,i,y))
  for cid,c in circuits.items():
   for location,node in [('Origem',nodes[owner[cid]]),('Fim',ends[cid])]:
    k=indices(node)
    for fault in ['3F','LL L2-L3']:
     # Add ideal short constraints to MNA. 3F ties all three conductors to a floating fault star.
     # LL uses a single b-c ideal short current unknown; no ground/N connection.
     if fault=='3F':
      B=np.zeros((n,2),complex);B[k[0],0]=1;B[k[1],0]=-1;B[k[1],1]=1;B[k[2],1]=-1
     else:B=np.zeros((n,1),complex);B[k[1],0]=1;B[k[2],0]=-1
     m=np.block([[Y,B],[B.T,np.zeros((B.shape[1],B.shape[1]),complex)]]);right=np.r_[rhs,np.zeros(B.shape[1])];solution=solve(m,right);V=solution[:n];If=B[k]@solution[n:];res=float(np.max(np.abs(m@solution-right)));maxres=max(maxres,res);check(res<1e-7,name+' MNA residual')
     def compare(sourceid,expected):
      row=next(row for row in rows if row['Scenario']==scenario and row['Circuit']==cid and row['Location']==location and row['Fault']==fault and row['Source']==sourceid)
      for phase in range(3):near(complex(float(row[f'I{phase+1}Re']),float(row[f'I{phase+1}Im'])),expected[phase],name+str((scenario,cid,location,fault,sourceid,phase)))
      near(float(row['MagnitudeA']),max(abs(x) for x in If),name+' magnitude');check(float(row['ResidualA'])<1e-6,name+' C# residual')
     compare('TOTAL',If);total=np.zeros(3,complex)
     for source,i,y in sourceitems:
      current=y@(E-V[i]);compare(source['Id'],current);total+=current
     check(np.max(np.abs(total-If))<1e-7,name+' KCL sum sources')
 return {'case':name,'nodes_3phase':len(nodes),'max_MNA_residual_A':maxres}
results=[case(name) for name in ['single-unequal-sequences','parallel-source-outage','remote-source-three-level']];report={'status':'PASS','comparisons':count,'cases':results,'method':'direct physical three-phase MNA with ideal floating-star/LL fault constraints; scipy.linalg.solve','scipy':scipy.__version__,'numpy':np.__version__};(r/'verificacao-independente.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(report)
