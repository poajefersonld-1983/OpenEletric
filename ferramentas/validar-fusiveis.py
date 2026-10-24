#!/usr/bin/env python3
import json,csv,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=p['Boards'][0]['Circuits'][0];a=c['Cable'];pr=c['Protection'];binding=c['SwitchgearSelection'];rating=binding['Rating'];table=list(csv.DictReader((r/'tabela.csv').open(encoding='utf-8-sig'),delimiter=';'));n=0
def check(ok):
 global n
 assert ok
 n+=1
check(p['FileVersion']==4 and 'switchgear-selection-v1' in p['RequiredCapabilities']);check(binding['Engine']=='cs-fusiveis-seccionadores-v1');check(rating['Family']==binding['RequiredFamily']==8 and rating['Poles']==binding['RequiredPoles']==3);check(rating['VoltageV']==c['Voltage'] and rating['FrequencyHz']==p['Frequency'])
origin=next(x for x in table if x['Reference']==rating['Reference'])
for key in ['Manufacturer','Reference','Source','DataVersion','Standard','Sheet']:check(origin[key]==rating[key])
for key in ['VoltageV','FrequencyHz','RatedA','BreakingKA','I2A','IcwKA','IcwSeconds','IcmPeakKA']:check(float(origin[key])==rating[key])
ib=c['PowerKw']*c['DemandFactor']*1000/(math.sqrt(3)*c['Voltage']*c['PowerFactor']);ik=p['Source']['ShortCircuitKA'];ratio=p['Source']['RXRatio'];peak=math.sqrt(2)*(1.02+.98*math.exp(-3*ratio))*ik
check(ib<=rating['RatedA']);check(rating['BreakingKA']==rating['I2A']==0);check(ik<=rating['IcwKA']);check(0<pr['DisconnectSeconds']<=rating['IcwSeconds']);check(peak<=rating['IcmPeakKA']);check(pr['BreakingCapacityKA']>=ik);check(pr['BreakerSnapshot']['Rating']['CapacityKA']==pr['BreakingCapacityKA']);check(pr['BreakerSnapshot']['Rating']['RatedA']==c['BreakerA']==25);check(rating['RatedA']==40)
fuse=next(x for x in table if x['Family']=='gG');iz=a['AmpacityA']*a['TemperatureFactor']*a['GroupingFactor']*a['Parallel'];check(ib<=float(fuse['RatedA'])<=iz);check(float(fuse['I2A'])<=1.45*iz);check(float(fuse['BreakingKA'])>=ik)
materials=list(csv.DictReader((r/'materiais.csv').open(encoding='utf-8-sig'),delimiter=';'));selected=[x for x in materials if x.get('Categoria')=='Seccionador selecionado'];check(len(selected)==1 and selected[0]['Quantidade']=='1' and 'SW40' in selected[0]['Referência'])
result={'status':'PASS','comparacoes':n,'Ib_A':ib,'Ik_nominal_kA':ik,'pico_nominal_kA':peak,'switch_rated_A':rating['RatedA'],'primary_breaker_A':c['BreakerA'],'scope':'Tabela sintética; comparação independente de fusível e de suportabilidade/manobra do seccionador, sem certificação normativa.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False,indent=2))
