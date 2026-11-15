"""Bounded non-extracting identification. Copyright 2026 JLC. VSL 1.0."""
import json,pathlib,sqlite3,sys,tarfile,zipfile
from contextlib import closing

def identify(path):
    path=pathlib.Path(path)
    with path.open('rb') as f: header=f.read(16384)
    def result(kind,description,confidence,tools):return dict(Kind=kind,Description=description,Confidence=confidence,SuggestedTools=tools)
    if header.startswith(b'SQLite format 3\x00'):
        try:
            with closing(sqlite3.connect(path.resolve().as_uri()+'?mode=ro&immutable=1',uri=True)) as db:tables={r[0] for r in db.execute("SELECT name FROM sqlite_master WHERE type='table'")}
            if any(pair<=tables for pair in ({'urls','visits'},{'moz_places','moz_historyvisits'},{'history_items','history_visits'})):return result('NAVEGADOR','SQLite con estructura de historial reconocida.','ESTRUCTURA',['browser'])
        except sqlite3.Error:pass
        return result('SQLITE','Base SQLite sin un perfil de historial reconocido.','FIRMA',['strings'])
    if header.startswith((b'PAGEDUMP',b'PAGEDU64')):return result('RAM_WINDOWS','Cabecera de volcado Windows; Volatility debe validar compatibilidad.','FIRMA',['memory'])
    if header.startswith(b'MDMP'):return result('MINIDUMP','Minidump; no equivale a RAM completa compatible con Volatility.','FIRMA',['strings'])
    if header.startswith((b'\x89PNG\r\n\x1a\n',b'\xff\xd8\xff',b'GIF87a',b'GIF89a')):return result('IMAGEN','Cabecera de imagen reconocida.','FIRMA',['media'])
    if header.startswith(b'MZ'):return result('EJECUTABLE','Cabecera DOS/PE; FLOSS comprobará compatibilidad.','FIRMA',['floss'])
    if header[512:520]==b'EFI PART' or header[4096:4104]==b'EFI PART' or len(header)>512 and header[510:512]==b'\x55\xaa':return result('DISCO_RAW','Firma de arranque o particiones; requiere validación estructural.','FIRMA',['disk','foremost'])
    if header.startswith((b'EVF',b'LVF',b'vhdxfile',b'QFI\xfb',b'KDMV')):return result('CONTENEDOR_DISCO','Contenedor de disco: requiere motor compatible; no se trata como RAW.','FIRMA',[])
    names=[];container=None
    try:
        if header.startswith((b'PK\x03\x04',b'PK\x05\x06')):
            container='ZIP'
            with zipfile.ZipFile(path) as archive:names=[n.filename.lower().replace('\\','/') for n in archive.infolist()[:20000]]
        elif header[257:262]==b'ustar' or header.startswith(b'\x1f\x8b') or path.suffix.lower() in ('.tar','.tgz','.gz'):
            container='TAR'
            with tarfile.open(path,'r:*') as archive:
                for index,member in enumerate(archive):
                    names.append(member.name.lower().replace('\\','/'))
                    if header.startswith(b'\x1f\x8b') and member.offset_data+member.size>256*1024*1024:break
                    if index>=19999:break
    except (tarfile.TarError,zipfile.BadZipFile,OSError):return result('ARCHIVO_DAÑADO','No se pudo validar la estructura del archivo.','NO CONCLUYENTE',[])
    if container:
        android=any('/data/data/' in '/'+n or '/data/user/' in '/'+n or 'build.prop' in n or '/com.android.' in n for n in names)
        ios=any('private/var/mobile/' in n or 'mobile/library/safari/' in n or 'systemversion.plist' in n for n in names)
        if android and not ios:return result('ANDROID',f'{container} con rutas de extracción Android; inspección de hasta 20000 entradas.','ESTRUCTURA',['aleapp'])
        if ios and not android:return result('IOS',f'{container} con rutas de extracción iOS; inspección de hasta 20000 entradas.','ESTRUCTURA',['ileapp'])
        return result('ARCHIVO',f'{container} reconocido; plataforma no concluyente.','CONTENEDOR',['archive'])
    return result('SIN_IDENTIFICAR','Identificación no concluyente. La extensión no demuestra RAM o disco.','NO CONCLUYENTE',['strings'])

if __name__=='__main__':pathlib.Path(sys.argv[2]).write_text(json.dumps(identify(sys.argv[1]),ensure_ascii=False,indent=2),encoding='utf-8')
