from pathlib import Path
import json,hashlib,collections,xml.etree.ElementTree as ET,re,sys
import ezdxf
from ezdxf.lldxf.encoding import decode_dxf_unicode
r=Path(sys.argv[1]) if len(sys.argv)>1 else Path(__file__).resolve().parent.parent/'app/verificacao/referencias-dxf';checks=[]
def check(good,label):
 if not good:raise AssertionError(label)
 checks.append(label)
for p in sorted(r.glob('simbolo-*.dxf'))+ [r/'unifilar.dxf',r/'frontal.dxf',r/'frontal-pendente.dxf',r/'literal.dxf']:
 d=ezdxf.readfile(p);audit=d.audit();check(not audit.errors,p.name+' external audit no errors');check(not audit.fixes,p.name+' external audit no repairs');check(d.dxfversion=='AC1009',p.name+' R12 version');check(all(e.dxftype() in ['LINE','CIRCLE','TEXT'] for e in d.modelspace()),p.name+' declared entity types')
for version in ['R12','R2010']:
 before=r/('externo-'+version+'.dxf');after=r/('roundtrip-'+version+'.dxf');check(before.read_bytes()==after.read_bytes(),version+' byte-exact independent source');a=ezdxf.readfile(after);check(len(a.modelspace())==3,version+' reopened independently')
check((r/'externo-entidades-adicionais.dxf').read_bytes()==(r/'preservado-adicionais.dxf').read_bytes(),'unpreviewed entities and metadata retained byte exact')
unifilar=ezdxf.readfile(r/'unifilar.dxf');check(any(decode_dxf_unicode(e.dxf.text)=='Luz Ω 漢字' for e in unifilar.modelspace().query('TEXT')),'independent Unicode decoder')
literal=ezdxf.readfile(r/'literal.dxf');check(decode_dxf_unicode(next(iter(literal.modelspace())).dxf.text)==r'Literal \U+0041 %%d Ω','independent decoder preserves literal escapes')
# Compare exported lines and circles against the existing SVG generator, independent from C# DXF serialization.
ns={'s':'http://www.w3.org/2000/svg'};root=ET.parse(r/'unifilar.svg').getroot();svg_lines=[];svg_circles=[]
for e in root.findall('.//s:path',ns):
 match=re.fullmatch(r'M ([\d.+-]+) ([\d.+-]+) L ([\d.+-]+) ([\d.+-]+)',e.get('d',''))
 if match:
  x,y,x2,y2=map(float,match.groups());svg_lines.append((x,-y,x2,-y2))
for e in root.findall('.//s:circle',ns):svg_circles.append((float(e.get('cx')),-float(e.get('cy')),float(e.get('r'))))
dxf_lines=[(e.dxf.start.x,e.dxf.start.y,e.dxf.end.x,e.dxf.end.y) for e in unifilar.modelspace().query('LINE')];dxf_circles=[(e.dxf.center.x,e.dxf.center.y,e.dxf.radius) for e in unifilar.modelspace().query('CIRCLE')]
check(collections.Counter(svg_lines)==collections.Counter(dxf_lines),'all SVG unifilar line geometry equals independent DXF read')
check(collections.Counter(svg_circles)==collections.Counter(dxf_circles),'all SVG symbol circles equal independent DXF read')
for idx in range(9):
 root=ET.parse(r/('simbolo-'+str(idx)+'.svg')).getroot();lines=[];circles=[]
 for e in root.findall('.//s:path',ns):
  m=re.fullmatch(r'M ([\d.+-]+) ([\d.+-]+) L ([\d.+-]+) ([\d.+-]+)',e.get('d',''))
  if m:
   x,y,x2,y2=map(float,m.groups());lines.append((x,-y,x2,-y2))
 for e in root.findall('.//s:circle',ns):circles.append((float(e.get('cx')),-float(e.get('cy')),float(e.get('r'))))
 doc=ezdxf.readfile(r/('simbolo-'+str(idx)+'.dxf'))
 check(collections.Counter(lines)==collections.Counter((e.dxf.start.x,e.dxf.start.y,e.dxf.end.x,e.dxf.end.y) for e in doc.modelspace().query('LINE')),'symbol '+str(idx)+' line geometry')
 check(collections.Counter(circles)==collections.Counter((e.dxf.center.x,e.dxf.center.y,e.dxf.radius) for e in doc.modelspace().query('CIRCLE')),'symbol '+str(idx)+' circle geometry')
frontal=ezdxf.readfile(r/'frontal.dxf');rails=[e for e in frontal.modelspace().query('LINE') if e.dxf.layer=='RAILS'];check(len(rails)==2 and abs(rails[1].dxf.start.y-rails[0].dxf.start.y)==125,'independent front rail pitch')
check(all(abs(e.dxf.start.x-43.5)<1e-9 and abs(e.dxf.end.x-253.5)<1e-9 for e in rails),'independent usable width210mm not outer297mm')
check(any(e.dxf.text.startswith('PENDENTE:') for e in ezdxf.readfile(r/'frontal-pendente.dxf').modelspace().query('TEXT')),'occupancy warning remains in drawing')
result={'status':'PASS','checks':len(checks),'assertions':checks,'reader':'ezdxf '+ezdxf.__version__,'method':'external R12/R2010 generated fixtures, independent reader/audit, SVG geometry comparison and byte equality','limits':['No ABB AMB compatibility evidence','No AutoCAD/ABB application executed','Inspection is not circuit reconstruction','R12 has no automatic units; front explicitly mm, unifilar abstract']}
(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print('PASS:',len(checks),'independent DXF checks')
