#!/usr/bin/env python3
"""Conferência independente dos artefatos contra o projeto JSON salvo; apenas stdlib."""
import csv,json,math,sys,xml.etree.ElementTree as ET
from html.parser import HTMLParser
from pathlib import Path
root=Path(sys.argv[1])
p=json.loads((root/'origem.circuito').read_text(encoding='utf-8-sig'))
boards={b['Id']:b for b in p['Boards']}
checks=[]
def check(ok,label):
 if not ok: raise AssertionError(label)
 checks.append(label)
def number(text): return float(text.removeprefix("'").replace(',','.'))
def near(text,value,label): check(abs(number(text)-value)<=.0050001,label)
def load(c):
 if c['ChildBoardId']:
  values=[load(x) for x in boards[c['ChildBoardId']]['Circuits']]
  return sum(x[0] for x in values),sum(x[1] for x in values)
 kw=c['PowerKw']*c['DemandFactor']
 kvar=c['ReactiveKvarOverride']*c['DemandFactor'] if c.get('ReactiveKvarOverride') is not None else kw*math.sqrt(1-c['PowerFactor']**2)/c['PowerFactor']
 return kw,kvar
rows=list(csv.reader((root/'dados.csv').open(encoding='utf-8-sig'),delimiter=';'))
check(rows[1][1]==p['Name'] and rows[1][3]==p['Client'],'identificação Unicode do CSV')
h=next(i for i,row in enumerate(rows) if row[0]=='Quadro ID')
records=[dict(zip(rows[h],row)) for row in rows[h+1:-1]]
check(len(records)==sum(len(b['Circuits']) for b in boards.values()),'todos os circuitos no CSV')
by_id={r['Circuito ID']:r for r in records}
terminal=[c for b in boards.values() for c in b['Circuits'] if not c['ChildBoardId']]
near(rows[2][5],sum(load(c)[0] for c in terminal),'total CSV sem duplicação dos alimentadores')
class TableReader(HTMLParser):
 def __init__(self): super().__init__(convert_charrefs=True);self.rows=[];self.row=None;self.cell=None
 def handle_starttag(self,tag,attrs):
  if tag=='tr':self.row=[]
  if tag=='td':self.cell=''
 def handle_data(self,data):
  if self.cell is not None:self.cell+=data
 def handle_endtag(self,tag):
  if tag=='td':self.row.append(self.cell);self.cell=None
  if tag=='tr' and self.row:self.rows.append(self.row);self.row=None
reader=TableReader();reader.feed((root/'dados.html').read_text())
html={row[0].split(' · ')[0]:row for row in reader.rows}
ns={'s':'http://www.w3.org/2000/svg'}
for b in boards.values():
 xml=ET.parse(root/(b['Id']+'.svg')).getroot()
 check(xml.find('s:title',ns).text==p['Name']+' — '+b['Name'],'título SVG '+b['Id'])
 groups={g.get('data-circuit-id'):g for g in xml.findall('.//s:g[@data-circuit-id]',ns)}
 check(set(groups)=={c['Id'] for c in b['Circuits']},'IDs SVG '+b['Id'])
 for i,c in enumerate(b['Circuits']):
  r=by_id[c['Id']];kw,kvar=load(c);current=math.hypot(kw,kvar)*1000/c['Voltage']/(math.sqrt(3) if c['Phases']==1 else 1)
  check(r['Quadro ID']==b['Id'] and r['Circuito']==c['Name'] and r['Observações']==c['Notes'],'identificação e notas CSV '+c['Id'])
  for column,value in [('Tensão (V)',c['Voltage']),('Demanda (kW)',kw),('Reativo (kvar)',kvar),('Corrente de projeto (A)',current),('Seção informada (mm²)',c['CableMm2'])]:near(r[column],value,column+' '+c['Id'])
  row=html[c['Id']];near(row[5],kw,'HTML demanda '+c['Id']);near(row[6],kvar,'HTML kvar '+c['Id']);near(row[8],current,'HTML corrente '+c['Id'])
  check(row[0]==c['Id']+' · '+c['Name'] and row[12]==c['Notes'],'identificação HTML '+c['Id'])
  group=groups[c['Id']];title=group.find('s:title',ns).text
  check(title==c['Id']+' · '+c['Name'],'título integral SVG '+c['Id'])
  texts=[e.text for e in group.findall('s:text',ns)]
  pq=next(t for t in texts if ' kW · ' in t);a,q=pq.replace(' kvar','').split(' kW · ');near(a,kw,'SVG kW '+c['Id']);near(q,kvar,'SVG kvar '+c['Id'])
  ai=next(t for t in texts if ' A · ' in t);amp,volts=ai.replace(' V','').split(' A · ');near(amp,current,'SVG corrente '+c['Id']);near(volts,c['Voltage'],'SVG tensão '+c['Id'])
  x=c['DiagramX'] if c['DiagramX']>0 else 95+(i%4)*218
  y=c['DiagramY'] if c['DiagramY']>0 else 115+(i//4)*310
  expected=f'M {x:g} {115+(i//4)*310:g} L {x:g} {y+30:g}'
  check(any(e.get('d')==expected for e in group.findall('.//s:path',ns)),'posição persistida SVG '+c['Id'])
check('Corrente de projeto (A)' in rows[h] and 'Reativo (kvar)' in rows[h],'unidades dos resultados CSV explícitas')
(root/'verificacao-independente.json').write_text(json.dumps({'status':'PASS','verificacoes':len(checks),'evidencias':checks},ensure_ascii=False,indent=2))
print('PASS:',len(checks),'conferências independentes JSON/CSV/HTML/SVG')
