namespace DigitalSigningTaskRunner.Core.Interfaces.Services;

public interface IDigitalSignatureService
{
    /// <summary>
    /// Envía el PDF desencriptado en memoria hacia la API externa de Pegasus (HSM)
    /// y devuelve el arreglo de bytes del PDF firmado digitalmente.
    /// Retorna nulo si ocurre un error (el detalle se registra en el log).
    /// </summary>
    Task<byte[]?> SignDocumentAsync(
        byte[] unlockedPdfBytes,
        string fileName,
        string passwordfile,
        CancellationToken cancellationToken = default);
}