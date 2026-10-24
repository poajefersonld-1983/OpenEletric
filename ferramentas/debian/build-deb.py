"""Build the Debian amd64 compatibility package. Requires a tested PE32+ application,
unchanged Wine Mono runtime, and the exact corresponding upstream source archive.
No Wine prefix or vendor ABB binaries are installed by this recipe.
"""
from pathlib import Path
import argparse,shutil,os,subprocess,hashlib,json,zipfile,gzip
parser=argparse.ArgumentParser();parser.add_argument('--binary',required=True);parser.add_argument('--mono',required=True);parser.add_argument('--mono-source',required=True);parser.add_argument('--mono-copying',required=True);parser.add_argument('--work',required=True);parser.add_argument('--output',required=True);args=parser.parse_args()
r=Path(__file__).resolve().parents[2];recipes=Path(__file__).parent;work=Path(args.work).resolve();work.mkdir(parents=True,exist_ok=True);root=work/'root'
if root.exists():shutil.rmtree(root)
app=root/'opt/openeletric';doc=root/'usr/share/doc/openeletric';app.mkdir(parents=True);doc.mkdir(parents=True);control=root/'DEBIAN';control.mkdir()
version='0.2.0-1';binary=Path(args.binary).resolve();data=binary.read_bytes();pe=int.from_bytes(data[60:64],'little');assert int.from_bytes(data[pe+4:pe+6],'little')==0x8664,'Build /platform:x64 first'
source=Path(args.mono_source).resolve();assert hashlib.sha256(source.read_bytes()).hexdigest()=='128e335396689e466dff5e1aa0770a714dbe3d16397fc4ddbb8a80999c67323b','Unexpected upstream source archive'
shutil.copy2(binary,app/'OpenEletric.exe');(app/'VERSION').write_text(version+'\n');shutil.copytree(Path(args.mono).resolve(),app/'runtime/mono',symlinks=True)
# Include the complete matching upstream source, with submodules, for runtime licenses.
shutil.copy2(source,doc/source.name);shutil.copy2(args.mono_copying,doc/'Wine-Mono-COPYING')
for relative,name in [('usr/bin','openeletric'),('usr/share/applications','openeletric.desktop'),('usr/share/icons/hicolor/scalable/apps','openeletric.svg'),('usr/share/mime/packages','openeletric.xml')]:
 target=root/relative;target.mkdir(parents=True,exist_ok=True);shutil.copy2(recipes/name,target/name)
examples=app/'exemplos';examples.mkdir()
for p in (r/'examples/industriais').glob('*.circuito'):shutil.copy2(p,examples/p.name)
assert len(list(examples.glob('*.circuito')))==10
readme='''OpenEletric para Debian13 — amd64

Instalação: sudo apt install ./openeletric_0.2.0-1_amd64.deb
Depois abra OpenEletric no menu de aplicativos, ou execute openeletric.
O aplicativo pode abrir diretamente um arquivo .circuito pelo gerenciador de arquivos.

Esta edição usa Wine64 do Debian e o runtime Wine Mono10.4.1 incluído no pacote.
Não é uma reescrita nativa GTK/Qt. Arquitetura amd64, sem dependência obrigatória de Wine32.
A primeira abertura prepara um ambiente privado do usuário, sem baixar o runtime.

Projetos:Documentos/OpenEletric. Os10exemplos industriais são copiados para
Documentos/OpenEletric/Exemplos na primeira abertura, sem substituir arquivos existentes.
Dados internos:$XDG_DATA_HOME/openeletric/wine64 (padrão ~/.local/share).
Registro:$XDG_STATE_HOME/openeletric/ultima-execucao.log (padrão ~/.local/state).
A instalação e remoção não apagam esses dados pessoais.

Remoção:sudo apt remove openeletric. Não execute o aplicativo com sudo.
Os limites de cálculo/AMB da versão validada são preservados. Os exemplos são sintéticos;
o forno é um teste negativo de sobrecarga, não uma instalação aprovada.

Fontes da aplicação:app-source.zip neste diretório. Código completo correspondente ao
runtime:wine-mono-10.4.1-src.tar.xz neste diretório. Wine-Mono-COPYING explica as licenças
dos componentes, também presentes no arquivo de fontes. Fonte oficial:
https://github.com/wine-mono/wine-mono/releases/tag/wine-mono-10.4.1
Pacote Wine do sistema é fornecido e mantido pelo Debian, não por este aplicativo.
'''
(doc/'README.Debian').write_text(readme)
(doc/'copyright').write_text('''Format: https://www.debian.org/doc/packaging-manuals/copyright-format/1.0/
Upstream-Name: OpenEletric (local ABB Novo project)
Source: app-source.zip included in this package

Files: opt/openeletric/runtime/mono/*
Copyright: Wine Mono and its respective component authors
License: Various (MIT, LGPL, GPL-with-exception, BSD, zlib, MS-PL)
 Full upstream license notices and the exact complete corresponding sources are
 supplied in Wine-Mono-COPYING and wine-mono-10.4.1-src.tar.xz in this directory.

Files: opt/openeletric/OpenEletric.exe
Comment: Application source and bundled-library license notices are included
 in app-source.zip. No ABB DOC3/JPCAD executable or prototype is distributed.
''')
with zipfile.ZipFile(doc/'app-source.zip','w',zipfile.ZIP_DEFLATED) as z:
 for p in (r/'app').rglob('*'):
  if not p.is_file() or 'verificacao' in p.relative_to(r/'app').parts or p.suffix=='.exe':continue
  z.write(p,p.relative_to(r))
 for name in ['LICENSE','THIRD_PARTY_NOTICES.md']:
  z.write(r/name,name)
 for p in recipes.iterdir():
  if p.is_file():z.write(p,p.relative_to(r))
 for name in ['build-linux.py','validar-industriais.py','validar-fluxo.py','validar-saidas-industriais.py']:
  p=r/'ferramentas'/name;z.write(p,p.relative_to(r))
 for name in ['MATRIZ-FUNCOES.csv','VALIDACAO-FINAL.md','INTERCAMBIO-DXF-AMB.md']:
  p=r/'analise'/name;z.write(p,p.relative_to(r))
post='''#!/bin/sh
set -e
root=${DPKG_ROOT:-}
if command -v update-desktop-database >/dev/null 2>&1; then update-desktop-database "$root/usr/share/applications" || :; fi
if command -v update-mime-database >/dev/null 2>&1; then update-mime-database "$root/usr/share/mime" || :; fi
exit 0
'''
for name in ['postinst','postrm']:(control/name).write_text(post)
installed=sum(p.stat().st_size for p in root.rglob('*') if p.is_file())//1024
(control/'control').write_text(f'''Package: openeletric
Version: {version}
Section: science
Priority: optional
Architecture: amd64
Maintainer: poajefersonld-1983 <318690780+poajefersonld-1983@users.noreply.github.com>
Installed-Size: {installed}
Depends: wine64 (>= 10.0~), fonts-dejavu-core, fonts-droid-fallback, zenity, util-linux, xdg-user-dirs, desktop-file-utils, shared-mime-info
Description: Electrical circuit and board studies (Debian desktop edition)
 OpenEletric for Debian13 amd64, using Wine64 and an included Wine Mono
 runtime. Provides the validated electrical-study application, desktop/file
 integration and ten synthetic industrial example projects.
''')
md5=[]
for p in root.rglob('*'):
 if p.is_symlink():continue
 if p.is_dir():p.chmod(0o755)
 elif p.is_file():
  executable=bool(p.stat().st_mode&0o111) or p.name in ['postinst','postrm'] or p==root/'usr/bin/openeletric';p.chmod(0o755 if executable else 0o644)
  if control not in p.parents:md5.append(hashlib.md5(p.read_bytes()).hexdigest()+'  '+str(p.relative_to(root)))
(control/'md5sums').write_text('\n'.join(sorted(md5))+'\n')
subprocess.run(['desktop-file-validate',str(root/'usr/share/applications/openeletric.desktop')],check=True);subprocess.run(['sh','-n',str(root/'usr/bin/openeletric')],check=True)
output=Path(args.output).resolve();output.parent.mkdir(parents=True,exist_ok=True);subprocess.run(['dpkg-deb','--root-owner-group','--threads-max=2','-Zxz','-z1','--build',str(root),str(output)],check=True)
print(json.dumps(dict(output=str(output),bytes=output.stat().st_size,sha256=hashlib.sha256(output.read_bytes()).hexdigest(),installed_kib=installed),indent=2))
