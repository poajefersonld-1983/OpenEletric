#!/usr/bin/env python3
import csv,json,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=p['Boards'][0]['Circuits'][0];a=c['Cable'];pr=c['Protection'];checks=0
candidates=list(csv.DictReader((r/'cabos.csv').open(encoding='utf-8-sig'),delimiter=';'));measured={float(x['Section']):x for x in csv.DictReader((r/'avaliacoes.csv').open(),delimiter=';')}
def check(ok):
 global checks
 assert ok
 checks+=1
ib=1000*c['PowerKw']*c['DemandFactor']/(math.sqrt(3)*c['Voltage']*c['PowerFactor']);passing=[]
for row in candidates:
 s=float(row['SectionMm2']);iz=float(row['IzA'])*a['TemperatureFactor']*a['GroupingFactor']*a['Parallel'];drop=100*math.sqrt(3)*ib*c['LengthM']/1000/a['Parallel']*float(row['ResistanceOhmKm'])*c['PowerFactor']/c['Voltage'];record=measured[s]
 for actual,expected in [(float(record['Ib']),ib),(float(record['Iz']),iz),(float(record['Drop']),drop)]:check(math.isclose(actual,expected,rel_tol=1e-12))
 ok=ib<=c['BreakerA']<=iz and pr['ConventionalTripA']<=1.45*iz and pr['LetThroughA2s']<=(115*s)**2 and drop<=(c.get('VoltageDropLimitPercent') or p['Source']['MaxDropPercent'])
 check((record['Status']=='Atende verificações locais')==ok)
 if ok:passing.append(s)
selected=[s for s,row in measured.items() if row['Recommended']=='True'];check(selected==[min(passing)]);check(selected==[10.0])
result={'status':'PASS','comparacoes':checks,'Ib_A':ib,'min_section_mm2':min(passing),'scope':'Tabela e fatores sintéticos; referência de cálculo independente para as verificações locais.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False,indent=2))
