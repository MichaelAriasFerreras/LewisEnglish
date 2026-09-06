# Lewis, English Speaking Coach

Sistema de gestión y facturación de estudiantes para un profesor de inglés,
desarrollado en **WPF (.NET 7)**. Incluye la aplicación principal y un
**actualizador (launcher)** que descarga automáticamente las nuevas versiones
desde este repositorio.

## Estructura del repositorio

```
LewisEnglish/            # Aplicación principal (WPF, MVVM, EF Core + SQLite, QuestPDF)
LewisEnglishLauncher/    # Actualizador que consulta update.json y descarga la nueva versión
update.json              # Manifiesto de la última versión publicada
```

## Aplicación principal — `LewisEnglish/`

Sistema para administrar estudiantes de clases de inglés (sesiones de 2 horas,
cada estudiante con su día, hora, tarifa y frecuencia de pago).

### Características

- **Estudiantes**: perfiles completos (cédula, teléfono/WhatsApp, día y hora de
  clase, tarifa, frecuencia y forma de pago).
- **Horario**: clases por día y franja horaria con estado de pago.
- **Asistencia**: registro por clase (Presente / Ausente / Justificado) e historial.
- **Pagos**: periodos automáticos (Semanal / Quincenal / Mensual) con estados
  **Pagado / Pendiente / Vencido**, registro de pagos y recordatorios.
- **Facturas**: comprobantes en **PDF** (marca de agua "PAGADO", logo y colores
  institucionales).
- **Dashboard**: métricas del mes (activos, cobrado, pendiente, mora, ingresos de
  los últimos 6 meses, próximas clases y pagos vencidos).
- **Recordatorios por WhatsApp**: individuales y masivos (`wa.me`).
- **Configuración**: cambio de nombre, correo y contraseña.

### Acceso inicial

- **Correo:** lewis@ingles.com
- **Contraseña:** lewis123

### Tecnologías

- .NET 7 (WPF) — patrón MVVM
- Entity Framework Core + SQLite (base de datos local)
- QuestPDF para la generación de facturas
- Tema oscuro navy (#1A3A52) + turquesa (#00A9A5)

## Actualizador — `LewisEnglishLauncher/`

Aplicación WPF que:

1. Lee la versión instalada localmente (`app/version.txt`).
2. Consulta el manifiesto `update.json` publicado en la rama `main`:
   `https://raw.githubusercontent.com/MichaelAriasFerreras/LewisEnglish/main/update.json`
3. Compara versiones y, si hay una nueva, pide **confirmación** al usuario antes
   de descargar e instalar el ZIP de la actualización.
4. Abre la aplicación (`app/LewisEnglish.exe`).

### Estructura de distribución (lo que recibe el cliente)

```
LewisEnglish/
  LewisEnglishLauncher.exe
  app/
    LewisEnglish.exe
    lewis_logo.png
    version.txt        (versión instalada, ej. "1.0.0")
```

## Publicar una nueva versión

1. Compila y publica la app y el launcher (ver `LewisEnglish/publish.bat`).
2. Empaqueta el contenido de `app/` (`LewisEnglish.exe`, `lewis_logo.png`,
   `version.txt`) en `LewisEnglish-app.zip`.
3. Crea un **Release** en GitHub (ej. tag `v1.0.0`) y sube `LewisEnglish-app.zip`.
4. Actualiza `update.json` en la raíz del repositorio con la nueva `version`,
   la `url` del ZIP del release y las `notes`.

## Requisitos

- Windows 10/11
- [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) (para compilar)

Los ejecutables publicados son **autocontenidos**: no requieren tener .NET
instalado en la máquina del cliente.
