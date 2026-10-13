using DigitalSigningTaskRunner.Core.Constants.Messages;
using DigitalSigningTaskRunner.Core.Dto;
using DigitalSigningTaskRunner.Core.Interfaces.Services;
using DigitalSigningTaskRunner.Core.Models;
using Microsoft.Extensions.Options;
using System.Net.Http.Json;

namespace DigitalSigningTaskRunner.Infrastructure.Services;

public sealed class DigitalSignatureService(
    HttpClient httpClient,
    IOptions<SigningApiSettings> signingOptions,
    ILoggingService<DigitalSignatureService> logger) : IDigitalSignatureService
{
    private readonly SigningApiSettings _signingSettings = signingOptions.Value;

    public async Task<byte[]?> SignDocumentAsync(
        byte[] unlockedPdfBytes,
        string fileName,
        string passwordfile,
        CancellationToken cancellationToken = default)
    {
        if (unlockedPdfBytes == null || unlockedPdfBytes.Length == 0)
        {
            logger.LogError(null, LogsConstants.ERROR_INVALID_DATA_FORMAT);
            return null;
        }

        try
        {
            string pdfBase64Content = Convert.ToBase64String(unlockedPdfBytes);

            // 🟢 Orden exacto espejado de la clase FirmaBytesRequest de Java
            var payload = new
            {
                archivo = pdfBase64Content,
                nomArchivos = fileName,
                passwordfile = passwordfile,
                fuente = _signingSettings.Fuente,
                usuarioHSM = _signingSettings.UsuarioHSM,
                pinHSM = _signingSettings.PinHSM,
                serieHSM = _signingSettings.SerieHSM
            };

            HttpResponseMessage httpResponse = await httpClient.PostAsJsonAsync(
                _signingSettings.Endpoint,
                payload,
                cancellationToken
            );

            if (!httpResponse.IsSuccessStatusCode)
            {
                string errorContent = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
                logger.LogError(null, LogsConstants.ERROR_SIGNING_API_HTTP, (int)httpResponse.StatusCode, errorContent);
                return null;
            }

            SigningApiResponse? responsePayload = await httpResponse.Content
                .ReadFromJsonAsync<SigningApiResponse>(cancellationToken: cancellationToken);

            if (responsePayload == null || responsePayload.Estado != 1 || string.IsNullOrWhiteSpace(responsePayload.Archivo))
            {
                string messageDetail = !string.IsNullOrWhiteSpace(responsePayload?.Mensaje)
                    ? responsePayload.Mensaje
                    : string.Format(LogsConstants.ERROR_SIGNING_API_INVALID_RESPONSE, responsePayload?.Estado ?? -1);

                logger.LogError(null, LogsConstants.ERROR_SIGNING_API_FAILED, messageDetail);
                return null;
            }

            return Convert.FromBase64String(responsePayload.Archivo);
        }
        catch (TaskCanceledException ex)
        {
            logger.LogError(ex, LogsConstants.ERROR_SIGNING_API_FAILED, LogsConstants.ERROR_SIGNING_API_TIMEOUT_DETAIL);
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, LogsConstants.ERROR_SIGNING_API_FAILED, ex.Message);
            return null;
        }
    }
}