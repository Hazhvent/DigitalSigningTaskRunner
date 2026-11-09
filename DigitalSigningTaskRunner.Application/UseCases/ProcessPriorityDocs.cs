using DigitalSigningTaskRunner.Application.Actions;
using DigitalSigningTaskRunner.Core.Constants.Messages;
using DigitalSigningTaskRunner.Core.Constants.TaskRunner;
using DigitalSigningTaskRunner.Core.Dto;
using DigitalSigningTaskRunner.Core.Interfaces.Repositories;
using DigitalSigningTaskRunner.Core.Interfaces.Services;
using DigitalSigningTaskRunner.Core.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace DigitalSigningTaskRunner.Application.UseCases;

public sealed record ProcessPriorityDocsCommand : IRequest<Unit>;

public class ProcessPriorityDocsHandler(
    IRepository repository,
    ILoggingService<ProcessPriorityDocsHandler> logger,
    IEventPublisherService eventPublisher,
    INetworkConnectionService networkConnection,
    IStorageService storageService,
    ISender sender,
    IOptions<TaskRunnerSettings> taskRunnerOptions)
    : IRequestHandler<ProcessPriorityDocsCommand, Unit>
{
    private static bool _isFirstRun = true;
    private readonly TaskRunnerSettings _settings = taskRunnerOptions.Value;

    public async Task<Unit> Handle(ProcessPriorityDocsCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener parámetros de rutas desde la base de datos
        IEnumerable<Parametros> routeParams = await repository.GetTaskRunnerPathsDataAsync();
        var routeList = routeParams.ToList();

        if (routeList.Count == 0)
        {
            logger.LogError(null, LogsConstants.ERROR_NOT_FOUND_CONFIG_SECTION, LogsConstants.ARG_DB_ROUTES_NAME);
            return Unit.Value;
        }

        // 2. Extraer rutas relativas configuradas
        string rutaPrioridades = routeList
            .FirstOrDefault(p => string.Equals(p.Clave?.Trim(), SettingsConstants.DB_PARAM_NAS_RUTA_PRIORIDAD, StringComparison.OrdinalIgnoreCase))
            ?.Parametro?.Trim() ?? string.Empty;

        string rutaFirmados = routeList
            .FirstOrDefault(p => string.Equals(p.Clave?.Trim(), SettingsConstants.DB_PARAM_FIRMADOS, StringComparison.OrdinalIgnoreCase))
            ?.Parametro?.Trim() ?? string.Empty;

        string rutaErrores = routeList
            .FirstOrDefault(p => string.Equals(p.Clave?.Trim(), SettingsConstants.DB_PARAM_ERROR_NO_FIRMADOS, StringComparison.OrdinalIgnoreCase))
            ?.Parametro?.Trim() ?? string.Empty;

        if (_isFirstRun)
        {
            logger.LogInfo(LogsConstants.INFO_LOADED_ROUTES, rutaPrioridades, rutaFirmados, rutaErrores);
            _isFirstRun = false;
        }

        if (string.IsNullOrWhiteSpace(rutaPrioridades))
        {
            logger.LogError(null, LogsConstants.ERROR_MISSING_ROUTE_KEY, SettingsConstants.DB_PARAM_NAS_RUTA_PRIORIDAD);
            return Unit.Value;
        }

        // 3. Garantizar la estructura de directorios en el NAS
        bool directoriesPrepared = false;
        await networkConnection.RunInNetworkScope(fileStore =>
        {
            directoriesPrepared = storageService.EnsureNetworkDirectoriesExist(fileStore, routeList);
        });

        if (!directoriesPrepared)
        {
            logger.LogError(null, LogsConstants.ERROR_PATH_NOT_EXISTS, LogsConstants.ARG_NAS_STRUCTURE_NAME);
            return Unit.Value;
        }

        int batchSize = _settings.BatchSize > 0
            ? _settings.BatchSize
            : SettingsConstants.DEFAULT_BATCH_SIZE;

        // 4. Cargar y ordenar documentos priorizados
        LoadPriorityDocumentsActionResponse loadResult = await sender.Send(
            new LoadPriorityDocumentsActionRequest(rutaPrioridades, batchSize), cancellationToken);

        if (!loadResult.DirectoryExists)
        {
            logger.LogError(null, LogsConstants.ERROR_PATH_NOT_EXISTS, rutaPrioridades);
            return Unit.Value;
        }

        // Manejo de archivos sin metadatos: Mover a Errores para evitar bucle infinito de logs
        if (loadResult.MetadataNotFoundFileNames.Count > 0)
        {
            await networkConnection.RunInNetworkScope(fileStore =>
            {
                foreach (string missingFile in loadResult.MetadataNotFoundFileNames)
                {
                    logger.LogError(null, LogsConstants.ERROR_METADATA_NOT_FOUND, missingFile);
                    string sourcePath = Path.Combine(rutaPrioridades, missingFile);
                    string errorPath = Path.Combine(rutaErrores, missingFile);

                    byte[]? missingBytes = storageService.ReadNetworkFileBytes(fileStore, sourcePath);
                    if (missingBytes != null && missingBytes.Length > 0)
                    {
                        storageService.MoveNetworkFile(fileStore, sourcePath, errorPath, missingBytes);
                    }
                }
            });
        }

        if (loadResult.Documents.Count == 0)
        {
            return Unit.Value;
        }

        logger.LogInfo(LogsConstants.INFO_DOCUMENTS_FOUND, loadResult.Documents.Count, batchSize);

        // 5. Procesamiento cíclico de documentos
        foreach (PriorityDocumentItemDto doc in loadResult.Documents)
        {
            try
            {
                InformacionPoliza metadata = doc.Metadata;
                byte[] activePdfBytes = doc.FileBytes;
                int estatusFinal;

                // Documento requiere firma digital
                if (metadata.FirmaDigital != 0)
                {
                    byte[]? signedPdfBytes = await sender.Send(new ExecuteDigitalSignatureActionRequest(
                        EncryptedPdfBytes: doc.FileBytes,
                        FileName: doc.FileName,
                        Password: metadata.Dni ?? string.Empty
                    ), cancellationToken);

                    if (signedPdfBytes == null)
                    {
                        logger.LogError(null, LogsConstants.ERROR_SIGNING_API_FAILED, doc.FileName, LogsConstants.ARG_SIGNING_API_FAILURE);

                        string errorDestinationPath = Path.Combine(rutaErrores, doc.FileName);
                        await networkConnection.RunInNetworkScope(fileStore =>
                        {
                            storageService.MoveNetworkFile(fileStore, doc.RawFilePath, errorDestinationPath, doc.FileBytes);
                        });

                        continue;
                    }

                    activePdfBytes = signedPdfBytes;
                    estatusFinal = SettingsConstants.BITACORA_ESTATUS_ENVIO_FIRMA;

                    logger.LogInfo(LogsConstants.INFO_DOCUMENT_SIGNED_SUCCESS, doc.FileName);
                }
                else
                {
                    estatusFinal = SettingsConstants.BITACORA_ESTATUS_COMPLETADO;
                }

                // 🟢 PASO 6: Registro de Bitácora en Oracle PRIMERO
                await repository.SetSinAprobacionBitacoraAsync(
                    idPoliza: metadata.IdPoliza,
                    ruta: rutaFirmados,
                    estatus: estatusFinal,
                    nombreArchivo: doc.FileName,
                    idGenerico: metadata.IdTblGenerica,
                    idUsuarioCreador: metadata.IdUsuarioCreador,
                    idUsuario: metadata.IdUsuario
                );

                logger.LogInfo(LogsConstants.INFO_BITACORA_REGISTERED_SUCCESS, doc.FileName, metadata.IdPoliza, estatusFinal);

                // 🟢 PASO 7: Traslado en NAS (Guarda en FIRMADOS y elimina de PRIORIDAD)
                string destinationPath = Path.Combine(rutaFirmados, doc.FileName);
                bool fileMoved = false;

                await networkConnection.RunInNetworkScope(fileStore =>
                {
                    fileMoved = storageService.MoveNetworkFile(fileStore, doc.RawFilePath, destinationPath, activePdfBytes);
                });

                if (!fileMoved)
                {
                    logger.LogError(null, LogsConstants.ERROR_FILE_MOVE_FAILED, doc.FileName);
                    continue;
                }

                logger.LogInfo(LogsConstants.INFO_FILE_MOVED_SUCCESS, doc.FileName, destinationPath);

                // 🟢 PASO 8: Emisión del evento en RabbitMQ
                string eventStatus = metadata.FirmaDigital != 0
                    ? SettingsConstants.EVENT_STATUS_SIGNED
                    : SettingsConstants.EVENT_STATUS_UNSIGNED;

                DocumentProcessedEvent processedEvent = new(
                    PolizaId: metadata.IdPoliza,
                    FileName: doc.FileName,
                    Status: eventStatus,
                    ProcessedAt: DateTime.Now
                );

                await eventPublisher.PublishAsync(processedEvent, cancellationToken);

                logger.LogInfo(LogsConstants.INFO_RABBITMQ_EVENT_PUBLISHED, nameof(DocumentProcessedEvent), metadata.IdPoliza, doc.FileName, eventStatus);
                logger.LogInfo(LogsConstants.INFO_DOCUMENT_PROCESSED_SUCCESS, doc.FileName, metadata.IdPoliza);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, LogsConstants.ERROR_DOCUMENT_PROCESSING_FAILED, doc.FileName, ex.Message);
            }
        }

        return Unit.Value;
    }
}

public sealed record DocumentProcessedEvent(
    long PolizaId,
    string FileName,
    string Status,
    DateTime ProcessedAt
);