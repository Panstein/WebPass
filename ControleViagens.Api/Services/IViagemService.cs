using ControleViagens.Api.Contracts;

namespace ControleViagens.Api.Services;

public interface IViagemService
{
    Task<IReadOnlyList<ViagemResponse>> GetAllAsync(ViagemFiltro filtro, CancellationToken cancellationToken);
    Task<ViagemResponse> CreateAsync(CreateViagemRequest request, CancellationToken cancellationToken);
    Task<ViagemResponse?> UpdateAsync(decimal idViagem, UpdateViagemRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(decimal idViagem, CancellationToken cancellationToken);
    Task<FaturamentoResponse?> FaturarAsync(IReadOnlyCollection<decimal> idsViagem, CancellationToken cancellationToken);
    Task<int> CancelarFaturamentoAsync(IReadOnlyCollection<decimal> idsViagem, CancellationToken cancellationToken);
}
