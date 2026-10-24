from pathlib import Path
import json,sys
r=Path(sys.argv[1]);p=json.loads((r/'rede.circuito').read_text(encoding='utf-8-sig'));n=0

def check(ok):
 global n
 assert ok;n+=1
s=p['GroundingSource'];check(p['FileVersion']==4);check('earthing-conductors-v1' in p['RequiredCapabilities']);check(s['Engine']=='cs-aterramento-condutores-v1');check(s['Scheme']==4);check(p['Earthing']=='TN-C-S');check(s['NeutralConnection']==1);check(s['NeutralDistributed']);check(s['InstallationRaOhm']==100);check(s['SourceRbOhm']==2);check(s['GroundingROhm'] is None and s['GroundingXOhm'] is None);check(s['Reference']=='Projeto QA Ω');check(s['DataVersion']=='2026.10');boards={b['Id']:b for b in p['Boards']};incoming={c['ChildBoardId']:(b,c) for b in boards.values() for c in b['Circuits'] if c['ChildBoardId']}
def separated(b):
 if b['SeparatesPEN']:return True
 return separated(incoming[b['Id']][0]) if b['Id'] in incoming else False
check(not separated(boards['A']));check(separated(boards['B']));check(separated(boards['D']));check(incoming['B'][1]['Conductors']['HasPEN']);check(not incoming['B'][1]['Conductors']['HasNeutral']);check(not incoming['B'][1]['Conductors']['HasPE']);check(boards['B']['Circuits'][0]['Conductors']['HasNeutral']);check(boards['B']['Circuits'][0]['Conductors']['HasPE']);check(not boards['B']['Circuits'][0]['Conductors']['HasPEN']);check(boards['B']['Circuits'][0]['Protection']['RcdMilliA']==30);check(boards['B']['Circuits'][0]['Protection']['EarthResistanceOhm']==9999)
for b in boards.values():
 for c in b['Circuits']:
  w=c['Conductors']
  if w:
   check(not(w['HasPEN'] and (w['HasNeutral'] or w['HasPE'])))
   if w['HasPEN']:check(not separated(b))
   if w['Insulation']==1:check(w['HasPE'] or w['HasPEN'])
  if c['Protection']['RcdMilliA']>0:check(separated(b))
result={'status':'PASS','comparacoes':n,'scope':'Cadastro sintético TN-C-S, separação e condutores; não é prova de continuidade física, corrente de falta ou tempo.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(result)
