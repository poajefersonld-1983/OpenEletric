#!/usr/bin/env python3
import csv,json,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=p['Boards'][0]['Circuits'][0];s=c['ProtectionResponses'];queries=list(csv.DictReader((r/'consulta.csv').open(),delimiter=';'));n=0
def check(ok):
 global n
 assert ok
 n+=1
check(p['FileVersion']==4 and 'protection-responses-v1' in p['RequiredCapabilities']);check(s['Engine']=='cs-curvas-resposta-v1')
for field,kind,kind_id,unit in [('Time','TimeSeconds',0,'s'),('Energy','EnergyA2s',1,'A²s'),('Peak','PeakKA',2,'kA pico')]:
 curve=s[field]['Curve'];table=list(csv.DictReader((r/(kind+'.csv')).open(encoding='utf-8-sig'),delimiter=';'));query=next(x for x in queries if x['Kind']==kind);check(curve['Kind']==kind_id and query['Unit']==unit)
 for key in ['Manufacturer','Reference','Settings','Source','DataVersion','Conditions']:check(curve[key]==table[0][key])
 check(curve['RatedA']==c['BreakerA']==25);check(curve['MagneticPickupA']==c['Protection']['MagneticTripA']==125);check(curve['DelaySettingSeconds']==0);check(curve['VoltageV']==c['Voltage'] and curve['FrequencyHz']==p['Frequency'])
 x=float(query['CurrentA']);left,right=table;fraction=math.log(x/float(left['CurrentA']))/math.log(float(right['CurrentA'])/float(left['CurrentA']))
 for key in ['Lower','Upper']:
  expected=float(left[key])*(float(right[key])/float(left[key]))**fraction;check(math.isclose(float(query[key]),expected,rel_tol=1e-12));check(math.isclose(curve['Points'][0][key],float(left[key]),rel_tol=1e-12));check(math.isclose(curve['Points'][1][key],float(right[key]),rel_tol=1e-12))
check(c['Protection']['LetThroughA2s']==0 and c['Protection']['DisconnectSeconds']==0)
result={'status':'PASS','comparacoes':n,'consulta_A':float(queries[0]['CurrentA']),'bandas':{x['Kind']:{'lower':float(x['Lower']),'upper':float(x['Upper']),'unit':x['Unit']} for x in queries},'scope':'Curvas sintéticas: interpolação e persistência independentes, sem homologação do dispositivo.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False,indent=2))
