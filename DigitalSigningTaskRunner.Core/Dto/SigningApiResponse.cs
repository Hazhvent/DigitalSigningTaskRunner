namespace DigitalSigningTaskRunner.Core.Dto;

public sealed record SigningApiResponse
{
    public string? Archivo { get; init; }
    public int Estado { get; init; }
    public string? Mensaje { get; init; }
}