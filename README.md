# KRONOS V2.0 · Laboratorio Forense Linux

**Kit de Recolección, Organización y Normalización de Observaciones de Seguridad.**

**Developed By JLC 2026 UlysesX12.** Proyecto de José Luis Cabañas. Código propio bajo [Volatility Software License 1.0](LICENSE.md). [Autoría](AUTHORS.md) y [licencias de terceros](docs/THIRD_PARTY_NOTICES.md).

Aplicación gráfica nativa para **Kali Linux x86_64**, pensada para trabajar dentro de una VM: selecciona una copia o respaldo, prepara una copia exacta de trabajo, comprueba SHA-256, identifica su estructura y propone el análisis compatible. Presenta los resultados por categorías en español y permite ver las fotos dentro de KRONOS.

## Empezar

Abre **KRONOS** en el escritorio de Kali o el lanzador `KRONOS` del proyecto. La primera ejecución prepara el entorno privado y descarga las dependencias oficiales verificadas; las siguientes lo reutilizan. No requiere recordar comandos de los motores. Para instalar desde código en una máquina nueva, ejecuta una vez `sh ./KRONOS` si el gestor de archivos no permite iniciar scripts. Requiere las dependencias Linux indicadas en la [guía de instalación](docs/LINUX.md).

1. Crea un caso con su nombre y el del analista.
2. En **Preparar copia**, pulsa **Seleccionar copia o respaldo**.
3. Espera la copia y las verificaciones; revisa la identificación y pulsa **Analizar a fondo**.
4. Revisa categorías y **Ver fotos**; añade los hallazgos a tus notas con su fuente.
5. Completa la interpretación y exporta el informe HTML/Markdown.

## Organización

```text
Evidencias/
├── copia_recibida.tar             Archivo aportado, sin modificar
└── venv/
    ├── bin/, lib/, tools/, app/   Entorno y herramientas
    └── Trabajo/
        └── KRONOS_Caso_…/
            ├── evidence/         Copia exacta de trabajo
            ├── .work/            Temporales y cachés
            ├── analysis/         Resultados y registros
            ├── reports/          Informes
            └── case.kronos.json
```

**No borres `venv`: contiene los casos.** La preparación conserva su contenido y no usa `--clear`. Las evidencias y entornos se excluyen de Git. La VM aporta aislamiento del anfitrión; el entorno Python aísla dependencias, no el acceso al sistema de archivos.

## Integraciones

- **Volatility 3 2.28.2:** RAM Windows compatible, cuatro o catorce módulos.
- **ALEAPP 2026.4.5 / iLEAPP 2026.4.6:** extracciones Android/iOS ZIP, TAR o TAR.GZ.
- **FLOSS 3.2.0:** cadenas de ejecutables compatibles.
- **Foremost:** recuperación por firmas de imágenes y archivos.
- **The Sleuth Kit, si está disponible en Kali:** listado recursivo de sistemas de archivos compatibles; sin montar la imagen.
- Adaptadores propios: identificación estructural, historial de navegadores y búsquedas GitHub, inventarios ZIP/TAR, cadenas con offsets y particiones RAW.
- Galería de fotos verificada, categorías de hallazgos y notas con referencias.

Los datos de identidad, SIM, números, MAC, mensajes, correos, llamadas y ubicaciones aparecen cuando están presentes y un módulo compatible los recupera. Los campos comunes se traducen; se conservan los artefactos y referencias originales.

## Integridad y límites

La importación compara SHA-256 del origen antes/después y del destino. Los análisis comprueban la copia antes/después; se guardan versiones, hashes, registros y manifiestos de salida. Las tablas y fotos se verifican antes de mostrarlas. El caso no está firmado ni sellado de forma independiente.

La identificación puede ser no concluyente. No se promete recuperar todo, descifrar cualquier archivo ni determinar automáticamente malicia, autoría o motivos. La clasificación orienta la revisión. Los errores, ausencia de archivos y límites de cobertura quedan registrados. Esta versión no integra adquisición física, desbloqueo de móviles, reconstrucción RAID ni la interfaz completa de Autopsy.

Consulta [Linux y flujo guiado](docs/LINUX.md), [cobertura de los motores](docs/LABORATORIO.md), [RAM](docs/MEMORIA.md) y [validación](docs/VALIDACION.md).

## Desarrollo

Código de interfaz y núcleo en .NET/Avalonia; motores y adaptadores Python en entorno virtual. `scripts/setup-linux.py` prepara y publica Linux sin instalaciones Python globales. Las pruebas usan datos sintéticos y comprueban hashes, casos, motores, identificación, categorías y visualización de imágenes. Se mantiene compatibilidad de compilación con Windows para regresiones; el flujo principal y la publicación recomendada son Linux.
