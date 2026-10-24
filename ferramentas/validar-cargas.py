#!/usr/bin/env python3
"""Conferência independente P/Q/S/FP/I por fasores fundamentais; somente stdlib."""
import cmath,csv,json,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'))
rows=list(csv.DictReader((r/'resultados.csv').open(encoding='utf-8-sig'),delimiter=';'))
boards={b['Id']:b for b in p['Boards']};all_c=[c for b in boards.values() for c in b['Circuits']];checks=[]
def near(got,expected,label):
 if not math.isclose(float(got),expected,rel_tol=1e-10,abs_tol=1e-9):raise AssertionError((label,got,expected))
 checks.append(label)
def input_kw(c):
 m=c.get('MotorStarting')
 return m['ShaftKw']/m['Efficiency'] if m else c['PowerKw']
def power(c):
 if c['ChildBoardId']:return sum((power(child) for child in boards[c['ChildBoardId']]['Circuits']),0j)
 active=input_kw(c)*c['DemandFactor'];q=c.get('ReactiveKvarOverride')
 reactive=q*c['DemandFactor'] if q is not None else active*math.sqrt(1-c['PowerFactor']**2)/c['PowerFactor']
 return complex(active,reactive)
terminals=[c for c in all_c if not c['ChildBoardId']];total=sum((power(c) for c in terminals),0j)
measured=next(row for row in rows if row['Tipo']=='Total')
for column,value in [('P_kW',total.real),('Q_kvar',total.imag),('S_kVA',abs(total)),('FP',total.real/abs(total))]:near(measured[column],value,'total '+column)
by_id={row['Id']:row for row in rows if row['Tipo']=='Circuito'}
for c in all_c:
 s=power(c);current=abs(s.conjugate()*1000/(c['Voltage']*(math.sqrt(3) if c['Phases']==1 else 1)))
 for column,value in [('P_kW',s.real),('Q_kvar',s.imag),('I_A',current)]:near(by_id[c['Id']][column],value,c['Id']+' '+column)
phase=[0j,0j,0j]
for c in terminals:
 s=power(c)
 if c['Phases']==1:
  each=s/3;v=c['Voltage']/math.sqrt(3)
  for k in range(3):phase[k]+=each.conjugate()*1000/v*cmath.exp(-2j*math.pi*k/3)
 else:
  k={'L1':0,'L2':1,'L3':2}[c['PhaseLabel']];phase[k]+=s.conjugate()*1000/c['Voltage']*cmath.exp(-2j*math.pi*k/3)
for k,current in enumerate(phase):near(next(row for row in rows if row['Tipo']=='Fase' and row['Id']=='L'+str(k+1))['I_A'],abs(current),'fase L'+str(k+1)+' A')
near(by_id['DRIVE']['P_kW'],3,'potência mecânica de saída do drive não adicionada à entrada')
near(by_id['MOTOR']['P_kW'],5,'rendimento e demanda aplicados uma vez no motor')
near(by_id['CAP']['Q_kvar'],-3,'capacitor preserva sinal e unidade kvar')
result={'status':'PASS','engine':'Python complex / soma de potências e fasores, sem chamar o C#','verificacoes':len(checks),'total_kW':total.real,'total_kvar':total.imag,'total_kVA':abs(total),'FP':total.real/abs(total),'correntes_fase_A':[abs(i) for i in phase],'checks':checks,'scope':'Modelo senoidal fundamental, tensões nominais; sem harmônicas ou cálculo de desequilíbrio de tensão.'}
(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps({k:v for k,v in result.items() if k!='checks'},ensure_ascii=False,indent=2))
