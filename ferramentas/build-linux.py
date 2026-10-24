from pathlib import Path
import subprocess,os,json,hashlib,sys,shutil
root=Path(__file__).resolve().parent
src=Path(os.environ.get('CIRCUITO_QA_SRC',str(root.parent/'app')))
out=Path(os.environ.get('CIRCUITO_QA_OUT',str(root.parent/'app/verificacao')));out.mkdir(parents=True,exist_ok=True)
for fixture in (src/'amostras-testes').glob('referencias-*'):
 if fixture.is_dir():shutil.copytree(fixture,out/fixture.name,dirs_exist_ok=True)
def win(p): return 'Z:'+str(p).replace('/','\\')
args=['/nologo','/langversion:5','/target:winexe' if sys.argv[1]=='CircuitoStudio.Program' else '/target:exe','/optimize+','/main:'+sys.argv[1],'/out:'+win(out/(sys.argv[1]+'.exe'))]
platform=os.environ.get('CIRCUITO_QA_PLATFORM','anycpu')
if platform not in ('anycpu','x64','x86'):raise SystemExit('Invalid CIRCUITO_QA_PLATFORM')
args+=['/platform:'+platform]
args+=['/r:'+x+'.dll' for x in ['System.Drawing','System.Windows.Forms','System.Runtime.Serialization','System.Core','System.Xml.Linq','System.IO.Compression','System.IO.Compression.FileSystem','System.Data','System.Numerics']]
for name in ['ExcelDataReader.dll','System.ValueTuple.dll']:
 p=src/'third_party/ExcelDataReader'/name
 args+=['/r:'+win(p),'/resource:'+win(p)+',CircuitoStudio.'+name]
for file,name in [('LICENSE.txt','Legal.ExcelDataReader'),('System.ValueTuple.LICENSE.txt','Legal.ValueTuple'),('System.ValueTuple.NOTICES.txt','Legal.ValueTuple.Notices')]:
 args+=['/resource:'+win(src/'third_party/ExcelDataReader'/file)+','+name]
pdf=src/'third_party/PDFsharp'
for pkg in json.loads((pdf/'manifest.json').read_text(encoding='utf-8-sig')):
 for asm in pkg['Assemblies']:
  p=pdf/asm['File'];assert hashlib.sha256(p.read_bytes()).hexdigest().upper()==asm['Hash']
  alias='PdfEngine=' if p.name=='PdfSharp-gdi.dll' else 'PdfSupport=' if p.name.startswith('PdfSharp') else None
  if alias:args+=['/r:'+alias+win(p)]
  args+=['/resource:'+win(p)+',CircuitoStudio.'+p.name]
args+=['/resource:'+win(pdf/'LICENSES.txt')+',Legal.PDFsharp']
mono=Path(os.environ.get('CIRCUITO_WINE_MONO','/usr/share/wine/mono/wine-mono-10.4.1'))/'lib/mono/4.5'
args=[a for a in args if a not in ['/r:System.IO.Compression.dll','/r:System.IO.Compression.FileSystem.dll']]
args+=['/r:'+win(mono/'Facades/netstandard.dll'),'/r:WindowsBase.dll','/r:'+win(mono.parent/'4.8-api/System.IO.Compression.dll'),'/r:'+win(mono.parent/'4.8-api/System.IO.Compression.FileSystem.dll'),'/r:'+win(next((mono.parent/'gac/PresentationCore').glob('*/*.dll')))]
api=mono.parent/'4.8-api'
args=['/nostdlib']+[('/r:'+win(api/a[3:])) if a.startswith('/r:') and '=' not in a and '\\' not in a and (api/a[3:]).exists() else a for a in args]
args+=['/r:'+win(api/'mscorlib.dll'),'/r:'+win(api/'System.dll')]
args += [win(p) for p in sorted(src.glob('*.cs')) if not p.stem.endswith('Tests') or p.stem==sys.argv[1]]
rsp=out/(sys.argv[1]+'.rsp');rsp.write_text('\n'.join('"'+a+'"' for a in args))
env=dict(os.environ,CIRCUITO_QA_VISUAL=os.environ.get('CIRCUITO_QA_VISUAL','0'),DISPLAY=os.environ.get('DISPLAY',':94'),WINEDEBUG='-all',WINEPREFIX=os.environ.get('CIRCUITO_WINEPREFIX',str(root.parent/'work/qa-wine')))
if env['CIRCUITO_QA_VISUAL']=='1' and subprocess.run(['xset','q'],env=env,stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL).returncode:
 print('Tela virtual :94 indisponível. Recrie Xvfb antes de aceitar evidência visual.',file=sys.stderr);sys.exit(3)
r=subprocess.run(['wine',str(mono/'csc.exe'),'@'+win(rsp)],env=env)
if r.returncode:sys.exit(r.returncode)
if sys.argv[1]=='CircuitoStudio.Program':sys.exit(0)
r=subprocess.run(['wine',str(out/(sys.argv[1]+'.exe'))],env=env)
sys.exit(r.returncode)
