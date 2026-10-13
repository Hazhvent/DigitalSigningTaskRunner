namespace DigitalSigningTaskRunner.Core.Constants.TaskRunner;

public static class SettingsConstants
{
    // ⚙️ Nombres de secciones de configuración (appsettings.json)
    public const string DB_CONNECTION_STRING_NAME = "Database";
    public const string TASK_RUNNER_SETTINGS_SECTION_NAME = "TaskRunnerSettings";
    public const string PRIORITY_WORDS_SETTINGS_SECTION_NAME = "PriorityWordsSettings";
    public const string NAS_SETTINGS_SECTION_NAME = "NASSettings";
    public const string SIGNING_API_SETTINGS_SECTION_NAME = "SigningApiSettings";
    public const string RABBITMQ_SETTINGS_SECTION_NAME = "RabbitMQSettings";

    // ⚙️ Valores por defecto de configuración
    public const int DEFAULT_INTERVAL_IN_SECONDS = 15;
    public const int DEFAULT_BATCH_SIZE = 50;

    // 🖥️ Identificador del Servicio de Windows
    public const string WINDOWS_SERVICE_NAME = "E-policy - Digital Signing";

    // 🟢 Formato y Separadores
    public const string HTTP_CONTENT_TYPE_JSON = "application/json";

    // 💾 Claves de Parámetros de Rutas en Base de Datos (PD_PARAMETROS)
    public const string DB_PARAM_FIRMADOS = "FIRMADOS";
    public const string DB_PARAM_ERROR_NO_FIRMADOS = "ERROR_NO_FIRMADOS";
    public const string DB_PARAM_NAS_RUTA_PRIORIDAD = "NAS_RUTA_PRIORIDAD";

    // 📊 Estados de Bitácora (Oracle)
    public const int BITACORA_ESTATUS_COMPLETADO = 3;
    public const int BITACORA_ESTATUS_ENVIO_FIRMA = 5;

    // 📢 Estados de Eventos (RabbitMQ)
    public const string EVENT_STATUS_SIGNED = "SignedAndProcessed";
    public const string EVENT_STATUS_UNSIGNED = "ProcessedWithoutSignature";

    // ⚙ Configuraciones sobre gestión de archivos
    public const string SMB_ALL_FILES_PATTERN = "*";
    public const string PDF_EXTENSION = ".pdf";
    public const int SMB_READ_CHUNK_SIZE = 1024 * 64;

    // 🛠️ Separadores y Punteros de Directorios Remotos (SMB / Red)
    public const string SMB_DIRECTORY_CURRENT = ".";
    public const string SMB_DIRECTORY_PARENT = "..";
    public const char PATH_SEPARATOR_BACKSLASH = '\\';
    public const char PATH_SEPARATOR_SLASH = '/';
}