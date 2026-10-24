from pathlib import Path
import json,csv,math,sys
r=Path(sys.argv[1]);p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=p['Boards'][0]['Circuits'][0];sel=c['ReturnSizing'];req=sel['Requirements'];n=0

def check(ok):
 global n
 assert ok;n+=1

def near(a,b):check(math.isclose(float(a),b,rel_tol=1e-10,abs_tol=1e-10))
check('return-sizing-v1' in p['RequiredCapabilities']);check(sel['Engine']=='cs-retornos-dimensionamento-v1');check(c['CableMm2']==35);check(req['NeutralMode']==1);check(req['ProtectiveMethod']==0);check(req['HarmonicsAssessed']);near(req['NeutralDesignRmsA'],20);near(req['NeutralProtectiveRatedA'],50);near(req['NeutralProtectiveI2A'],72.5);near(req['LetThroughA2s'],10000);near(req['ClearingSeconds'],.1);near(req['NeutralK'],115);check(bool(req['NeutralCurrentSource']));check(bool(req['NeutralProtectionReference']));check(len(req['ProtectionConfiguration'])==64);check(bool(req['RuleReference']) and bool(req['RuleEdition']) and bool(req['RuleConditions']))
minimum_n=max(16,math.sqrt(req['LetThroughA2s'])/req['NeutralK']);minimum_pe=c['CableMm2'] if c['CableMm2']<=16 else 16 if c['CableMm2']<=35 else c['CableMm2']/2
for slot,minimum in [('Neutral',minimum_n),('Protective',minimum_pe)]:
 row=sel[slot];near(row['SectionMm2'],16);check(row['SectionMm2']>=minimum);check(row['ResistanceOhmKm'] is None and row['ReactanceOhmKm'] is None);check(row['Source']=='Sintético QA');check(row['DataVersion']=='2026.10')
neutral=sel['Neutral'];check(req['NeutralAmpacityConditions']==neutral['Conditions']);check(req['NeutralDesignRmsA']<=req['NeutralProtectiveRatedA']<=neutral['CorrectedIzA']);check(req['NeutralProtectiveI2A']<=1.45*neutral['CorrectedIzA'])
comparisons=list(csv.DictReader((r/'comparacoes.csv').open(),delimiter=';'));check(len(comparisons)==8)
for row in comparisons:
 minimum=minimum_n if row['Role']=='Neutral' else minimum_pe;near(row['MinimumMm2'],minimum);check((row['Status']=='Atende critérios locais declarados')==(float(row['SectionMm2'])>=minimum))
rows=list(csv.DictReader((r/'condutores.csv').open(encoding='utf-8-sig'),delimiter=';'));check(len(rows)==2);check(all(row['ResistanceOhmKm']=='' and row['ReactanceOhmKm']=='' for row in rows));zero=list(csv.DictReader((r/'zero.csv').open(encoding='utf-8-sig'),delimiter=';'))[0];near(zero['ResistanceOhmKm'],0);check(zero['ReactanceOhmKm']=='')
materials=list(csv.DictReader((r/'materiais.csv').open(encoding='utf-8-sig'),delimiter=';'));returns=[row for row in materials if row.get('Categoria')=='Condutor de retorno selecionado'];check(len(returns)==2)
for row in returns:near(row['Quantidade'].replace(',','.'),c['LengthM']);check(row['Unidade']=='m de condutor');check('16 mm²' in row['Descrição'])
result={'status':'PASS','comparacoes':n,'min_neutral_mm2':minimum_n,'min_PE_mm2':minimum_pe,'scope':'Dados sintéticos: correnteRMS/proteção, seção por relação/adiabático e materiais; não é estudo de laço ou certificação.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(result)
