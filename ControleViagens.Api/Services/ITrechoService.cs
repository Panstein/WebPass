using ControleViagens.Api.Contracts;

namespace ControleViagens.Api.Services;

public interface ITrechoService
{
    Task<IReadOnlyList<TrechoResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<TrechoResponse>> SearchByOrigemAsync(string origem, int limite, CancellationToken cancellationToken);
    Task<TrechoResponse> CreateAsync(CreateTrechoRequest request, CancellationToken cancellationToken);
    Task<TrechoResponse?> UpdateAsync(decimal idTrecho, UpdateTrechoRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(decimal idTrecho, CancellationToken cancellationToken);
}