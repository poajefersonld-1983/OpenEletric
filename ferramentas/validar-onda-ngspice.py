from pathlib import Path
import subprocess,os,csv,math,json,hashlib,sys,bisect,gzip
qa=Path(__file__).resolve().parent;r=qa/'bin/referencias-temporais';r.mkdir(exist_ok=True)
exe=qa/'bin/FaultWaveformTests.exe';spice=qa/'ngspice/usr/bin/ngspice';env=dict(os.environ,WINEPREFIX=str(qa/'wine'),WINEDEBUG='-all',DISPLAY=':94');subprocess.run(['wine',str(exe),'--benchmark-dir','Z:'+str(r).replace('/','\\')],env=env,check=True)
version=subprocess.run([str(spice),'-v'],capture_output=True,text=True,check=True).stdout;results=[]
for name,resistance,reactance,hz,ka,angle in [('rl-50hz',.1,1,50,10,30),('rl-60hz',.03,.6,60,7,85),('l-50hz',0,1,50,10,0)]:
 rows=list(csv.DictReader((r/(name+'.csv')).open(encoding='utf-8-sig'),delimiter=';'));omega=2*math.pi*hz;amplitude=math.sqrt(2)*ka*1000*math.hypot(resistance,reactance);L=reactance/omega
 output=r/(name+'-spice.txt');net=r/(name+'.cir');resistor=f'Rline in mid {resistance}\nLline mid 0 {L} IC=0' if resistance else f'Lline in 0 {L} IC=0';net.write_text(f'''ABB QA independent R-L transient
Bdrive in 0 V={amplitude}*sin({omega}*time+{angle*math.pi/180})
{resistor}
.options reltol=1e-9 abstol=1e-9 vntol=1e-9 method=trap
.control
set wr_singlescale
set wr_vecnames
tran 0.000001 0.1 0 0.000001 uic
wrdata {output} i(Lline)
quit
.endc
.end
''')
 run=subprocess.run([str(spice),'-b',str(net)],capture_output=True,text=True,check=True);(r/(name+'-spice.log')).write_text(run.stdout+run.stderr);data=[list(map(float,line.split())) for line in output.read_text().splitlines()[1:]];ts=[0.]+[row[0] for row in data];amps=[0.]+[row[1] for row in data];errors=[]
 for row in rows:
  t=float(row['Tempo_s']);index=bisect.bisect_left(ts,t)
  if index==0:value=amps[0]
  elif index==len(ts):value=amps[-1]
  else:value=amps[index-1]+(amps[index]-amps[index-1])*(t-ts[index-1])/(ts[index]-ts[index-1])
  expected=float(row['CorrenteInstantanea_kA'])*1000;errors.append(abs(value-expected))
 with gzip.open(str(output)+".gz","wb") as compressed:compressed.write(output.read_bytes())
 output.unlink()
 maximum=max(errors);assert maximum<ka*1000*1e-5,(name,maximum);results.append({'case':name,'samples_CSharp':len(rows),'steps_ngspice':len(data),'max_error_A':maximum,'allowed_A':ka*1000*1e-5})
report={'status':'PASS','version':version.strip(),'ngspice_binary_sha256':hashlib.sha256(spice.read_bytes()).hexdigest(),'cases':results,'method':'Independent ngspice transient, behavioural sinusoidal voltage, explicit R and L, initial inductor current zero, no analytical current expression in simulator'};(r/'validacao-ngspice.json').write_text(json.dumps(report,ensure_ascii=False,indent=2));print(report)
