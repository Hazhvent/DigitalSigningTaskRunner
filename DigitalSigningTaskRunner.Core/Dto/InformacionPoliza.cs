namespace DigitalSigningTaskRunner.Core.Dto;

public class InformacionPoliza
{
    public string Nombre { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Dni { get; set; } = string.Empty;
    public string Docs { get; set; } = string.Empty;
    public int Numero { get; set; }
    public List<InformacionLote> Documentos { get; set; } = [];
    public string Contratante { get; set; } = string.Empty;
    public long IdPoliza { get; set; }
    public string Asegurado { get; set; } = string.Empty;
    public long Certificado { get; set; }
    public int Reenvios { get; set; }
    public string Producto { get; set; } = string.Empty;
    public int IdDocumentoRTF { get; set; }
    public int FirmaDigital { get; set; }
    public DateTime Fecha { get; set; }
    public int IdUsuarioCreador { get; set; }
    public int IdUsuario { get; set; }
    public string Operacion { get; set; } = string.Empty;
    public int Prioridad { get; set; }
    public int IdTblGenerica { get; set; }
}