"""Compare product inspection with independent original JPCAD SDK control specifications.
Usage: python validar-amb.py FIXTURE_DIR [NATIVE_ORACLE_JSON]
The official prototype is optional and must be obtained from ABB, not redistributed.
"""
from pathlib import Path
import json,sys,math,hashlib,struct
root=Path(sys.argv[1]);rows=json.loads((root/'inspection-results.json').read_text());rows={x['File']:x for x in rows};checks=0

def check(value,name):
 global checks
 checks+=1
 if not value:raise AssertionError(name)
def close(a,b):return math.isclose(a,b,rel_tol=1e-12,abs_tol=1e-10)
cases=[dict(name='native-control',values=[-12.25,3.5,47,-8.125,10,20,4.75,7,8,2.5,0,0,'Controle AMB'])]+json.loads((root/'native-cases.json').read_text())
for case in cases:
 name=case['name'];v=case['values'];r=rows[name+'.amb'];data=(root/(name+'.amb')).read_bytes();check(data[:32]==b'AMOEBA DATA FILE, ver 1.1'.ljust(32,b'\0'),name+' original signature');check(r['Records']==7,name+' records');ents={e['Type']:e for e in r['Entities']};line=ents['LINE'];circle=ents['CIRCLE']
 for key,expected in zip(['X','Y','X2','Y2'],v[:4]):check(close(line[key],expected),name+' line '+key)
 for key,expected in zip(['X','Y','Radius'],v[4:7]):check(close(circle[key],expected),name+' circle '+key)
 if v[11]==0:
  text=ents['TEXT']
  for key,expected in zip(['X','Y','Height','Rotation'],[v[7],v[8],v[9],math.degrees(v[10])%360]):check(close(text[key],expected),name+' text '+key)
  check(text['Text']==v[12],name+' encoded text')
 else:check('TEXT' not in ents and any(p['Key']=='TEXT' for p in r['Unsupported']),name+' unsupported alignment')
 product='product-roundtrip.amb' if name=='native-control' else name+'-product.amb';check(data==(root/product).read_bytes(),name+' all bytes retained')
 if 'sha256' in case:check(hashlib.sha256(data).hexdigest()==case['sha256'],name+' native fixture hash')
if 'Prototyp.amb' in rows:
 p=rows['Prototyp.amb'];classes={x['Key']:x['Value'] for x in p['Classes']};check(p['Records']==22906,'original prototype records');check(classes['BLOCK']==203 and classes['ATTR']==9881,'nested symbols and metadata counted');check(len(p['Entities'])==0,'block geometry not misrepresented as world entities');check((root/'Prototyp.amb').read_bytes()==(root/'prototype-roundtrip.amb').read_bytes(),'official prototype all bytes retained')
if len(sys.argv)>2:
 oracle=json.loads(Path(sys.argv[2]).read_text())
 for r in oracle:
  check(r['equal'] and r['native_before']==r['native_after'],'original SDK product roundtrip '+r['original']);check(hashlib.sha256((root/r['original']).read_bytes()).hexdigest()==r['sha256'],'oracle sample hash '+r['original'])
result=dict(checks=checks,passed=True,scope='AMB 1.1 structural inspection, limited preview and exact byte reexport; no semantic electrical import, editing or checksum authentication')
(root/'independent-results.json').write_text(json.dumps(result,indent=2));print(json.dumps(result))
