#!/usr/bin/env python3
"""Confere o par de fatores e a ampacidade contra JSON/CSV salvos; stdlib."""
import csv,json,math,sys
from pathlib import Path
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=p['Boards'][0]['Circuits'][0];a=c['Cable'];s=a['FactorsSnapshot'];conditions=s['Conditions'];table={row['Id']:row for row in csv.DictReader((r/'fatores.csv').open(encoding='utf-8-sig'),delimiter=';')};checks=[]
def check(ok,label):
 if not ok:raise AssertionError(label)
 checks.append(label)
check(p['FileVersion']==4 and 'cable-factors-v1' in p['RequiredCapabilities'],'formato e capacidade protegem dados')
for name,kind in [('Temperature',0),('Grouping',1)]:
 row=s[name];origin=table[row['Id']];check(row['Kind']==kind,'tipo '+name)
 for key in ['Source','DataVersion','AmpacityReference']:check(row[key]==origin[key],name+' '+key+' preservado')
 check(row['Material']==a['Material'] and row['Insulation']==a['Insulation'],'material/isolação '+name)
 check(row['Installation']==a['InstallationMethod'] and row['AmpacityReference']==a['Reference'],'instalação/base '+name)
 check(row['LoadedConductors']==conditions['LoadedConductors'],'condutores '+name)
 check(row.get('Arrangement','')==conditions['Arrangement'] if kind==1 else not row.get('Arrangement') or row['Arrangement']==conditions['Arrangement'],'arranjo '+name)
 check((row['MinSectionMm2']==0 or c['CableMm2']>=row['MinSectionMm2']) and (row['MaxSectionMm2']==0 or c['CableMm2']<=row['MaxSectionMm2']),'seção '+name)
 check(math.isclose(row['Factor'],float(origin['Factor']),rel_tol=1e-12),'valor de fator '+name)
 check(row['AmbientC']==conditions['AmbientC'] if kind==0 else row['GroupCount']==conditions['GroupCount'],'condição específica '+name)
check(a['TemperatureFactor']==s['Temperature']['Factor'] and a['GroupingFactor']==s['Grouping']['Factor'],'valores ativos coerentes com referências')
expected=a['AmpacityA']*float(table[s['Temperature']['Id']]['Factor'])*float(table[s['Grouping']['Id']]['Factor'])*a['Parallel']
measured=float((r/'resultado.txt').read_text());check(math.isclose(expected,measured,rel_tol=1e-12),'produto de ampacidade e paralelos')
result={'status':'PASS','verificacoes':len(checks),'Iz_A':expected,'ambient_C':conditions['AmbientC'],'group_count':conditions['GroupCount'],'scope':'Dados sintéticos; verificação de aplicação/rastreabilidade, sem homologação da tabela.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(json.dumps(result,ensure_ascii=False,indent=2))
