#!/usr/bin/env python3
"""KRONOS Linux bootstrap. Private venv, pinned tools, no sudo/global pip. Copyright 2026 JLC; VSL 1.0."""
import hashlib,json,os,pathlib,platform,shutil,subprocess,sys,tarfile,tempfile,urllib.request,venv,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
EVIDENCE=ROOT/'Evidencias'
ENV=EVIDENCE/'venv'
TOOLS={
 'floss':('3.2.0','https://github.com/mandiant/flare-floss/releases/download/v3.2.0/floss-v3.2.0-linux.zip','c449366960b688769b6510662cd6db6a3bf9edada4e8a8878056becd324df52c'),
 'aleapp':('2026.4.5','https://github.com/abrignoni/ALEAPP/releases/download/v2026.4.5/ALEAPP-2026.4.5-linux-x64.AppImage','ae724c4c92f916e0912b635ee55e66d4fc0c534ea9774fadc6f5518124b8e283'),
 'ileapp':('2026.4.6','https://github.com/abrignoni/iLEAPP/releases/download/v2026.4.6/iLEAPP-2026.4.6-linux-x64.AppImage','3d1b03beac5be669beb7bb06d2a4410bd5632aa610931cbb911c77cd5d24cfbe')}

def digest(path,algorithm='sha256'):
 with path.open('rb') as f:return hashlib.file_digest(f,algorithm).hexdigest()

def source_hash():
 value=hashlib.sha256()
 files=[ROOT/'Directory.Build.props',ROOT/'global.json',ROOT/'scripts/setup-linux.py',ROOT/'KRONOS']
 for folder in ('src','engines'):
  files.extend(p for p in (ROOT/folder).rglob('*') if p.is_file() and not any(x in ('bin','obj','__pycache__') for x in p.relative_to(ROOT).parts))
 for path in sorted(files):
  value.update(path.relative_to(ROOT).as_posix().encode());value.update(path.read_bytes())
 return value.hexdigest()

def fetch(url,expected,name,algorithm='sha256'):
 target=ENV/'downloads'/name;target.parent.mkdir(parents=True,exist_ok=True)
 if not target.exists():
  print('Descargando '+name,flush=True)
  partial=target.with_suffix(target.suffix+'.part')
  subprocess.run(['curl','--fail','-L','--retry','3','--connect-timeout','20',url,'-o',str(partial)],check=True)
  partial.replace(target)
 if digest(target,algorithm).lower()!=expected.lower():raise RuntimeError('Hash incorrecto: '+name)
 return target

def run(args,**kwargs):subprocess.run(list(map(str,args)),check=True,env=process_env(),**kwargs)
def process_env():
 env=dict(os.environ)
 env.update(DOTNET_CLI_HOME=str(ENV/'home/dotnet'),NUGET_PACKAGES=str(ENV/'nuget'),DOTNET_CLI_TELEMETRY_OPTOUT='1',AVALONIA_TELEMETRY_OPTOUT='1',PIP_CACHE_DIR=str(ENV/'cache/pip'),TMPDIR=str(ENV/'tmp'))
 return env

def sdk():
 executable=ENV/'sdk/dotnet'
 if executable.exists():return executable
 if shutil.which('dotnet'):
  output=subprocess.check_output(['dotnet','--list-sdks'],text=True)
  if any(line.startswith('10.0.4') for line in output.splitlines()):return pathlib.Path(shutil.which('dotnet'))
 metadata=json.load(urllib.request.urlopen('https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json',timeout=60))
 selected=None
 for release in metadata['releases']:
  for entry in [release.get('sdk',{}),*release.get('sdks',[])]:
   if entry.get('version')=='10.0.404':selected=next((f for f in entry['files'] if f.get('rid')=='linux-x64' and f['name'].endswith('.tar.gz')),None)
 if selected is None:raise RuntimeError('No se encontró el SDK Linux fijado 10.0.404.')
 package=fetch(selected['url'],selected['hash'],'dotnet-sdk-10.0.404-linux-x64.tar.gz','sha512')
 destination=ENV/'sdk';destination.mkdir(exist_ok=True)
 with tarfile.open(package) as archive:archive.extractall(destination,filter='data')
 return executable

def install_tools():
 directory=ENV/'tools';directory.mkdir(exist_ok=True);receipts=[]
 for name,(version,url,sha) in TOOLS.items():
  # Exact official asset names are recorded in releases.lock.json after discovery.
  package=fetch(url,sha,url.rsplit('/',1)[1])
  destination=directory/name
  if not destination.exists():
   if name=='floss':
    destination.mkdir()
    with zipfile.ZipFile(package) as archive:
     member=next(n for n in archive.namelist() if pathlib.PurePosixPath(n).name=='floss')
     (destination/'floss').write_bytes(archive.read(member))
   else:
    package.chmod(0o755)
    unpack=ENV/'tmp'/('unpack-'+name);unpack.mkdir(exist_ok=True)
    run([package,'--appimage-extract'],cwd=unpack,stdout=subprocess.DEVNULL)
    (unpack/'squashfs-root').rename(destination)
  executable=destination/('floss' if name=='floss' else 'AppRun')
  executable.chmod(executable.stat().st_mode|0o111)
  receipts.append(dict(id=name,version=version,url=url,archiveSha256=sha,executable=executable.relative_to(directory).as_posix(),sha256=digest(executable)))
 foremost=directory/'foremost/foremost';foremost.parent.mkdir(exist_ok=True)
 if not foremost.exists():
  if shutil.which('foremost'):shutil.copy2(shutil.which('foremost'),foremost)
  elif shutil.which('cc'):
   run(['cc','-std=gnu99','-O2','-fcommon','-D__UNIX','-D__LINUX','-D_FILE_OFFSET_BITS=64','-DVERSION="1.5.7-kronos1"','-o',foremost,*sorted((ROOT/'vendor/foremost').glob('*.c'))])
  else:raise RuntimeError('Falta foremost o un compilador C en Kali.')
 receipts.append(dict(id='foremost',version=subprocess.check_output([str(foremost),'-V'],stderr=subprocess.STDOUT,text=True).splitlines()[0],executable='foremost/foremost',sha256=digest(foremost)))
 (directory/'tools.json').write_text(json.dumps(receipts,indent=2),encoding='utf-8')

def main():
 if sys.platform!='linux' or platform.machine() not in ('x86_64','amd64'):raise SystemExit('Esta publicación requiere Linux x86_64.')
 if '--check' in sys.argv:
  try:ready=json.loads((ENV/'ready.json').read_text())
  except (OSError,ValueError):raise SystemExit(1)
  raise SystemExit(0 if ready.get('source_sha256')==source_hash() and (ENV/'app/Kronos.Desktop').is_file() and (ENV/'bin/python').is_file() and (ENV/'tools/tools.json').is_file() else 1)
 EVIDENCE.mkdir(exist_ok=True)
 if not (ENV/'bin/python').exists():venv.EnvBuilder(with_pip=True).create(ENV)
 for name in ('tmp','cache','home','Trabajo'): (ENV/name).mkdir(parents=True,exist_ok=True)
 python=ENV/'bin/python'
 run([python,'-m','pip','install','--require-hashes','--only-binary=:all:','-r',ROOT/'engines/memory/requirements.lock'])
 install_tools()
 if '--dependencies-only' in sys.argv:sdk();return
 output=ENV/'app'
 try:prebuilt=json.loads((output/'prebuilt.json').read_text())
 except (OSError,ValueError):prebuilt={}
 if prebuilt.get('source_sha256')!=source_hash() or not (output/'Kronos.Desktop.dll').is_file() or digest(output/'Kronos.Desktop.dll')!=prebuilt.get('application_sha256'):
  dotnet=sdk()
  run([dotnet,'publish',ROOT/'src/Kronos.Desktop','-c','Release','-r','linux-x64','--self-contained','true','-o',output,'-p:PublishReadyToRun=false','-p:NuGetLockFilePath=packages.linux-x64.lock.json'])
 for name in ('engines','licenses','docs'):
  shutil.copytree(ROOT/name,output/name,dirs_exist_ok=True)
 for name in ('README.md','LICENSE.md','AUTHORS.md'):shutil.copy2(ROOT/name,output/name)
 executable=output/'Kronos.Desktop';executable.chmod(executable.stat().st_mode|0o111)
 receipt=dict(platform='linux-x64',sdk='10.0.404',source_sha256=source_hash(),application_sha256=digest(output/'Kronos.Desktop.dll'))
 (ENV/'ready.json').write_text(json.dumps(receipt),encoding='utf-8')
 (output/'prebuilt.json').write_text(json.dumps(receipt),encoding='utf-8')
 launcher=ROOT/'KRONOS';launcher.chmod(0o755)
 desktop=ROOT/'KRONOS.desktop'
 desktop.write_text('[Desktop Entry]\nType=Application\nName=KRONOS Laboratorio Forense\nComment=Preparación verificada y análisis forense\nExec="'+str(launcher)+'"\nPath='+str(ROOT)+'\nTerminal=false\nCategories=Utility;\n',encoding='utf-8')
 desktop.chmod(0o755)
 desktop_dir=pathlib.Path.home()/'Desktop'
 if shutil.which('xdg-user-dir'):
  result=subprocess.run(['xdg-user-dir','DESKTOP'],capture_output=True,text=True)
  if result.returncode==0 and result.stdout.strip():desktop_dir=pathlib.Path(result.stdout.strip())
 if desktop_dir.is_dir():
  shortcut=desktop_dir/'KRONOS.desktop'
  if not shortcut.exists() or str(ROOT) in shortcut.read_text(errors='replace'):
   shutil.copy2(desktop,shortcut);shortcut.chmod(0o755)
   if shutil.which('gio'):subprocess.run(['gio','set',str(shortcut),'metadata::trusted','true'],stdout=subprocess.DEVNULL,stderr=subprocess.DEVNULL)
 print('KRONOS Linux preparado. Copias de entrada: '+str(EVIDENCE)+'; casos: '+str(ENV/'Trabajo'),flush=True)

if __name__=='__main__':main()
