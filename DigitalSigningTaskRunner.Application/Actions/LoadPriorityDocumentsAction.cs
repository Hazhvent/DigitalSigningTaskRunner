using DigitalSigningTaskRunner.Core.Dto;
using DigitalSigningTaskRunner.Core.Interfaces.Repositories;
using DigitalSigningTaskRunner.Core.Interfaces.Services;
using DigitalSigningTaskRunner.Core.Models;
using MediatR;
using Microsoft.Extensions.Options;

namespace DigitalSigningTaskRunner.Application.Actions;

public sealed record PriorityDocumentItemDto(
    string FileName,
    string RawFilePath,
    byte[] FileBytes,
    InformacionPoliza Metadata
);

public sealed record LoadPriorityDocumentsActionRequest(string PriorityFolderRelativePath, int BatchSize)
    : IRequest<LoadPriorityDocumentsActionResponse>;

public sealed record LoadPriorityDocumentsActionResponse(
    List<PriorityDocumentItemDto> Documents,
    List<string> MetadataNotFoundFileNames,
    bool DirectoryExists
);

public class LoadPriorityDocumentsActionHandler(
    INetworkConnectionService networkConnection,
    IStorageService storageService,
    IRepository repository,
    IOptions<PriorityWordsSettings> priorityWordsOptions)
    : IRequestHandler<LoadPriorityDocumentsActionRequest, LoadPriorityDocumentsActionResponse>
{
    private readonly PriorityWordsSettings _prioritySettings = priorityWordsOptions.Value;

    public async Task<LoadPriorityDocumentsActionResponse> Handle(
        LoadPriorityDocumentsActionRequest request,
        CancellationToken cancellationToken)
    {
        var tempReadFiles = new List<(string FileName, string RawPath, byte[] Bytes)>();
        bool directoryExists = true;

        await networkConnection.RunInNetworkScope(fileStore =>
        {
            if (!storageService.CheckNetworkDirectoryExists(fileStore, request.PriorityFolderRelativePath))
            {
                directoryExists = false;
                return;
            }

            // 1. Obtener SOLO LOS NOMBRES de los archivos (es instantáneo)
            var pdfFiles = storageService.GetFilesInDirectory(fileStore, request.PriorityFolderRelativePath);

            // 2. ORDENAR LOS NOMBRES según las PriorityWords ANTES de hacer el corte
            var orderedFileNames = pdfFiles.OrderByDescending(fileName =>
                _prioritySettings.Keys.Any(kw =>
                    !string.IsNullOrWhiteSpace(kw) &&
                    fileName.Contains(kw, StringComparison.OrdinalIgnoreCase)
                )
            ).ToList();

            // 3. APLICAR EL BATCH SIZE al inicio de los archivos ya priorizados
            var batchFiles = orderedFileNames.Take(request.BatchSize);

            // 4. LEER LOS BYTES pesados SOLO para los archivos ganadores
            foreach (var fileName in batchFiles)
            {
                string smbFilePath = Path.Combine(request.PriorityFolderRelativePath, fileName);
                byte[]? bytes = storageService.ReadNetworkFileBytes(fileStore, smbFilePath);

                if (bytes != null && bytes.Length > 0)
                {
                    tempReadFiles.Add((fileName, smbFilePath, bytes));
                }
            }
        });

        if (!directoryExists || tempReadFiles.Count == 0)
        {
            return new LoadPriorityDocumentsActionResponse([], [], directoryExists);
        }

        var resultList = new List<PriorityDocumentItemDto>();
        var metadataNotFoundFiles = new List<string>();

        // 5. Consultar Oracle únicamente por el lote de archivos
        foreach (var (fileName, rawPath, bytes) in tempReadFiles)
        {
            InformacionPoliza? metadata = await repository.GetInfoArchivoPDFAsync(fileName);

            if (metadata == null)
            {
                metadataNotFoundFiles.Add(fileName);
                continue;
            }

            // Ya vienen ordenados de la NAS, solo los empaquetamos
            resultList.Add(new PriorityDocumentItemDto(fileName, rawPath, bytes, metadata));
        }

        return new LoadPriorityDocumentsActionResponse(resultList, metadataNotFoundFiles, directoryExists);
    }
}