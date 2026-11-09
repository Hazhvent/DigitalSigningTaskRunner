using DigitalSigningTaskRunner.Core.Interfaces.Services;
using MediatR;

namespace DigitalSigningTaskRunner.Application.Actions;

public sealed record ExecuteDigitalSignatureActionRequest(
    byte[] EncryptedPdfBytes,
    string FileName,
    string Password
) : IRequest<byte[]?>;

public class ExecuteDigitalSignatureActionHandler(
    IDigitalSignatureService digitalSignature)
    : IRequestHandler<ExecuteDigitalSignatureActionRequest, byte[]?>
{
    public async Task<byte[]?> Handle(ExecuteDigitalSignatureActionRequest request, CancellationToken cancellationToken)
    {
        // 1. Mandar los bytes originales (encriptados) directamente a Pegasus.
        // Pegasus usará el "request.Password" para abrirlo internamente, firmarlo y devolverlo sellado.
        byte[]? signedPdf = await digitalSignature.SignDocumentAsync(
            request.EncryptedPdfBytes,
            request.FileName,
            request.Password,
            cancellationToken
        );

        if (signedPdf == null) return null;

        // 2. Devolver directamente el archivo intacto que procesó el HSM
        return signedPdf;
    }
}