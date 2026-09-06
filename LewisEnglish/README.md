# Lewis, English Speaking Coach

Aplicacion de escritorio (Windows / WPF, .NET 7) para la gestion de estudiantes,\
horarios, asistencia, pagos y facturacion de un profesor de ingles.\
Todas las clases tienen una duracion de **2 horas** y cada estudiante puede tener\
su propio dia, hora, tarifa y frecuencia de pago.

## Caracteristicas

* **Estudiantes**: perfiles completos (nombre, cedula, telefono/WhatsApp, direccion,\
  email, dia y hora de clase, tarifa, frecuencia y forma de pago).

* **Horario**: vista de las clases por dia y franja horaria, con el estado de pago\
  de cada estudiante.

* **Asistencia**: registro de asistencia por clase (Presente, Ausente, Justificado)\
  con historial por estudiante.

* **Pagos**: generacion automatica de periodos (Semanal, Quincenal, Mensual) con\
  estados **Pagado / Pendiente / Vencido**, registro de pagos y recordatorios.

* **Facturas**: generacion de comprobantes en **PDF** (con marca de agua "PAGADO",\
  logo y colores institucionales) que se pueden abrir o guardar.

* **Dashboard**: metricas del mes (estudiantes activos, cobrado, pendiente, mora,\
  ingresos de los ultimos 6 meses, proximas clases y pagos vencidos).

* **Recordatorios por WhatsApp**: individuales y masivos mediante enlaces `wa.me`.

* **Configuracion**: cambio de nombre, correo y contrasena de acceso.

## Tecnologias

* .NET 7 (WPF) — patron MVVM

* Entity Framework Core + SQLite (base de datos local)

* QuestPDF para la generacion de facturas

* Tema oscuro con identidad visual navy (#1A3A52) + turquesa (#00A9A5)

## Requisitos

* Windows 10/11

* [.NET 7 SDK](https://dotnet.microsoft.com/download/dotnet/7.0) (para compilar)

## Compilar y ejecutar

```bat
dotnet restore
dotnet build
dotnet run
```

## Generar ejecutable (publish)

Ejecuta el script incluido (crea un ejecutable autocontenido en `publish/`):

```bat
publish.bat
```

El ejecutable resultante `LewisEnglish.exe` no requiere tener .NET instalado.

## Acceso inicial

* **Correo:** [lewis@ingles.com](mailto:lewis@ingles.com)

* **Contrasena:** lewis123

Se recomienda cambiar estas credenciales desde la seccion **Configuracion**\
la primera vez que se inicie la aplicacion.

## Datos

La base de datos SQLite (`lewis.db`) y las copias de las facturas se guardan en la\
carpeta del usuario (`Documentos\LewisEnglish`), por lo que la informacion se\
conserva entre actualizaciones de la aplicacion.