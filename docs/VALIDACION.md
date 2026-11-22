# Validación de la versión actual

KRONOS incluye gestión de casos, copia y verificación de evidencias, análisis RAM Windows con Volatility 3, tabla de resultados con búsqueda, notas con referencias de fuente e informes HTML/Markdown.

La versión principal es ahora Linux/Kali. Se ha compilado y ejecutado de forma nativa en la VM de Kali del usuario, con el runtime y herramientas dentro de `Evidencias/venv`. Las pruebas de integración ejecutan los motores Linux reales sobre datos sintéticos: historial Android/iOS, PE para FLOSS, carving Foremost, identificación y galería. La galería se prueba con una miniatura decodificada por Skia dentro de KRONOS, no mediante un visor externo. Los resultados por categoría conservan las referencias a las salidas originales. El TAR del usuario no se usa en estas pruebas.

## Pruebas automatizadas

- Núcleo: SHA-256 conocido, origen intacto, copia verificada, persistencia, apertura concurrente, nombres con espacios/tildes, exportación, alteraciones, ausencias, archivos ajenos, rutas fuera del caso y cancelación.
- RAM: perfiles de cuatro y catorce módulos mediante datos sintéticos, referencias JSON Pointer, límite de vista previa, salidas vacías, resultados parciales, rechazo de evidencia alterada, detección de JSON alterado, cancelación, historial e informes.
- Motor real: Volatility instalado y runtime Python empaquetado rechazan un archivo de ceros sintéticos como imagen compatible; no se transforma ese error en resultados válidos.
- Interfaz headless: tema claro, inicio guiado y siete páginas con navegación lateral, recuperación tras un error, tabla RAM y tabla de archivos con filas verificadas de un caso sintético guardado y altura de lectura comprobada. El modo `--screenshots` renderiza capturas con Skia, usando solo datos sintéticos.
- Laboratorio: pruebas Python de adaptadores, incluidos offsets, cruce de bloques, SQLite sin modificar, ZIP con rutas peligrosas, GPT 4K/CRC, MBR/NTFS, identificación estructural, categorías y copia de imágenes.
- Motores portátiles reales: Foremost recupera candidatos de una imagen construida; FLOSS encuentra una cadena conocida de un PE compilado para la prueba; ALEAPP interpreta historial Chromium Android e iLEAPP historial Safari iOS sintéticos. Orquestación, hashes, manifiestos, evidencias derivadas y manipulación de resultados se prueban desde el núcleo .NET. Un proceso Python real comprueba que temporales, perfil y caché quedan dentro del caso y que no se altera el entorno temporal del anfitrión. Las extracciones móviles se generan directamente dentro del caso, sin transferencia desde el temporal de Windows.
- Módulos de artefactos: base Chromium SQLite sintética, generación de resultados, fuente intacta y detección de alteración.
- Sincronización Git: repositorios temporales, historial inicial, conservación de archivos, rechazo de diferencias y protección de commits existentes.

Estas pruebas no extraen información de volcados del usuario ni realizan adquisiciones del anfitrión. La prueba funcional de su volcado se realiza desde la interfaz.

## Entorno y ejecución

Desarrollo previsto en Dev Container, con SDK y dependencias fijados. GitHub Actions configura pruebas en Windows, Ubuntu y Docker. Los resultados efectivos de cada revisión se consultan en [Actions](https://github.com/JLC-DevLab/KRONOS-V2.0/actions); configurar una prueba no significa que ya haya pasado.

La publicación Windows x64 es autocontenida e incorpora Python embebido y el motor RAM. Los avisos de licencia se conservan. Los hashes de dependencias Python están fijados en `engines/memory/requirements.lock`; el archivo del runtime embebido se comprueba mediante SHA-256 durante el empaquetado.

## Limitaciones

El análisis RAM se limita a los módulos Windows integrados y depende de los símbolos disponibles y del soporte real de Volatility. No se garantiza recuperar todo ni atribuir autoría por coincidencias. El rendimiento no mejora automáticamente por integrar el motor en una interfaz.

La ventana Windows, los selectores del sistema y un volcado compatible requieren prueba manual. Las pruebas headless no certifican toda la interacción gráfica ni el acceso a hardware. Las pruebas móviles y de particiones usan datos construidos, no certifican cobertura de todos los teléfonos o sistemas de archivos. No se ha certificado adquisición RAM, discos físicos, desbloqueo de móviles, reconstrucción RAID, exploración completa de discos ni análisis RAM Linux/macOS. Consulta la cobertura exacta en [Laboratorio](LABORATORIO.md).

El historial del caso no está firmado. Una interrupción abrupta puede dejar un análisis en curso o un archivo de evidencia no registrado; conserva el caso completo y verifica su integridad antes de emitir conclusiones.
