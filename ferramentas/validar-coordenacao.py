#!/usr/bin/env python3
import csv,json,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));boards={b['Id']:b for b in p['Boards']};down=boards['Q2']['Circuits'][0];up=next(c for b in boards.values() for c in b['Circuits'] if c['ChildBoardId']=='Q2');bind=down['BackupEvidence'];ev=bind['Record'];row=next(csv.DictReader((r/'pares.csv').open(encoding='utf-8-sig'),delimiter=';'));n=0
def check(ok):
 global n
 assert ok
 n+=1
check(p['FileVersion']==4 and 'coordination-tables-v1' in p['RequiredCapabilities']);check(bind['Engine']=='cs-coordenacao-tabelas-v1');check(bind['UpstreamCircuitId']==up['Id'] and bind['DownstreamCircuitId']==down['Id']);check(ev['Kind']==1 and row['Kind']=='Backup')
for key in ['UpstreamReference','DownstreamReference','Issuer','Document','Page','DataVersion','Scope','UpstreamConfiguration','DownstreamConfiguration']:check(ev[key]==row[key])
for key in ['SystemVoltageV','FrequencyHz','MinimumCurrentA','LimitKA']:check(math.isclose(ev[key],float(row[key]),rel_tol=1e-12))
for device,key in [(up,'Upstream'),(down,'Downstream')]:
 rating=device['Protection']['BreakerSnapshot']['Rating'];check(ev[key+'Reference']==rating['Manufacturer']+' / '+rating['Reference']);check(rating['VoltageV']==device['Voltage'] and rating['FrequencyHz']==p['Frequency']);check(rating['Poles']==3 and rating['Metric']==0);check(len(ev[key+'Configuration'])==64)
s=p['Source'];z=s['VoltageFactor']*s['LineVoltage']/(math.sqrt(3)*s['ShortCircuitKA']*1000);sx=z/math.sqrt(1+s['RXRatio']**2);sr=sx*s['RXRatio'];a=up['Cable'];cr=a['ResistanceOhmKm']*up['LengthM']/1000/a['Parallel'];cx=a['ReactanceOhmKm']*up['LengthM']/1000/a['Parallel'];ik=s['VoltageFactor']*s['LineVoltage']/(math.sqrt(3)*abs(complex(sr+cr,sx+cx))*1000)
check(math.isclose(ik,float((r/'ik.txt').read_text()),rel_tol=1e-12));check(ev['SystemVoltageV']==boards['Q2']['Voltage'] and ev['FrequencyHz']==p['Frequency']);check(ev['MinimumCurrentA']<=ik*1000<=ev['LimitKA']*1000);check(down['Protection']['BreakingCapacityKA']<ik);check(up['Protection']['BreakingCapacityKA']>=s['ShortCircuitKA']);check('não é tabela certificada real' in ev['Scope'])
result={'status':'PASS','comparacoes':n,'Ik_nominal_kA':ik,'capacidade_individual_jusante_kA':down['Protection']['BreakingCapacityKA'],'limite_par_kA':ev['LimitKA'],'scope':'Dados sintéticos; consistência nominal do par e referência independente do curto, sem certificação/autenticidade da tabela.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False,indent=2))
