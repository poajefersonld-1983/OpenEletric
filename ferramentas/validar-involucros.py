from pathlib import Path
import csv,json,hashlib,re,sys,xml.etree.ElementTree as ET
q=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).parent;out=Path(sys.argv[2]) if len(sys.argv)>2 else q/'ferramentas-qa/bin/referencias-involucros';checks=[]
def check(ok,label):
 if not ok:raise AssertionError(label)
 checks.append(label)
sha=hashlib.sha256((q/'mistral.pdf').read_bytes()).hexdigest()
text=(q/'mistral.txt').read_text();page=text.split('\f')[13];opaque=page.split('Avec porte opaque')[1].split('Avec porte transparente')[0]
rows=list(csv.DictReader((out/'abb-mistral41w.csv').open(),delimiter=';'))
for r in rows:
 match=re.search(r'(12|24|36)\s+297\s*x\s*(262|387|512)\s*x\s*119\s+Lisse?[^\n]*'+r['Reference'],opaque)
 # Table continues merged columns; use the row's explicit values instead of inferred external dimensions.
 line=next(x for x in opaque.splitlines() if r['Reference'] in x)
 check(r['DocumentSha256']==sha,r['Reference']+' PDF hash')
 for val in [r['HeightMm'],r['WidthMm'],r['DepthMm'],r['AnnotatedDissipationW'],r['AnnotatedMaxCurrentA']]:check(val in line,r['Reference']+' table value '+val)
 check(int(r['Rows'])*int(r['ModulesPerRow'])==int(line.strip().split()[0]),r['Reference']+' capacity explicitly documented')
 check(r['ModulePitchMm']=='17.5' and '17.5 mm' in page,'module pitch in table')
 if int(r['Rows'])>1:check('125' in line and r['RowPitchMm']=='125','rail pitch in table')
for name in ['um-trilho','dois-trilhos','tres-trilhos']:
 data=json.loads((out/(name+'.circuito')).read_text(encoding='utf-8-sig'));b=data['Boards'][0];e=b['Enclosure'];rating=b['EnclosureCatalogSelection']['Rating']
 check('enclosure-catalog-v1' in data['RequiredCapabilities'],name+' capability')
 check(e['Rows']==rating['Rows'] and e['ModulesPerRow']==12,name+' saved catalog capacity')
 occupied=set();expected={}
 for c in b['Circuits']:
  n=c['DinFootprint']['WidthModules'];check(c['DinModules']==n and c['DinFootprint']['ModulePitchMm']==17.5,name+' declared footprint')
  if c['DinRow'] and c['DinColumn']:pos=(c['DinRow']-1,c['DinColumn']-1)
  else:
   pos=next(( (row,col) for row in range(e['Rows']) for col in range(13-n) if all((row,j) not in occupied for j in range(col,col+n))),None)
  check(pos is not None,name+' independent packing');row,col=pos
  check(all((row,j) not in occupied for j in range(col,col+n)),name+' no overlap')
  occupied.update((row,j) for j in range(col,col+n));expected[c['Name']]=((297-12*17.5)/2+col*17.5,30+row*125,n*17.5-1)
 root=ET.parse(out/(name+'.svg')).getroot();ns={'s':'http://www.w3.org/2000/svg'}
 for g in root.findall('s:g',ns):
  title=g.find('s:title',ns).text;rect=g.find('s:rect',ns);ex=expected[title]
  check(tuple(float(rect.get(k)) for k in ['x','y','width'])==ex,name+' independent SVG coordinates '+title)
 check('sem cotas de montagem/furação' in ''.join(root.itertext()),name+' schematic limitation')
result={'status':'PASS','checks':len(checks),'assertions':checks,'catalog_pdf_sha256':sha,'independent_method':'Original ABB p14 table + separate grid occupancy + XML geometry; no C# evaluator called'}
(out/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print('PASS:',len(checks),'independent enclosure checks')
