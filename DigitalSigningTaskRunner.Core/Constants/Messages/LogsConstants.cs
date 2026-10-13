using DigitalSigningTaskRunner.Core.Constants.TaskRunner;

namespace DigitalSigningTaskRunner.Core.Constants.Messages;

public static class LogsConstants
{
    // ⚙️ PARÁMETROS DE CONFIGURACIÓN DE LOGS (SERILOG)
    public const string LOGGER_FILE_PATH_KEY = $"{SettingsConstants.TASK_RUNNER_SETTINGS_SECTION_NAME}:LogDirectoryPath";
    public const string LOGGER_CATEGORY_MICROSOFT = "Microsoft";
    public const string LOGGER_CATEGORY_MICROSOFT_HOSTING_LIFETIME = "Microsoft.Hosting.Lifetime";
    public const int LOGGER_FILE_RETAINED_COUNT_LIMIT = 30;
    public const string LOGGER_CONSOLE_OUTPUT_TEMPLATE = "[{Timestamp:HH:mm:ss} {Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";
    public const string LOGGER_FILE_OUTPUT_TEMPLATE = "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}";

    // 🟢 INFO LOGS ENRIQUECIDOS (STRUCTURED LOGGING)
    public const string INFO_LOADED_ROUTES = "📁 Rutas configuradas en BD -> Prioridades: '{RutaPrioridades}' | Firmados: '{RutaFirmados}' | Errores: '{RutaErrores}'";
    public const string INFO_DOCUMENTS_FOUND = "🔍 Se encontraron {Count} documentos prioritarios elegidos para procesar (Lote Máximo: {BatchSize}).";
    public const string INFO_DOCUMENT_SIGNED_SUCCESS = "✍️ Documento '{FileName}' firmado digitalmente con éxito.";
    public const string INFO_BITACORA_REGISTERED_SUCCESS = "📋 Bitácora en Oracle registrada con éxito -> Archivo: '{FileName}' | Póliza ID: {PolizaId} | Estatus: {Estatus}.";
    public const string INFO_FILE_MOVED_SUCCESS = "🚚 Archivo '{FileName}' trasladado exitosamente en el NAS a la ruta: '{DestinationPath}'.";
    public const string INFO_RABBITMQ_EVENT_PUBLISHED = "📬 Evento '{EventName}' publicado en RabbitMQ -> Póliza ID: {PolizaId} | Archivo: '{FileName}' | Estado: '{Status}'.";
    public const string INFO_DOCUMENT_PROCESSED_SUCCESS = "✅ Documento '{FileName}' (Póliza ID: {PolizaId}) procesado, actualizado y despachado por completo.";
    public const string INFO_WORKER_STARTING = "🚀 Iniciando DigitalSigningTaskRunner...";
    public const string INFO_AUTHOR = "👤 Autor: Creado y diseñado por Cesar Galvez.";
    public const string INFO_CONTACT = "📧 Para consultas concretas por favor utilice este correo para comunicarse: hazhvent@hotmail.com.";
    public const string INFO_WORKER_STOP_SIGNAL = "🛑 El servicio ha recibido la señal de detención.";
    public const string INFO_WORKER_STOPPED = "💤 DigitalSigningTaskRunner detenido por completo.";

    // 🔴 ERROR LOGS

    // Errores de Negocio / Base de Datos
    public const string ERROR_METADATA_NOT_FOUND = "Imposible recuperar información del archivo '{FileName}' desde la base de datos. Requiere revisión manual.";
    public const string ERROR_MISSING_ROUTE_KEY = "Falta la configuración de la clave de ruta '{Clave}' en la base de datos.";

    // Errores internos
    public const string ERROR_NOT_FOUND_CONFIG_SECTION = "La sección '{SectionName}' no se encontró o no pudo ser mapeada en la configuración.";
    public const string ERROR_FILE_MOVE_FAILED = "No se pudo trasladar el archivo '{FileName}' a su ruta destino final.";
    public const string ERROR_WORKER_CRITICAL = "🔥 Error crítico inesperado en el ciclo principal del TaskRunner: {Message}";
    public const string ERROR_WORKER_INTERNAL_CANCELLED = "⚠️ Se canceló una operación interna en el ciclo principal (posible timeout de BD/Red): {Message}";
    public const string ERROR_DOCUMENT_PROCESSING_FAILED = "🔥 Error no controlado procesando el documento '{FileName}': {Message}";

    // Errores específicos de Firma Digital / API HTTP
    public const string ERROR_SIGNING_API_HTTP = "Error de respuesta en API de Firma Digital (HTTP {StatusCode}): {Detail}";
    public const string ERROR_SIGNING_API_FAILED = "Fallo en la firma digital para el archivo '{FileName}': {Detail}";
    public const string ERROR_SIGNING_API_INVALID_RESPONSE = "Respuesta no válida de la API de firma (Estado: {Estado}).";
    public const string ERROR_SIGNING_API_TIMEOUT_DETAIL = "Timeout agotado o proceso cancelado.";

    // Errores específicos de RabbitMQ / Eventos
    public const string ERROR_RABBITMQ_PUBLISH = "Fallo de infraestructura al publicar el evento en RabbitMQ para el archivo '{FileName}': {Detail}";

    // Errores específicos del Protocolo SMB / Network
    public const string ERROR_SMB_HOST_NOT_FOUND = "Fallo SMB: No se pudo resolver el nombre del host o dirección IP '{HostName}' en la red (DNS / Host desconocido).";
    public const string ERROR_SMB_TIMEOUT = "Fallo SMB: Se agotó el tiempo de espera al comunicarse con el servidor o el puerto SMB (445) no está alcanzable.";
    public const string ERROR_SMB_AUTH_CREDENTIALS_INVALID = "Fallo SMB: El usuario o contraseña especificados en el archivo de configuración son incorrectos.";
    public const string ERROR_SMB_AUTH_READ_DENIED = "Fallo SMB: Acceso denegado. El usuario de red no tiene permisos de LECTURA para inspeccionar o acceder a esta ruta.";
    public const string ERROR_SMB_AUTH_WRITE_DENIED = "Fallo SMB: Acceso denegado. El usuario de red puede leer la ruta, pero carece de permisos de ESCRITURA/CREACIÓN para crear o modificar directorios en el destino.";
    public const string ERROR_SMB_FILE_LOCKED = "Fallo SMB: El archivo está bloqueado o en uso por otro proceso (Sharing Violation).";
    public const string ERROR_SMB_GENERIC = "Fallo SMB: El servidor rechazó la operación (Código: {StatusCode}).";

    // Errores de datos e infraestructura
    public const string ERROR_PATH_NOT_EXISTS = "Fallo SMB: La ruta o directorio '{Path}' especificado no existe en el servidor.";
    public const string ERROR_INVALID_DATA_FORMAT = "La estructura interna de los datos no tiene un formato válido o presenta errores de sintaxis.";

    // 📝 ARGUMENTOS Y VALORES QUEMADOS DE LOGS
    public const string ARG_DB_ROUTES_NAME = "Rutas BD TaskRunner";
    public const string ARG_NAS_STRUCTURE_NAME = "Estructura de directorios NAS";
    public const string ARG_SIGNING_API_FAILURE = "Fallo en endpoint HTTPS/Pegasus";
}