#!/usr/bin/env python3
"""Bundle Linux source + native app; recreate venv on the destination, never copy a foreign venv."""
import pathlib,zipfile
ROOT=pathlib.Path(__file__).resolve().parents[1]
output=ROOT/'artifacts/KRONOS-Linux.zip';output.parent.mkdir(exist_ok=True)
files=[]
for folder in ('src','tests','scripts','engines','docs','licenses','vendor'):
 files.extend(p for p in (ROOT/folder).rglob('*') if p.is_file() and not any(x in ('bin','obj','__pycache__') for x in p.relative_to(ROOT).parts))
files.extend(ROOT/name for name in ('KRONOS','README.md','LICENSE.md','AUTHORS.md','Directory.Build.props','global.json','Kronos.sln','NuGet.Config','.gitignore'))
files.extend(p for p in (ROOT/'Evidencias/venv/app').rglob('*') if p.is_file())
with zipfile.ZipFile(output,'w',zipfile.ZIP_DEFLATED) as archive:
 for file in files:archive.write(file,'KRONOS/'+file.relative_to(ROOT).as_posix())
print(output)
