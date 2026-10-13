namespace DigitalSigningTaskRunner.Core.Models
{
    // [no tocar 🔒]
    public sealed record SigningApiSettings // uso de null! por principio de fail-fast
    {
        public string Endpoint { get; init; } = null!;
        public string UsuarioHSM { get; init; } = null!;
        public string PinHSM { get; init; } = null!;
        public string SerieHSM { get; init; } = null!;
        public string Fuente { get; init; } = null!;
    }
}