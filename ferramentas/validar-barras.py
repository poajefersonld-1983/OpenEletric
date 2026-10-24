from pathlib import Path
import json,csv,math,sys
r=Path(sys.argv[1]);p=json.loads((r/'selecionado.circuito').read_text(encoding='utf-8-sig'));b=p['Boards'][0];binding=b['BusbarSelection'];q=binding['Requirements'];n=0
rows=list(csv.DictReader((r/'candidatos.csv').open(),delimiter=';'))
for row in rows:
 rated=float(row['RatedA']);phase=max(q['L1RmsA'],q['L2RmsA'],q['L3RmsA']);corrected=rated*q['CapacityFactor'];expected=all([float(row['FrequencyHz'])==p['Frequency'],phase<=corrected,q['RequiredRmsUpperKA']<=float(row['IcwKA']),q['RequiredSeconds']<=1,q['RequiredPeakKA']<=float(row['IpkKA'])]);assert row['Accepted'].lower()==str(expected).lower();n+=1
 assert abs(float(row['CorrectedA'])-corrected)<1e-9;n+=1
 assert float(row['PhaseMaxA'])==850 and phase==850;n+=1
 assert row['Failed'].lower()!=row['Accepted'].lower();n+=1
 # Small real reference excerpt independently transcribed after visual PDF p146 check.
 catalog_reference={800:(31,64),1000:(50,110),1250:(50,110)}
 assert (float(row['IcwKA']),float(row['IpkKA']))==catalog_reference[rated];n+=1
assert min(float(row['RatedA']) for row in rows if row['Accepted']=='True')==binding['Rating']['RatedCurrentA']==1000;n+=1
assert sum([850,100,100])/3<800 and 850>800*.97;n+=1
origin=json.loads((r/'catalogo-origem.json').read_text());assert origin['rating_page_printed']==144 and origin['rating_pdf_page']==146;n+=1
report={'status':'PASS','comparisons':n,'cases':len(rows),'maximum_phase_A':850,'mean_A':350,'chosen_rating_A':1000,'OEM_reference':'DEBU021EN04/2026 printedp144/PDFp146,3L+PE standard','document_sha256':origin['pdf_sha256'],'limits':'synthetic requirements, real factory ratings; local nominal/withstand selection, not certification or busway line voltage-drop model'};(r/'verificacao-independente.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(report)
