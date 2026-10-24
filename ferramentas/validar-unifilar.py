from pathlib import Path
import json,csv,math,sys,xml.etree.ElementTree as ET
r=Path(sys.argv[1]);p=json.loads((r/'rede.circuito').read_text(encoding='utf-8-sig'));boards={b['Id']:b for b in p['Boards']};results={x['Id']:x for x in csv.DictReader((r/'resultados.csv').open(),delimiter=';')};n=0

def check(ok):
 global n
 assert ok;n+=1

def near(a,b):check(math.isclose(float(a),b,rel_tol=1e-10,abs_tol=1e-10))
def load(b):return sum(load(boards[c['ChildBoardId']]) if c['ChildBoardId'] else c['PowerKw']*c['DemandFactor'] for c in b['Circuits'])
def walk(b,drop=0):
 for c in b['Circuits']:
  power=load(boards[c['ChildBoardId']]) if c['ChildBoardId'] else c['PowerKw']*c['DemandFactor'];amp=power*1000/(c['Voltage']*(math.sqrt(3) if c['Phases']==1 else 1));cable=c['Cable'];res=cable['ResistanceOhmKm'] or (23.7/c['CableMm2'] if c['CableMm2'] else 0);local=amp*res*c['LengthM']/1000*(math.sqrt(3) if c['Phases']==1 else 2)/c['Voltage']*100;total=drop+local
  near(results[c['Id']]['DemandKw'],power);near(results[c['Id']]['CurrentA'],amp);near(results[c['Id']]['DropPercent'],total)
  if c['ChildBoardId']:walk(boards[c['ChildBoardId']],total)
children={c['ChildBoardId'] for b in boards.values() for c in b['Circuits'] if c['ChildBoardId']};roots=[b for b in boards.values() if b['Id'] not in children];near(sum(load(b) for b in roots),11)
for b in roots:walk(b)
a=boards['A']['Circuits'][0];check(a['ChildBoardId']=='B');check(boards['B']['Circuits'][0]['ChildBoardId']=='D');near(a['DiagramX'],420);near(a['DiagramY'],180)
svg=ET.parse(r/'raiz.svg').getroot();group=next(g for g in svg.iter() if g.get('data-circuit-id')==a['Id']);texts=[t.text for t in group.iter() if t.tag.endswith('text')];check('→ quadro B' in texts);check(any('9 kW' in t for t in texts if t));check(any('12,99 A' in t for t in texts if t));check(any(g.get('x')=='390' for g in group.iter() if g.tag.endswith('rect')))
result={'status':'PASS','comparacoes':n,'scope':'Rede radial de três níveis; agregação, corrente, queda nominal e geometria/ligação SVG. Não é fluxo de tensão não linear.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(result)
