using DigitalSigningTaskRunner.Core.Dto;

namespace DigitalSigningTaskRunner.Core.Interfaces.Repositories;

public interface IRepository
{
    Task<IEnumerable<Parametros>> GetTaskRunnerPathsDataAsync();

    Task<InformacionPoliza?> GetInfoArchivoPDFAsync(string nombre);

    Task<Bitacora?> SetSinAprobacionBitacoraAsync(
        long idPoliza,
        string ruta,
        int estatus,
        string nombreArchivo,
        int idGenerico,
        int idUsuarioCreador,
        int idUsuario
    );
}