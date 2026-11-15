# KRONOS Laboratorio para Kali Linux

KRONOS es una aplicación gráfica nativa Linux x86_64. Conserva la interfaz clara de escritorio; no utiliza Wine. Su runtime .NET es privado y los motores Python se ejecutan mediante un entorno virtual. La VM proporciona el aislamiento respecto al anfitrión; `venv` aísla dependencias y no es un bloqueo del sistema de archivos.

## Abrir y organizar

Tras la preparación inicial, abre el acceso **KRONOS** del escritorio de Kali o `KRONOS.desktop` dentro del proyecto. El lanzador `KRONOS` comprueba si la aplicación necesita reconstruirse y prepara las dependencias privadas cuando faltan. La primera preparación requiere conexión, `python3-venv`, `curl`, bibliotecas gráficas de Avalonia y Foremost o un compilador C. No ejecuta `sudo`, ni instala paquetes Python globales ni usa `--break-system-packages`. Si falta una dependencia del sistema, se registra el error: no se finge una instalación completa.

```text
KRONOS/
├── KRONOS                   Lanzador
├── KRONOS.desktop           Acceso gráfico generado
├── src/, engines/, scripts/ Código de la aplicación
└── Evidencias/              Coloca aquí las copias recibidas
    ├── copia_recibida.tar
    └── venv/
        ├── bin/, lib/       Entorno Python
        ├── tools/           FLOSS, ALEAPP, iLEAPP y Foremost
        ├── sdk/, app/       Compilación y aplicación Linux
        └── Trabajo/
            └── KRONOS_Caso_…/
                ├── case.kronos.json
                ├── evidence/ Copia exacta de trabajo
                ├── .work/    Temporales y cachés del caso
                ├── analysis/ Resultados, registros y manifiestos
                └── reports/  Informes editables
```

La copia recibida queda en la raíz de **Evidencias**. La aplicación crea una copia de trabajo dentro del caso y compara SHA-256 del origen antes/después y del destino. No crea una tercera copia completa de conservación además de esos dos archivos. El instalador nunca vacía `venv`: **contiene los casos y no debe borrarse o recrearse con `--clear`**. Conserva una copia de seguridad de `Evidencias/venv/Trabajo`.

## Flujo guiado

1. En **Inicio**, introduce nombre del caso y del analista y pulsa **Crear caso**. El destino se configura automáticamente en `Evidencias/venv/Trabajo`.
2. En **Preparar copia**, pulsa **Seleccionar copia o respaldo**. El selector comienza en `Evidencias`, aunque también puedes seleccionar un archivo de otra ubicación.
3. KRONOS copia, compara hashes e identifica cabeceras y estructura. Examina hasta 20000 nombres de entradas en ZIP/TAR; en TAR comprimido limita además la inspección lógica inicial. Las rutas Android/iOS permiten proponer ALEAPP/iLEAPP. Una extensión `.mem` no demuestra RAM: los formatos no concluyentes se muestran como tales.
4. Revisa la propuesta y pulsa **Analizar a fondo**. Se ejecuta el perfil compatible sin escribir comandos. Windows RAM con cabecera reconocida usa Volatility; discos RAW usan inventario/TSK cuando está disponible y Foremost; archivos móviles usan LEAPP; imágenes, ejecutables, historial y contenedores tienen sus propios perfiles. Los formatos sin perfil no se fuerzan a otro analizador.
5. En **Archivos y móviles**, usa las categorías en español: **Identidad y cuentas, Mensajes, Llamadas, Correos, Ubicaciones, Contactos, Conectividad, Navegación, Aplicaciones, Archivos y Otros hallazgos**. La clasificación se basa en nombres y campos de artefactos: orienta la revisión, no prueba atribución ni intención.
6. Pulsa **Ver fotos** para una galería paginada de miniaturas dentro de KRONOS. Selecciona una para ampliarla y añadirla a las notas con su hash y fuente. Se agrupan duplicados por SHA-256 en la vista; los archivos y sus ubicaciones originales se conservan en las salidas.
7. Añade las filas relevantes a notas, completa tu interpretación y genera el informe. La plantilla pericial completa y la generación automática de conclusiones siguen pendientes; las notas y los informes HTML/Markdown actuales se conservan.

## Qué se muestra y qué no se garantiza

Los campos de identidad, SIM, números, MAC, cuentas y modelo se presentan cuando los plugins recuperan esos artefactos. No se supone que un número pertenezca al titular ni que una coordenada acredite presencia física. Los datos ausentes, módulos sin archivos, incompatibilidades y errores son situaciones diferentes. El estado **REVISAR COBERTURA** exige revisar las incidencias.

Las etiquetas frecuentes se traducen y se mantiene el artefacto y la fuente originales; no todos los campos específicos de cientos de aplicaciones están traducidos. El límite de normalización es 100000 filas, con vista previa de 10000 por categoría. Las salidas originales completas se conservan. El truncamiento queda registrado.

La galería muestra JPG/PNG/GIF/BMP/WebP compatibles desde las salidas recuperadas o móviles. También reconoce por cabecera hasta 10000 archivos sin extensión de imagen, para detectar cachés. No incorpora un visor de vídeo, HEIC, imágenes incrustadas en cualquier formato ni todas las imágenes posibles del dispositivo. Los archivos superiores a 64 MiB, 100 megapíxeles o 50000 píxeles por lado se conservan sin vista previa. Se comprueba el hash y se limitan dimensiones antes de solicitar la decodificación nativa. El índice se reutiliza mientras el manifiesto permanezca intacto.

The Sleuth Kit (`fls`) permite listar recursivamente sistemas de archivos compatibles sin montar el disco. Se guardan versión, hash, listado y errores. No se integra la interfaz completa de Autopsy. EBR, RAID/LVM, volúmenes cifrados y formatos de imagen sin perfil requieren trabajo adicional. No se adquiere RAM, discos físicos ni teléfonos desde este flujo.

## Dependencias y actualización

`scripts/setup-linux.py` fija Volatility 2.28.2, FLOSS 3.2.0, ALEAPP 2026.4.5, iLEAPP 2026.4.6 y SDK .NET 10.0.404. Comprueba hashes de descargas oficiales y extrae las AppImages sin requerir FUSE. Foremost se copia de la instalación de Kali o se compila desde las fuentes incluidas; su versión y hash se registran. Las herramientas disponibles de The Sleuth Kit se usan desde Kali y se documentan en cada ejecución.

El lanzador compara el hash del código con la preparación guardada. Actualizar el código puede reconstruir la aplicación, reutilizando las descargas verificadas y conservando los casos. Las pruebas usan exclusivamente archivos sintéticos. El TAR del usuario se examina desde la aplicación cuando el analista decida iniciar su caso.

Fuentes: [entornos Python](https://docs.python.org/3/library/venv.html), [paquetes Python en Kali](https://www.kali.org/docs/general-use/python3-external-packages/), [Avalonia Linux](https://docs.avaloniaui.net/docs/deployment/linux) y [fls de The Sleuth Kit](https://www.sleuthkit.org/sleuthkit/man/fls.html).
