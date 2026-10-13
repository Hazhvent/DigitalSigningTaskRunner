# 🚀 DigitalSigningTaskRunner - Flujo de Póliza Electrónica

Servicio *Background Worker* de alto rendimiento para el procesamiento masivo, priorización, firma digital criptográfica (HSM) y despacho asíncrono de pólizas y certificados digitales desarrollado sobre **.NET 10**.

Este componente pertenece integralmente al **Flujo de Póliza Electrónica**, trabajando en sincronía con **FileProcessor API**. Esta refactorización consolida y moderniza la fase crítica de firmado: reemplaza tanto al servicio legacy de Windows (`PD.WindowsService` / `PD_WebService`) como al aplicativo intermedio desarrollado en **Java**, asumiendo la responsabilidad completa de ambos en un único componente asíncrono gobernado por un **sistema de colas y eventos sobre RabbitMQ**, cuyo destinatario final es **EmailDispatchingConsumer** (Worker Background Service en .NET 10 que consume la cola y traslada a la carpeta `Notificados`).

---

## 🔄 1. Flujo de Póliza Electrónica (Fase de Firma y Despacho)

Dentro del ciclo de vida de la emisión, **FileProcessor API** procesa y deposita los documentos validados en el repositorio compartido (NAS). A partir de ese punto, **DigitalSigningTaskRunner** toma el control del pipeline de forma autónoma:

1. **Monitoreo y Filtrado NAS (SMB)**: Descubrimiento de archivos PDF en la carpeta compartida (`Prioridades`), ordenados dinámicamente en memoria según palabras clave (`RPKit`, `VIP`, `URGENTE`).
2. **Extracción de Reglas y Metadatos**: Consulta a Oracle DB mediante Dapper para verificar si el documento requiere firma digital (`FirmaDigital != 0`) y extraer las credenciales del titular.
3. **Firma Criptográfica HSM Directa**: En lugar de esperar a un ejecutable externo, el servicio absorbe el rol del aplicativo Java legado, serializando el documento en memoria (Base64) y consumiendo de forma directa la API externa de **Pegasus (SignnetSignature)**.
4. **Auditoría Transaccional**: Inserción inmediata del estado en la bitácora de Oracle DB (`SP_SINAPROBITACORAALTA`).
5. **Traslado Físico (I/O)**: Movimiento atómico del PDF procesado hacia la carpeta `FIRMADOS` (o `ERROR_NO_FIRMADOS` ante incidentes).
6. **Despacho Asíncrono por Colas (RabbitMQ)**: Emisión del evento `DocumentProcessedEvent` hacia el exchange `e-policy.exchange` para ser recibido por **EmailDispatchingConsumer** (Worker Background Service en .NET 10 que consume la cola y traslada a la carpeta `Notificados`).

```text
[ FileProcessor API ]
        │  (Depósito inicial en NAS)
        ▼
 📁 Carpeta Prioridades ──► [ DigitalSigningTaskRunner (.NET 10) ]
    (Almacén SMB/NAS)                      │
                                           ├─► ⚙️ Oracle DB (Validación Metadatos)
                                           │
                                           ├─► 🔏 Motor Pegasus / HSM (HTTPS Directo)
                                           │   (Reemplaza aplicativo Java y .BATs)
                                           │
                                           ├─► 📝 Oracle DB (Bitácora de Auditoría)
                                           │
                                           ├─► 🚚 Traslado NAS ──► 📁 Carpeta Firmados
                                           │
                                           ▼
                                 🐇 RabbitMQ Message Broker
                              (e-policy.exchange / e-policy.signed)
                                           │
                                           ▼
                              [ EmailDispatchingConsumer ]
                         (Worker Background Service en .NET 10)
                                           │
                                           ▼
                             📁 Carpeta 'Notificados' (NAS)

```

### ⚠️ Problemas del Ecosistema Legado (`PD.WindowsService` + Aplicativo Java)

El ecosistema anterior sufría de una arquitectura fragmentada con tres puntos únicos de falla críticos:

* **Doble Salto y Espera Ciega (Servicio Windows + Java)**: El servicio Windows (`GeneradorCSV.cs`) generaba archivos CSV en disco (`c:/tmpCSV`), disparaba consolas `cmd.exe` para correr archivos `.bat`, y esperaba a que un **aplicativo externo en Java** leyera los archivos, interactuara con el HSM y los firmara.
* **Congelamiento Destructivo (`Thread.Sleep`)**: Para "coordinar" la espera con el programa en Java, el código calculaba el peso del archivo y ejecutaba bloqueos ciegos de hilos de 1 minuto (`Sleep(60000)`), 2 minutos (`Sleep(120000)`) o hasta 4 minutos (`Sleep(240000)`), paralizando todo el servicio.
* **Riesgo Crítico de Conexión COM (`mpr.dll`)**: El acceso a red montaba unidades compartidas mediante invocaciones no administradas a Windows (`WNetAddConnection2`) protegidas por `Mutex` globales, generando interbloqueos (*deadlocks*) permanentes ante caídas transitorias de red.
* **Validación Frágil por POP3**: Para confirmar el despacho de correos, el sistema intentaba conectarse vía POP3 a cuentas de correo (`SBMReportsCore`) para descargar adjuntos ZIP y parsear reportes manualmente.

### 📊 Comparativa de Arquitectura

| Dimensión | Ecosistema Legado (.NET Framework + Java) | Nueva Arquitectura TaskRunner (.NET 10) |
| --- | --- | --- |
| **Orquestación de Firma** | Híbrido frágil: .NET genera CSV → .bat → Aplicativo Java → HSM | **Consolidado**: `DigitalSigningTaskRunner` consume Pegasus HSM directamente vía HTTPS. |
| **Manejo de Tiempos** | `Thread.Sleep(240000)` (Bloqueos de 1 a 4 min) | 100% Asíncrono no bloqueante (`async/await`). |
| **Despacho y Notificación** | Polling POP3 de reportes acoplado en el timer | **Event-Driven**: Cola RabbitMQ hacia `EmailDispatchingConsumer` (.NET 10). |
| **Destino de Notificados** | Movimiento condicional rígido dentro del monolito | Delegado hacia la carpeta `Notificados` por el consumidor. |
| **Acceso a NAS** | Mapeo Windows con `mpr.dll` + Mutex global | Cliente de red nativo `SMBLibrary` (SMB1/2/3). |
| **Patrón Arquitectónico** | Clase Dios procedural (`GeneradorCSV.cs`, >1600 líneas) | **Clean Architecture** + **CQRS** + **MediatR**. |
| **Persistencia Oracle** | ADO.NET manual con código duplicado | **Dapper** + Mapeo automatizado de `RefCursor`. |
| **Trazabilidad** | Escritura saturada en el `EventLog` del SO | **Serilog**: Logging estructurado a consola y archivo rotativo. |

---

## 🏛️ 2. Arquitectura del Sistema (Clean Architecture)

La solución desacopla la lógica de negocio de los mecanismos de entrega, almacenamiento y comunicación:

```text
DigitalSigningTaskRunner.sln
│
├── 📂 DigitalSigningTaskRunner (Worker / Host de Servicio Windows)
│   ├── HostedService.cs                # Ciclo de vida BackgroundService & Control de Scopes
│   ├── Program.cs                      # Host Builder, Configuración de Windows Service y DI
│   └── appsettings.json                # Configuración central (Oracle, NAS, HSM, RabbitMQ)
│
├── 📂 DigitalSigningTaskRunner.Application (Capa de Aplicación)
│   ├── Actions/                        # Sub-Handlers CQRS (ExecuteDigitalSignature, LoadPriorityDocuments)
│   ├── Configuration/                  # Métodos de extensión para DI (TaskRunner, Logging)
│   └── UseCases/                       # Orquestador del flujo (ProcessPriorityDocsHandler)
│
├── 📂 DigitalSigningTaskRunner.Core (Capa de Dominio y Abstracciones)
│   ├── Constants/                      # Constantes de Rutas, BD, Colas y Logs estructurados
│   ├── Dto/ & Models/                  # DTOs, Entidades Oracle y Eventos (DocumentProcessedEvent)
│   └── Interfaces/                     # Contratos (IRepository, IDigitalSignature, IEventPublisher, etc.)
│
└── 📂 DigitalSigningTaskRunner.Infrastructure (Capa de Infraestructura)
    ├── Persistence/                    # Repositorio Dapper, DDL y extensión OracleParameters
    └── Services/                       # SMBLibrary, RabbitMQ Channel, DigitalSignatureClient

```

---

## 🧩 3. Patrones de Diseño, Orquestación y Sistema de Colas

### 🔀 1. CQRS y Orquestación Atómica de Dominio

El caso de uso principal (`ProcessPriorityDocsHandler`) actúa como un **Orquestador de Dominio**. Siguiendo el principio de Responsabilidad Única (SRP), delega submódulos de trabajo a través del `ISender` de MediatR ejecutando acciones atómicas (*Macro Actions*):

```text
ProcessPriorityDocsCommand (Mediator)
 │
 ├──► GetTaskRunnerPathsDataAsync      (Obtención de rutas activas desde Oracle)
 ├──► EnsureNetworkDirectoriesExist    (Verificación y armado del árbol de carpetas SMB)
 ├──► LoadPriorityDocumentsAction      (Descubrimiento, ordenamiento por prioridad y carga selectiva)
 └──► Iteración por documento:
       ├──► ExecuteDigitalSignatureAction  (Invocación HTTPS a Pegasus HSM - Reemplazo de Java)
       ├──► SetSinAprobacionBitacoraAsync  (Registro transaccional de auditoría en Oracle)
       ├──► MoveNetworkFile                (Traslado físico en la NAS a FIRMADOS)
       └──► EventPublisherService          (Publicación del evento en la cola de RabbitMQ)

```

### ⚡ 2. Absorción del Rol de Java y Firma Directa en Memoria

Para eliminar el aplicativo Java intermedio y los archivos de intercambio (`.csv` / `.bat`), el sub-handler `ExecuteDigitalSignatureActionHandler` se comunica directamente con la API de Pegasus vía HTTP:

* Serializa los bytes del PDF cifrado en Base64.
* Inyecta la contraseña dinámica del PDF (`metadata.Dni`) y las credenciales HSM (`UsuarioHSM`, `PinHSM`, `SerieHSM`).
* Recibe los bytes firmados y los procesa en memoria RAM sin dejar rastros en el disco local (`c:\tmpCSV`).

### 🐇 3. Desacoplamiento Event-Driven con RabbitMQ

Se sustituyó el procesamiento monolítico sincrónico por una arquitectura desacoplada orientada a eventos:

* **Exchange**: `e-policy.exchange` (Tipo `Direct`, durable).
* **Cola**: `e-policy.signed.queue` (Durable).
* **Routing Key**: `e-policy.signed`.
* **Integración con EmailDispatchingConsumer**: Cada archivo procesado emite un `DocumentProcessedEvent` persistente (`Persistent = true`). El consumidor **EmailDispatchingConsumer** (Worker Background Service en .NET 10 que consume la cola y traslada a la carpeta `Notificados`) se encarga de procesar las notificaciones de forma totalmente aislada.

### 🔝 4. Priorización Dinámica In-Memory y Batching

Para evitar sobrecargar la red y la memoria RAM, `LoadPriorityDocumentsActionHandler` inspecciona inicialmente **solo los nombres de archivo**. Aplica el ordenamiento por palabras clave configuradas (`PriorityWordsSettings`: `RPKit`, `VIP`, `URGENTE`) y segmenta según el `BatchSize`. Únicamente se descargan los bytes de los archivos que entran en la cuota del lote actual.

### 🔄 5. Aislamiento Scoped en BackgroundService

Para garantizar la liberación de memoria y conexiones a base de datos en un servicio de ejecución continua, el `HostedService` crea explícitamente un `IServiceScope` en cada iteración:

```csharp
while (!stoppingToken.IsCancellationRequested)
{
    using (IServiceScope scope = serviceScopeFactory.CreateScope())
    {
        ISender sender = scope.ServiceProvider.GetRequiredService<ISender>();
        await sender.Send(new ProcessPriorityDocsCommand(), stoppingToken);
    }
    await Task.Delay(TimeSpan.FromSeconds(intervalSeconds), stoppingToken);
}

```

---

## 🛠️ 4. Stack Tecnológico

| Categoría | Tecnología / Librería | Versión | Propósito |
| --- | --- | --- | --- |
| ⚡ **Runtime** | **.NET 10.0** | `10.0` | Framework base (`net10.0`) con C# 14, `ImplicitUsings` y `Nullable`. |
| 🖥️ **Host / Service** | **Microsoft.Extensions.Hosting.WindowsServices** | `10.0.12` | Integración nativa del Worker para ejecutarse como Servicio de Windows. |
| 🔀 **Arquitectura** | **MediatR** | `12.5.0` | Implementación del patrón Mediator, CQRS y pipeline de acciones. |
| 🛡️ **Validación** | **FluentValidation** | `12.1.1` | Validación fuertemente tipada de modelos y comandos. |
| 🗄️ **Base de Datos** | **Oracle.ManagedDataAccess.Core** | `23.26.301` | Driver oficial gestionado para conexión eficiente con Oracle DB. |
| 🚀 **ORM** | **Dapper** | `2.1.89` | Micro-ORM de alto rendimiento para mapeo automático de `RefCursor`. |
| 📁 **Almacenamiento Red** | **SMBLibrary** | `1.5.8.1` | Cliente nativo SMB1/SMB2/SMB3 para integración directa con servidores NAS. |
| 🐇 **Mensajería** | **RabbitMQ.Client** | `7.2.2` | Broker asíncrono para topología de eventos (`Exchange`, `Queue`, `Bindings`). |
| 📝 **Logging** | **Serilog.AspNetCore** | `10.0.0` | Logging estructurado hacia consola y archivos físicos rotativos. |
| 🔄 **JSON Engine** | **Microsoft.AspNetCore.Mvc.NewtonsoftJson** | `10.0.12` | Formateo estricto de payloads para la integración con la API de firma Pegasus. |

---

## ⚙️ 5. Desglose Detallado de Macro Actions (`Actions/` & `UseCases/`)

Los componentes CQRS modulares encargados de resolver tareas específicas de negocio son:

* 🚀 **`ProcessPriorityDocsHandler`**: Orquestador central del caso de uso. Lee rutas de BD, garantiza directorios NAS, coordina el firmado masivo, audita en Oracle, traslada archivos en red y despacha eventos a RabbitMQ.
* 📥 **`LoadPriorityDocumentsActionHandler`**: Sub-handler responsable del descubrimiento liviano en SMB, ordenamiento por palabras de prioridad (`PriorityWords`), aplicación del límite de lote (`BatchSize`) y lectura binaria en memoria.
* 🔏 **`ExecuteDigitalSignatureActionHandler`**: Sub-handler que reemplaza el componente legacy en Java. Empaqueta el archivo cifrado y credenciales HSM y consume directamente la API de Pegasus (`SignnetSignature`).

---

## 🔧 6. Configuración del Sistema (`appsettings.json`)

```json
{
  "ConnectionStrings": {
    "Database": "Data Source=<DB_HOST>:<DB_PORT>/<SERVICE_NAME>;User Id=<DB_USER>;Password=<DB_PASSWORD>;"
  },
  "TaskRunnerSettings": {
    "IntervalInSeconds": 15,
    "BatchSize": 50,
    "LogDirectoryPath": "C:\\Logs\\DigitalSigningTaskRunner\\taskrunner-.log"
  },
  "PriorityWordsSettings": {
    "Keys": [ "RPKit", "VIP", "URGENTE" ]
  },
  "NASSettings": {
    "Host": "Prtsrvnasapp",
    "Domain": "PROTECTA",
    "Username": "<NAS_USER>",
    "Password": "<NAS_PASSWORD>",
    "ShareFolder": "PRD"
  },
  "SigningApiSettings": {
    "Endpoint": "https://firmadigital.pegasus.com.pe/SignnetSignature/intranet/pegasus/firma-bytes",
    "UsuarioHSM": "<HSM_USER>",
    "PinHSM": "<HSM_PIN>",
    "SerieHSM": "<HSM_SERIE>",
    "Fuente": "3"
  },
  "RabbitMQSettings": {
    "Host": "<RABBITMQ_HOST>",
    "Port": 5672,
    "VirtualHost": "/",
    "Username": "DigitalSigner",
    "Password": "<RABBITMQ_PASS>",
    "ExchangeName": "e-policy.exchange",
    "RoutingKey": "e-policy.signed",
    "QueueName": "e-policy.signed.queue"
  }
}

```

---

## ⚙️ 7. Pipeline y Extensibilidad (`Program.cs`)

El punto de entrada `Program.cs` delega la configuración del Worker a métodos de extensión fuertemente tipados:

```csharp
var builder = Host.CreateApplicationBuilder(args);

// Configuración nativa del Servicio de Windows
builder.Services.AddWindowsService(options =>
{
    options.ServiceName = SettingsConstants.WINDOWS_SERVICE_NAME;
});

// Extensiones de builder (Servicios & DI)
builder.AddLogs();         // Configuración de Serilog (Consola + Archivo rotativo)
builder.AddTaskRunner();   // Registro de SMBLibrary, HttpClient tipado, RabbitMQ y MediatR
builder.AddDataBase();     // Registra OracleConnection (IDbConnection Scoped) y Dapper Repositories

// HostedService principal del ciclo de vida
builder.Services.AddHostedService<HostedService>();

var host = builder.Build();
await host.RunAsync();

```

---

## 🗄️ 8. Configuración de Base de Datos y Persistencia

La persistencia utiliza **Dapper** sobre conexiones administradas a Oracle DB. La conexión se registra con vida útil `Scoped` para garantizar que se libere automáticamente al finalizar cada lote:

```csharp
public static TBuilder AddDataBase<TBuilder>(this TBuilder builder)
    where TBuilder : IHostApplicationBuilder
{
    string? connectionString = builder.Configuration.GetConnectionString(SettingsConstants.DB_CONNECTION_STRING_NAME);

    builder.Services.AddScoped<IDbConnection>(sp =>
    {
        return new OracleConnection(connectionString);
    });

    builder.Services.AddScoped<IRepository, Repository>();
    return builder;
}

```

---

## 📦 9. Instalación y Despliegue como Servicio de Windows

Para compilar y registrar el worker como un Servicio de Windows en producción:

1. **Compilar y Publicar la Solución**:

```bash
dotnet publish DigitalSigningTaskRunner/DigitalSigningTaskRunner.csproj -c Release -o C:\Services\DigitalSigningTaskRunner

```

2. **Crear el Servicio de Windows con `sc.exe**`:

```cmd
sc.exe create "E-policy - Digital Signing" binPath= "C:\Services\DigitalSigningTaskRunner\DigitalSigningTaskRunner.exe" start= auto

```

3. **Iniciar el Servicio**:

```cmd
net start "E-policy - Digital Signing"

```

---

## 🚦 10. Guía de Ejecución Local

1. **Prerrequisitos**:

* Tener instalado **.NET 10 SDK**.
* Conexión de red/VPN con acceso a Oracle DB, servidor NAS (puerto SMB 445) y RabbitMQ.

2. **Restaurar dependencias NuGet**:

```bash
dotnet restore

```

3. **Compilar la solución**:

```bash
dotnet build

```

4. **Ejecutar el servicio localmente**:

```bash
dotnet run --project DigitalSigningTaskRunner/DigitalSigningTaskRunner.csproj

```
