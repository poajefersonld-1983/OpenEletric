from pathlib import Path
import ast,json,csv,math,cmath,sys
r=Path(sys.argv[1]);qa=Path(__file__).resolve().parent;n=0
# Reuse only the independently derived electrical formulas, not the C# implementation.
parsed=ast.parse((qa/'validar-lacos.py').read_text());module=ast.Module(body=[node for node in parsed.body if isinstance(node,ast.FunctionDef) and node.name in ('z','emf','evaluate')],type_ignores=[]);env={'math':math,'cmath':cmath};exec(compile(module,'electrical-independent','exec'),env)

def check(ok,label=''):
 global n
 assert ok,label;n+=1

def near(a,b,label):check(abs(float(a)-b)<=1e-8*max(1,abs(b)),label)
def residual_current(p,c,f,current,star):
 if f['Kind']==0:return 0j
 if f['Kind']==1:return current
 if f['Kind']==2:
  earth=p['GroundingSource'];s=p['LoopSource'];return 0j if earth['NeutralConnection']==2 else -star/(complex(earth['GroundingROhm'],earth['GroundingXOhm'])+complex(s['PEROhm'],s['PEXOhm'])+earth['SourceRbOhm'])
 owner={v['Id']:b for b in p['Boards'] for v in b['Circuits']};incoming={v['ChildBoardId']:v for b in p['Boards'] for v in b['Circuits'] if v['ChildBoardId']};second=next(v for b in p['Boards'] for v in b['Circuits'] if v['Id']==f['SecondCircuitId']);chain={second['Id']};board=owner[second['Id']]
 while board['Id'] in incoming:
  feeder=incoming[board['Id']];chain.add(feeder['Id']);board=owner[feeder['Id']]
 return 0j if c['Id'] in chain else current

def verify(p,row):
 c=next(v for b in p['Boards'] for v in b['Circuits'] if v.get('ContactTimeStudy') is not None);s=c['ContactTimeStudy'];rule=s['Rule'];f=c['LoopFault'];current,z,metal,reference,star=env['evaluate'](p,c,f);check('contact-time-v1' in p['RequiredCapabilities']);check(s['Engine']=='cs-contato-tempo-v1');check(rule['Reference'] and rule['Edition'] and rule['Clause'] and rule['Conditions']);check(rule['Scheme']==p['GroundingSource']['Scheme']);check(rule['CircuitClass']==(2 if c['ChildBoardId'] else 1));u0=p['Source']['LineVoltage']/math.sqrt(3);check(rule['MinimumU0V']<=u0<=rule['MaximumU0V']);check(not rule['UsesRatedCurrentLimit'] or c['BreakerA']<=rule['MaximumRatedA']);near(row['IfaultA'],abs(current),row['Case']+' fault');fail=False;pending=False;time=None;operate=None
 if metal is not None:near(row['TouchV'],abs(metal),row['Case']+' touch')
 else:check(row['TouchV']=='',row['Case']+' unknown touch not invented')
 if rule['Mode']==2:check(bool(rule['NoPotentialComparisonReason']))
 else:
  check(rule['PotentialReference']=={'Unknown':0,'RemoteEarth':1,'SourceNeutral':2}[reference],row['Case']+' reference');check(rule['TouchLimitV'] is not None)
  if rule['Mode'] in (1,3):fail|=metal is not None and abs(metal)>rule['TouchLimitV']
 if rule['Mode']==3:
  check(p['GroundingSource']['Scheme']==5 and f['Kind']==2);check(s['IMDSignalsFirstFault'] and s['FaultLocationAndRepairPlan']);check(bool(s['IMDReferenceAndTest']) and bool(s['MaintenancePlanReference']));check(rule['MaximumDisconnectSeconds'] is None);check(row['IoperateA']=='' and row['TimeUpperS']=='')
 else:
  operate=abs(residual_current(p,c,f,current,star)) if s['Device']==3 else abs(current);near(row['IoperateA'],operate,row['Case']+' operating current')
  if s['Device']==3:
   bind=c['ResidualSelection'];rating=bind['Rating'];check(rating['Poles'] in (4,5),row['Case']+' neutral included');check(c['BreakerA']<=rating['RatedA']);check(rating['ConditionalKA']>=abs(current)/1000);delta=rating['DeltaMilliA']/1000;fail|=operate<delta
   if p['GroundingSource']['Scheme']==1:check(abs(metal/current)*delta<=rule['TouchLimitV'])
  else:fail|=operate<(c['SwitchgearSelection']['Rating']['RatedA'] if s['Device']==2 else c['BreakerA'])
  if s['EvidenceMode']==0:
   e=s['Evidence'];check(e['Device']==s['Device']);check(e['CoversTotalClearing']);check(e['VoltageV']==c['Voltage'] and e['FrequencyHz']==p['Frequency']);check(len(e['DeviceConfiguration'])==64);check(bool(e['Source']) and bool(e['DataVersion']) and bool(e['Conditions']));covered=e['MinimumCurrentA']<=operate<=e['MaximumCurrentA']
   if covered:time=e['UpperSeconds']
   else:pending=True
  else:
   check(s['AttachedCurveCoversTotalClearing'] and bool(s['AttachedCurveDocument']));points=c['ProtectionResponses']['Time']['Curve']['Points'];check(points[0]['CurrentA']<=operate<=points[-1]['CurrentA']);right=next(pt for pt in points if pt['CurrentA']>=operate);idx=points.index(right)
   if right['CurrentA']==operate:time=right['Upper']
   else:
    left=points[idx-1];fraction=math.log(operate/left['CurrentA'])/math.log(right['CurrentA']/left['CurrentA']);time=math.exp(math.log(left['Upper'])+fraction*math.log(right['Upper']/left['Upper']))
  if time is not None:near(row['TimeUpperS'],time,row['Case']+' time');fail|=time>rule['MaximumDisconnectSeconds']
  else:check(row['TimeUpperS']=='')
 if p['GroundingSource']['Scheme']==5:check(s['IMDSignalsFirstFault'] and s['FaultLocationAndRepairPlan']);check(bool(s['IMDReferenceAndTest']) and bool(s['MaintenancePlanReference']))
 expected='Não atende ao caso' if fail else 'Dados incompletos' if pending else 'Atende comparações do caso declarado';check(row['Status']==expected,row['Case']+' status')
rows=list(csv.DictReader((r/'resultados.csv').open(),delimiter=';'));check(len(rows)>=8)
for row in rows:verify(json.loads((r/(row['Case']+'.circuito')).read_text(encoding='utf-8-sig')),row)
p=json.loads((r/'origem.circuito').read_text(encoding='utf-8-sig'));c=next(v for b in p['Boards'] for v in b['Circuits'] if v.get('ContactTimeStudy'));record=list(csv.DictReader((r/'tempos.csv').open(encoding='utf-8-sig'),delimiter=';'))[0];check(record['Device']=='SelectedResidual');check(record['CoversTotalClearing'].lower()=='true' and record['IncludesExternalActuator'].lower()=='true');check(record['DeviceConfiguration']==c['ContactTimeStudy']['Evidence']['DeviceConfiguration']);near(record['UpperSeconds'],.04,'relay complete upper bound');check(c['ResidualSelection']['Rating']['SensorReference']=='CT-QA' and c['ResidualSelection']['Rating']['ActuatorReference']=='TRIP-QA')
result={'status':'PASS','comparacoes':n,'cases':len(rows),'scope':'Synthetic rule scopes, independent loop/residual sums, total clearing bounds/log envelope, IT monitoring; no standard certification.'};(r/'verificacao-independente.json').write_text(json.dumps(result,ensure_ascii=False,indent=2));print(result)
