#!/usr/bin/env python3
import csv,json,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=p['Boards'][0]['Circuits'][0];a=c['Cable'];pr=c['Protection'];snap=pr['BreakerSnapshot'];table=list(csv.DictReader((r/'tabela.csv').open(encoding='utf-8-sig'),delimiter=';'));n=0

def check(v):
 global n
 assert v
 n+=1
check('breaker-rating-v1' in p['RequiredCapabilities'] and p['FileVersion']==4)
row=snap['Rating'];origin=next(x for x in table if x['Reference']==row['Reference'] and float(x['VoltageV'])==row['VoltageV'])
for key in ['Manufacturer','Reference','Source','DataVersion','Standard','Sheet']:check(origin[key]==row[key])
for key in ['VoltageV','FrequencyHz','RatedA','CapacityKA','I2A']:check(float(origin[key])==row[key])
ib=c['PowerKw']*c['DemandFactor']*1000/(math.sqrt(3)*c['Voltage']*c['PowerFactor']);iz=a['AmpacityA']*a['TemperatureFactor']*a['GroupingFactor']*a['Parallel'];ik=p['Source']['ShortCircuitKA']
check(math.isclose(ib,20,rel_tol=1e-12));check(ib<=c['BreakerA']<=iz);check(pr['ConventionalTripA']<=1.45*iz);check(pr['BreakingCapacityKA']>=ik);check(row['VoltageV']==c['Voltage'] and row['FrequencyHz']==p['Frequency']);check(row['Poles']==snap['RequiredPoles']==3);check(row['Metric']==snap['RequiredMetric']==0)
for key in ['LetThroughA2s','MagneticTripA','DisconnectSeconds','RcdMilliA']:check(pr[key]==0)
check(c['CurveStudy']['Downstream'] is None)
selected=[]
for x in table:
 compatible=x['Poles']=='ThreePole' and x['Metric']=='Icu' and float(x['VoltageV'])==c['Voltage'] and float(x['FrequencyHz'])==p['Frequency']
 passes=compatible and ib<=float(x['RatedA'])<=iz and float(x['I2A'])<=1.45*iz and float(x['CapacityKA'])>=ik
 if passes:selected.append(x['Reference'])
check(selected==['QA-25'])
result={'status':'PASS','comparacoes':n,'Ib_A':ib,'Iz_A':iz,'Ik_nominal_kA':ik,'opcoes_atendidas':selected,'scope':'Referência independente com tabela sintética; sem certificação de Ik máximo ou coordenação.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False,indent=2))
