namespace DigitalSigningTaskRunner.Core.Dto
{
    public class Bitacora
    {
        public int IDBITACORAPOLIZA { get; set; }
        public DateTime FECHA { get; set; }
        public string RUTADOCUMENTO { get; set; } = string.Empty;
        public int IDCATESTATUS { get; set; }
        public long IDPOLIZA { get; set; }
        public string NOMBREARCHIVO { get; set; } = string.Empty;
    }
}
