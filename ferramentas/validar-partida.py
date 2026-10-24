#!/usr/bin/env python3
"""Verifica partida por Kirchhoff/fasores, sem importar o motor C# (somente stdlib)."""
from pathlib import Path
import csv,json,math,sys
root=Path(sys.argv[1]);reports=[]
for case in ['terminal','radial']:
 p=json.loads((root/(case+'.circuito')).read_text(encoding='utf-8-sig'))
 measured=next(csv.DictReader((root/(case+'.csv')).open(),delimiter=';'))
 boards={b['Id']:b for b in p['Boards']};all_c=[c for b in boards.values() for c in b['Circuits']]
 motor=next(c for c in all_c if c['Id']==measured['CircuitId']);data=motor['MotorStarting'];source=p['Source']
 assert source['Model']==2 and all(c['Phases']==1 for c in all_c),'casos de referência exigem fonte R/X e circuitos equilibrados'
 current_start=data['StartingCurrentA']*complex(data['StartingPowerFactor'],-math.sqrt(1-data['StartingPowerFactor']**2))
 def current(c):
  if c['ChildBoardId']:return sum((current(child) for child in boards[c['ChildBoardId']]['Circuits']),0j)
  if c is motor:return current_start
  real=c['PowerKw']*c['DemandFactor']
  reactive=c['ReactiveKvarOverride']*c['DemandFactor'] if c.get('ReactiveKvarOverride') is not None else real*math.sqrt(1-c['PowerFactor']**2)/c['PowerFactor']
  return complex(real,-reactive)*1000/(math.sqrt(3)*c['Voltage'])
 def impedance(c):
  a=c['Cable'];r=a['ResistanceOhmKm'] if a['ResistanceOhmKm']>0 else (23.7 if a['Material']==0 else 37.6)/c['CableMm2']
  return complex(r,a['ReactanceOhmKm'])*c['LengthM']/1000/a['Parallel']
 children={c['ChildBoardId'] for c in all_c if c['ChildBoardId']}
 total=sum((current(c) for b in boards.values() if b['Id'] not in children for c in b['Circuits']),0j)
 parent={c['ChildBoardId']:(b,c) for b in boards.values() for c in b['Circuits'] if c['ChildBoardId']}
 board=next(b for b in boards.values() if motor in b['Circuits']);path=[motor]
 while board['Id'] in parent:
  board,feeder=parent[board['Id']];path.append(feeder)
 phase_v=source['LineVoltage']/math.sqrt(3)
 zsource=complex(source['DefinedPhaseROhm'],source['DefinedPhaseXOhm'])
 terminal=phase_v-zsource*total-sum((impedance(c)*current(c) for c in path),0j)
 projected=100*(phase_v-terminal.real)/phase_v
 magnitude=100*(phase_v-abs(terminal))/phase_v
 nominal=(data['ShaftKw']/data['Efficiency'])*1000/(math.sqrt(3)*motor['Voltage']*motor['PowerFactor'])
 assert math.isclose(float(measured['NominalA']),nominal,rel_tol=1e-10,abs_tol=1e-9),(case,'nominal')
 assert math.isclose(float(measured['StartingDropPercent']),projected,rel_tol=1e-10,abs_tol=1e-9),(case,'partida',measured,projected)
 reports.append({'case':case,'status':'PASS','nominal_A':nominal,'projected_drop_percent':projected,'voltage_magnitude_drop_percent_fixed_currents':magnitude,'note':'A aprovação do modelo C# usa a projeção linear; o módulo é mostrado apenas como comparação do fasor com correntes fixas.'})
(root/'verificacao-fasorial.json').write_text(json.dumps({'engine':'Python stdlib complex / Kirchhoff','cases':reports},ensure_ascii=False,indent=2))
print(json.dumps(reports,ensure_ascii=False,indent=2))
