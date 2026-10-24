#!/usr/bin/env python3
import csv,json,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=p['Boards'][0]['Circuits'][0];pr=c['Protection'];binding=c['ResidualSelection'];rating=binding['Rating'];rows=list(csv.DictReader((r/'tabela.csv').open(encoding='utf-8-sig'),delimiter=';'));table=next(x for x in rows if x['Reference']==rating['Reference']);n=0
def check(ok):
 global n
 assert ok
 n+=1
check(p['FileVersion']==4 and 'residual-assemblies-v1' in p['RequiredCapabilities']);check(binding['Engine']=='cs-diferenciais-conjuntos-v1');check(rating['Kind']==1 and table['Kind']=='ResidualRelay');check(rating['Waveform']==binding['RequiredWaveform']==2);check(rating['Poles']==binding['RequiredPoles']==3)
for key in ['Manufacturer','Reference','RequiredPrimaryConfiguration','SensorReference','ActuatorReference','AccessoryReference','AssemblyDocument','Source','DataVersion','Standard','Conditions']:check(rating[key]==table[key])
for key in ['VoltageV','FrequencyHz','RatedA','DeltaMilliA','ConditionalKA','ControlVoltageV']:check(float(table[key])==rating[key])
ib=c['PowerKw']*c['DemandFactor']*1000/(math.sqrt(3)*c['Voltage']*c['PowerFactor']);touch=pr['EarthResistanceOhm']*rating['DeltaMilliA']/1000
check(ib<=rating['RatedA']);check(c['BreakerA']<=rating['RatedA']);check(rating['DeltaMilliA']<=binding['MaximumDeltaMilliA']);check(bool(binding['RequirementSource']));check(rating['ConditionalKA']>=p['Source']['ShortCircuitKA']);check(pr['BreakingCapacityKA']>=p['Source']['ShortCircuitKA']);check(rating['ControlSupply']==binding['ControlSupply']==2);check(rating['ControlVoltageV']==binding['ControlVoltageV']==24);check(p['Earthing']=='TT' and touch<=pr['ContactLimitV']);check(rating['VoltageV']==c['Voltage'] and rating['FrequencyHz']==p['Frequency']);check(pr['RcdMilliA']==0 and c['BreakerA']==25);check(bool(rating['SensorReference']) and bool(rating['ActuatorReference']) and bool(rating['AssemblyDocument']))
materials=list(csv.DictReader((r/'materiais.csv').open(encoding='utf-8-sig'),delimiter=';'));selected=[x for x in materials if x.get('Categoria')=='Conjunto diferencial selecionado'];check(len(selected)==1 and selected[0]['Quantidade']=='1' and all(ref in selected[0]['Referência'] for ref in ['CT-QA','TRIP24-QA','AUX-QA']))
result={'status':'PASS','comparacoes':n,'Ib_A':ib,'I_delta_mA':rating['DeltaMilliA'],'Ra_ohm':pr['EarthResistanceOhm'],'Ra_Idelta_V':touch,'control_V':rating['ControlVoltageV'],'accessory':rating['AccessoryReference'],'scope':'Conjunto sintético: nominais, contato TT e rastreabilidade conferidos, sem seletividade/tempo/certificação presumidos.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False,indent=2))
