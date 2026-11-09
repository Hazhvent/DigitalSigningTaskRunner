using DigitalSigningTaskRunner.Core.Constants.Database;
using DigitalSigningTaskRunner.Core.Dto;
using DigitalSigningTaskRunner.Core.Interfaces.Repositories;
using System.Data;

namespace DigitalSigningTaskRunner.Infrastructure.Persistence.Repositories;

public class Repository(IDbConnection dbConnection) : IRepository
{
    private readonly IDbConnection _dbConnection = dbConnection;

    public async Task<IEnumerable<Parametros>> GetTaskRunnerPathsDataAsync()
    {
        var parametros = new Dictionary<string, object>();

        return await _dbConnection.QueryOracleAsync<Parametros>(
            ProceduresConstants.GET_TASK_RUNNER_PATHS_DATA,
            parametros,
            ParametersConstants.REF_CURSOR
        );
    }

    public async Task<InformacionPoliza?> GetInfoArchivoPDFAsync(string nombre)
    {
        var parametros = new Dictionary<string, object>
        {
            { ParametersConstants.NOMBRE_DOC, nombre }
        };

        return await _dbConnection.QueryOracleFirstOrDefaultAsync<InformacionPoliza>(
            ProceduresConstants.SP_GETARCHIVOPDF,
            parametros,
            ParametersConstants.CURSOR_RET
        );
    }

    public async Task<Bitacora?> SetSinAprobacionBitacoraAsync(
        long idPoliza, string ruta, int estatus, string nombreArchivo,
        int idGenerico, int idUsuarioCreador, int idUsuario)
    {
        var parametros = new Dictionary<string, object>
        {
            { ParametersConstants.P_ID_POLIZA, idPoliza },
            { ParametersConstants.P_RUTA, ruta },
            { ParametersConstants.P_NOMBRE_ARCHIVO, nombreArchivo },
            { ParametersConstants.P_ESTATUS, estatus },
            { ParametersConstants.P_ID_GENERICO, idGenerico },
            { ParametersConstants.P_ID_USUARIO_CREADOR, idUsuarioCreador },
            { ParametersConstants.P_ID_USUARIO, idUsuario }
        };

        return await _dbConnection.QueryOracleFirstOrDefaultAsync<Bitacora>(
            ProceduresConstants.SP_SINAPROBITACORAALTA,
            parametros,
            ParametersConstants.REF_CURSOR
        );
    }
}