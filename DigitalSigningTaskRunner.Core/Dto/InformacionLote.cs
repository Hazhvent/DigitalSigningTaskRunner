namespace DigitalSigningTaskRunner.Core.Dto
{
    public class InformacionLote
    {
        public string NombreDocumento { get; set; } = string.Empty;
        public string NuevoNombre { get; set; } = string.Empty;
        public string RutaRamoProducto { get; set; } = string.Empty;
        public int Externo { get; set; }
        public int IdTblGenerica { get; set; }
        public string Lote { get; set; } = string.Empty;
        public int TipoDocumento { get; set; }
        public int IsPolizaOrigenExcel { get; set; }
        public long IdPoliza { get; set; }
        public string Asunto { get; set; } = string.Empty;
        public int Ramo { get; set; }
        public bool IsCampoFe { get; set; }
        public bool IsCajaLosAndes { get; set; }
        public long IdCertificado { get; set; }
        public int IdDocumentoRTF { get; set; }
        public string GenerSaludo { get; set; } = string.Empty;
    }
}