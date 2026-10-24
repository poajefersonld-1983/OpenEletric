from pathlib import Path
import json,sys,unicodedata,xml.etree.ElementTree as ET
import pymupdf,ezdxf
from ezdxf.lldxf.encoding import decode_dxf_unicode
r=Path(sys.argv[1]);rows=json.loads((r/'resultados.json').read_text());checks=0;cases=[]
def check(value,label):
 global checks
 checks+=1
 if not value:raise AssertionError(label)
def br(x):return f'{x:.3f}'.replace('.',',')
for row in rows:
 name=row['Name'];p=json.loads((r/(name+'.circuito')).read_text(encoding='utf-8-sig'));doc=pymupdf.open(r/(name+'.pdf'));text=unicodedata.normalize('NFC',''.join(page.get_text() for page in doc));expected=f"P={br(row['Kw'])} kW; Q={br(row['Kvar'])} kvar; S={br(row['Kva'])} kVA; FP={br(row['PF'])}"
 check(expected in text,name+' PDF totals match saved project results');check(expected in (r/(name+'.txt')).read_text(encoding='utf-8-sig'),name+' text totals');check('TOTAIS' in (r/(name+'.html')).read_text(encoding='utf-8-sig'),name+' HTML engineering output');check(doc.page_count>0,name+' PDF pages');check(all(any(font[0] and doc.extract_font(font[0])[3] for font in page.get_fonts(full=True)) for page in doc),name+' embedded fonts')
 drawing=ezdxf.readfile(r/(name+'.dxf'));audit=drawing.audit();check(not audit.errors and not audit.fixes,name+' independent DXF read without repairs');entities=[e for e in drawing.modelspace().query('TEXT') if e.dxf.layer=='RESULTS'];kwtexts=[decode_dxf_unicode(e.dxf.text) for e in entities if ' kW / ' in decode_dxf_unicode(e.dxf.text)];expectedrows={v['Id']:v for v in row['Circuits']};check(len(kwtexts)==len(p['Boards'][0]['Circuits']),name+' DXF root circuit count')
 for c,value in zip(p['Boards'][0]['Circuits'],kwtexts):
  power,reactive=value.replace(' kvar','').split(' kW / ');check(abs(float(power)-expectedrows[c['Id']]['P'])<=.000500001 and abs(float(reactive)-expectedrows[c['Id']]['Q'])<=.000500001,name+c['Id']+' DXF P/Q rounding')
 ET.parse(r/(name+'.svg'));check(True,name+' SVG XML readable');cases.append(dict(name=name,pdf_pages=doc.page_count,dxf_entities=len(drawing.modelspace()),pdf_totals=True,dxf_no_repair=True))
 if name in ['01-bombeamento','03-forno','10-industria-tres-niveis']:
  for i,page in enumerate(doc):
   if f"P={br(row['Kw'])} kW" in page.get_text():page.get_pixmap(matrix=pymupdf.Matrix(1.2,1.2)).save(r/(name+'-totais.png'));break
 check(text.count('[Falha]')>=row['ChecksFailed'],name+' design failures visible in PDF')
report=dict(status='PASS',checks=checks,cases=cases,scope='PDF/TXT totals, readable paginated embedded fonts, root DXF P/Q and SVG structural output. Deliberately undersized oven remains a flagged design case.')
(r/'verificacao-saidas-independente.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(json.dumps(report,ensure_ascii=False,indent=2))
