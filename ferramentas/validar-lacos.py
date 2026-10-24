from pathlib import Path
import json,csv,math,cmath,sys
r=Path(sys.argv[1]);n=0

def check(ok,label=''):
 global n
 assert ok,label;n+=1

def near(a,b,label):check(abs(complex(a)-complex(b))<=1e-8*max(1,abs(b)),label)
def z(src,pre):return complex(src[pre+'ROhm'],src[pre+'XOhm'])
def emf(p,live):return 0j if live==3 else p['Source']['LineVoltage']/math.sqrt(3)*cmath.exp(-2j*math.pi*live/3)
def evaluate(p,c,f):
 boards={b['Id']:b for b in p['Boards']};owner={item['Id']:b for b in boards.values() for item in b['Circuits']};incoming={item['ChildBoardId']:item for b in boards.values() for item in b['Circuits'] if item['ChildBoardId']};src=p['LoopSource'];earth=p['GroundingSource'];scheme=earth['Scheme']
 def path(item):return path(incoming[owner[item['Id']]['Id']])+[item] if owner[item['Id']]['Id'] in incoming else [item]
 def phase(item):
  if item['LengthM']==0:return 0j
  cable=item['Cable'];res=cable['ResistanceOhmKm'] or (23.7 if cable['Material']==0 else 37.6)/item['CableMm2'];return complex(res,cable['ReactanceOhmKm'])*item['LengthM']/1000
 def ret(item,neutral):
  if item['LengthM']==0:return 0j
  wiring=item['Conductors'];rating=item['ReturnSizing']['Neutral'] if neutral and wiring['HasNeutral'] else item['ReturnSizing']['Protective'];return complex(rating['ResistanceOhmKm'],rating['ReactanceOhmKm'])*item['LengthM']/1000
 def phasepath(item):return sum(map(phase,path(item)),0j)
 def retpath(item,neutral):return sum((ret(part,neutral) for part in path(item)),0j)
 def sourceret(neutral):return z(src,'PEN') if scheme in (3,4) else z(src,'Neutral' if neutral else 'PE')
 zp=z(src,'Phase');zf=complex(f['FaultROhm'],f['FaultXOhm']);star=None;metal=None;ref='Unknown';imp=None
 if f['Kind']==0:imp=zp+phasepath(c)+sourceret(True)+retpath(c,True)+zf;current=emf(p,f['Live'])/imp
 elif f['Kind']==1:
  returnz=retpath(c,False)+(earth['InstallationRaOhm'] if scheme==1 else sourceret(False));imp=zp+phasepath(c)+returnz+(z(src,'PE')+earth['SourceRbOhm'] if scheme==1 else 0)+zf;current=emf(p,f['Live'])/imp;metal=current*returnz;ref='RemoteEarth' if scheme==1 else 'SourceNeutral'
 elif f['Kind']==2:
  # Independent reduction to star-node admittances, rather than the C#5x5 MNA.
  phasez=zp+phasepath(c);pe=retpath(c,False)+earth['InstallationRaOhm'];faultz=pe+zf;y=complex(src['ITConductanceSiemensPerPhase'],2*math.pi*p['Frequency']*src['ITCapacitanceNFPerPhase']*1e-9);yg=0j if earth['NeutralConnection']==2 else 1/(complex(earth['GroundingROhm'],earth['GroundingXOhm'])+z(src,'PE')+earth['SourceRbOhm']);healthy=y/(1+phasez*y);hasn=c['Conductors']['HasNeutral'];yn=complex(src['ITNeutralConductanceSiemens'],2*math.pi*p['Frequency']*src['ITNeutralCapacitanceNF']*1e-9) if hasn else 0j;zn=sourceret(True)+retpath(c,True) if hasn and (yn!=0 or f['Live']==3) else 0j;neutral_adm=yn/(1+zn*yn)
  if f['Live']==3:
   numerator=sum((healthy*emf(p,i) for i in range(3)),0j)
   if faultz==0:
    if zn==0:star=0j;current=-numerator
    else:star=-numerator/(3*healthy+yg+1/zn);current=star/zn
   else:
    neutral_fault_adm=(yn+1/faultz)/(1+zn*(yn+1/faultz));star=-numerator/(3*healthy+yg+neutral_fault_adm);current=star/(1+zn*(yn+1/faultz))/faultz
  elif faultz==0:
   current=(3*healthy+yg+neutral_adm)*emf(p,f['Live'])/(1+(2*healthy+yg+neutral_adm)*phasez);star=phasez*current-emf(p,f['Live'])
  else:
   admittances=[(y+1/faultz)/(1+phasez*(y+1/faultz)) if i==f['Live'] else healthy for i in range(3)];star=-sum((admittances[i]*emf(p,i) for i in range(3)),0j)/(sum(admittances)+yg+neutral_adm);voltage=(star+emf(p,f['Live']))/(1+phasez*(y+1/faultz));current=voltage/faultz
  metal=current*pe;ref='RemoteEarth'
 else:
  second=next(item for b in boards.values() for item in b['Circuits'] if item['Id']==f['SecondCircuitId']);a,b=path(c),path(second);shared=0
  while shared<min(len(a),len(b)) and a[shared]['Id']==b[shared]['Id']:shared+=1
  za=sourceret(True)+retpath(c,True) if f['Live']==3 else zp+phasepath(c);zb=sourceret(True)+retpath(second,True) if f['SecondLive']==3 else zp+phasepath(second);between=sum((ret(item,False) for item in a[shared:]+b[shared:]),0j);imp=za+zb+between+zf+complex(f['SecondFaultROhm'],f['SecondFaultXOhm']);current=(emf(p,f['Live'])-emf(p,f['SecondLive']))/imp
 return current,imp,metal,ref,star
rows=list(csv.DictReader((r/'resultados.csv').open(),delimiter=';'));check(len(rows)>=12,'benchmark cases missing')
for row in rows:
 p=json.loads((r/(row['Case']+'.circuito')).read_text(encoding='utf-8-sig'));c=next(item for b in p['Boards'] for item in b['Circuits'] if item['Id']==row['Circuit']);f=c['LoopFault'];current,imp,metal,ref,star=evaluate(p,c,f);near(complex(float(row['IRealA']),float(row['IImagA'])),current,row['Case']+' current');check(row['MetalReference']==ref,row['Case']+' potential reference')
 if imp is not None:near(complex(float(row['ZRealOhm']),float(row['ZImagOhm'])),imp,row['Case']+' impedance')
 else:check(row['ZRealOhm']=='' and row['ZImagOhm']=='',row['Case']+' no invented series Z')
 if metal is not None:near(float(row['MetalV']),abs(metal),row['Case']+' metal voltage')
 else:check(row['MetalV']=='',row['Case']+' no invented metal voltage')
 if star is not None:near(complex(float(row['StarRealV']),float(row['StarImagV'])),star,row['Case']+' star displacement')
 check('fault-loops-v1' in p['RequiredCapabilities']);check(p['LoopSource']['Engine']=='cs-lacos-desacoplados-v1')
result={'status':'PASS','comparacoes':n,'cases':len(rows),'method':'Python complex loop sums, star-node admittance reduction independent of 5x5/6x6 MNA, shared-PE path removal','scope':'Synthetic nominal equivalents, TT published rounded example; no certification or protection time.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(result)
