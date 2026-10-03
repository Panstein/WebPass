using ControleViagens.Api.Contracts;

namespace ControleViagens.Api.Services;

public interface IPassageiroService
{
    Task<IReadOnlyList<PassageiroResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<PassageiroResponse>> SearchByNomeAsync(string nome, int limite, CancellationToken cancellationToken);
    Task<PassageiroResponse> CreateAsync(string nome, CancellationToken cancellationToken);
    Task<bool> UpdateAsync(decimal idPass, string nome, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(decimal idPass, CancellationToken cancellationToken);
}