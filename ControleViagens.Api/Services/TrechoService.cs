using ControleViagens.Api.Contracts;
using Npgsql;
using NpgsqlTypes;

namespace ControleViagens.Api.Services;

public sealed class TrechoService(NpgsqlDataSource dataSource) : ITrechoService
{
    public async Task<IReadOnlyList<TrechoResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "SELECT id_trecho, origem, destino, valor FROM public.trechos ORDER BY id_trecho");
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var trechos = new List<TrechoResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            trechos.Add(ReadTrecho(reader));
        }

        return trechos;
    }

    public async Task<IReadOnlyList<TrechoResponse>> SearchByOrigemAsync(string origem, int limite, CancellationToken cancellationToken)
    {
        // Matches anywhere in the origin, listing origins that start with the term first.
        await using var command = dataSource.CreateCommand(
            "SELECT id_trecho, origem, destino, valor FROM public.trechos WHERE origem::text ILIKE @contem " +
            "ORDER BY origem::text ILIKE @inicio DESC, origem, destino, id_trecho LIMIT @limite");
        command.Parameters.AddWithValue("contem", SqlLike.Contains(origem));
        command.Parameters.AddWithValue("inicio", SqlLike.StartsWith(origem));
        command.Parameters.AddWithValue("limite", limite);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var trechos = new List<TrechoResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            trechos.Add(ReadTrecho(reader));
        }

        return trechos;
    }

    public async Task<TrechoResponse> CreateAsync(CreateTrechoRequest request, CancellationToken cancellationToken)
    {
        var origem = Normalize(request.Origem);
        var destino = Normalize(request.Destino);
        await using var command = dataSource.CreateCommand(
            "INSERT INTO public.trechos (origem, destino, valor) VALUES (@origem, @destino, @valor) " +
            "RETURNING id_trecho, origem, destino, valor");
        command.Parameters.AddWithValue("origem", origem);
        command.Parameters.AddWithValue("destino", destino);
        command.Parameters.Add("valor", NpgsqlDbType.Money).Value = request.Valor;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return ReadTrecho(reader);
    }

    public async Task<TrechoResponse?> UpdateAsync(decimal idTrecho, UpdateTrechoRequest request, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "UPDATE public.trechos SET origem = @origem, destino = @destino, valor = @valor " +
            "WHERE id_trecho = @id_trecho RETURNING id_trecho, origem, destino, valor");
        command.Parameters.AddWithValue("origem", Normalize(request.Origem));
        command.Parameters.AddWithValue("destino", Normalize(request.Destino));
        command.Parameters.Add("valor", NpgsqlDbType.Money).Value = request.Valor;
        command.Parameters.AddWithValue("id_trecho", idTrecho);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadTrecho(reader) : null;
    }

    public async Task<bool> DeleteAsync(decimal idTrecho, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "DELETE FROM public.trechos WHERE id_trecho = @id_trecho");
        command.Parameters.AddWithValue("id_trecho", idTrecho);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    private static string Normalize(string value) => value.Trim().ToUpperInvariant();

    private static TrechoResponse ReadTrecho(NpgsqlDataReader reader) =>
        new(reader.GetDecimal(0), reader.GetString(1).Trim(), reader.GetString(2).Trim(), reader.GetDecimal(3));
}