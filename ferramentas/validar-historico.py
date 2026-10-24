from pathlib import Path
import csv,json,math,sys
r=Path(sys.argv[1]);source=list(csv.DictReader((r/'stamford-symmetric-50hz.csv').open(),delimiter=';'));source=[(float(x['TimeSeconds']),float(x['CurrentA'])) for x in source];queries=list(csv.DictReader((r/'consultas-stamford.csv').open(),delimiter=';'));n=0
for row in queries:
 t=float(row['TimeSeconds']);actual=float(row['CurrentA']);i=next(i for i,(time,current) in enumerate(source) if time>=t)
 if i==0 or source[i][0]==t:expected=source[i][1]
 else:
  t1,i1=source[i-1];t2,i2=source[i];exponent=math.log(i2/i1)/math.log(t2/t1);expected=i1*(t/t1)**exponent
 assert abs(actual-expected)<=1e-8*max(1,expected),(t,actual,expected);n+=1
# Exact documented plateau annotation provides an external sanity check on digitization at long times.
metadata=json.loads((r/'stamford-digitization.json').read_text());plateau=metadata['annotation_sustained_A'];assert all(abs(i-plateau)/plateau<.03 for t,i in source if t>=2);n+=1
assert source[0][1]>source[6][1] and source[6][1]<source[-1][1];n+=1
report={'status':'PASS','comparisons':n,'queries':len(queries),'kind':'symmetric AC RMS, no instantaneous/asymmetric peak inferred','reference':metadata['reference'],'document_sha256':metadata['sha256_pdf'],'plateau_annotation_A':plateau,'digitized_plateau_A':source[-1][1],'reading_tolerance_fraction':.03};(r/'validacao-historico.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(report)
