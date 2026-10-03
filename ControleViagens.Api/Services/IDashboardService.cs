using ControleViagens.Api.Contracts;

namespace ControleViagens.Api.Services;

public interface IDashboardService
{
    Task<IReadOnlyList<DashboardMesResponse>> GetAnoAsync(int ano, CancellationToken cancellationToken);
    Task SaveMetaAsync(int ano, int mes, UpdateMetaRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteMetaAsync(int ano, int mes, CancellationToken cancellationToken);
}
