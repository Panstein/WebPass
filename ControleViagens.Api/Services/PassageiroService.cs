using ControleViagens.Api.Contracts;
using Npgsql;

namespace ControleViagens.Api.Services;

public sealed class PassageiroService(NpgsqlDataSource dataSource) : IPassageiroService
{
    public async Task<IReadOnlyList<PassageiroResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id_pass, nome FROM public.passageiros ORDER BY nome, id_pass");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var passageiros = new List<PassageiroResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            passageiros.Add(new PassageiroResponse(reader.GetDecimal(0), reader.GetString(1)));
        }

        return passageiros;
    }

    public async Task<IReadOnlyList<PassageiroResponse>> SearchByNomeAsync(string nome, int limite, CancellationToken cancellationToken)
    {
        // Matches anywhere in the name, listing names that start with the term first.
        await using var command = dataSource.CreateCommand(
            "SELECT id_pass, nome FROM public.passageiros WHERE nome ILIKE @contem " +
            "ORDER BY nome ILIKE @inicio DESC, nome, id_pass LIMIT @limite");
        command.Parameters.AddWithValue("contem", SqlLike.Contains(nome));
        command.Parameters.AddWithValue("inicio", SqlLike.StartsWith(nome));
        command.Parameters.AddWithValue("limite", limite);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var passageiros = new List<PassageiroResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            passageiros.Add(new PassageiroResponse(reader.GetDecimal(0), reader.GetString(1)));
        }

        return passageiros;
    }

    public async Task<PassageiroResponse> CreateAsync(string nome, CancellationToken cancellationToken)
    {
        var nomeNormalizado = nome.Trim().ToUpperInvariant();
        await using var command = dataSource.CreateCommand(
            "INSERT INTO public.passageiros (nome) VALUES (@nome) RETURNING id_pass, nome");
        command.Parameters.AddWithValue("nome", nomeNormalizado);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return new PassageiroResponse(reader.GetDecimal(0), reader.GetString(1));
    }

    public async Task<bool> UpdateAsync(decimal idPass, string nome, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "UPDATE public.passageiros SET nome = @nome WHERE id_pass = @id_pass");
        command.Parameters.AddWithValue("nome", nome);
        command.Parameters.AddWithValue("id_pass", idPass);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    public async Task<bool> DeleteAsync(decimal idPass, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "DELETE FROM public.passageiros WHERE id_pass = @id_pass");
        command.Parameters.AddWithValue("id_pass", idPass);

        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}