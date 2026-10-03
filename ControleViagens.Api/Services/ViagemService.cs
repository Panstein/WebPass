using ControleViagens.Api.Contracts;
using Npgsql;

namespace ControleViagens.Api.Services;

public sealed class ViagemService(NpgsqlDataSource dataSource) : IViagemService
{
    private const string SelectFromViagem =
        "SELECT v.id_viagem, v.data_viagem, v.id_passageiro, p.nome, v.id_trecho, t.origem, t.destino, t.valor, v.pago, v.grupo_pagamento " +
        "FROM {0} v " +
        "JOIN public.passageiros p ON p.id_pass = v.id_passageiro " +
        "JOIN public.trechos t ON t.id_trecho = v.id_trecho";

    public async Task<IReadOnlyList<ViagemResponse>> GetAllAsync(ViagemFiltro filtro, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand();
        var conditions = new List<string>();
        if (filtro.DataInicial is { } dataInicial)
        {
            conditions.Add("v.data_viagem >= @data_inicial");
            command.Parameters.AddWithValue("data_inicial", dataInicial);
        }

        if (filtro.DataFinal is { } dataFinal)
        {
            conditions.Add("v.data_viagem <= @data_final");
            command.Parameters.AddWithValue("data_final", dataFinal);
        }

        if (filtro.IdPassageiro is { } idPassageiro)
        {
            conditions.Add("v.id_passageiro = @id_passageiro");
            command.Parameters.AddWithValue("id_passageiro", idPassageiro);
        }

        if (filtro.IdTrecho is { } idTrecho)
        {
            conditions.Add("v.id_trecho = @id_trecho");
            command.Parameters.AddWithValue("id_trecho", idTrecho);
        }

        if (filtro.Pago is { } pago)
        {
            conditions.Add("v.pago = @pago");
            command.Parameters.AddWithValue("pago", pago);
        }

        var where = conditions.Count == 0 ? string.Empty : " WHERE " + string.Join(" AND ", conditions);
        command.CommandText = string.Format(SelectFromViagem, "public.viagem") + where +
            " ORDER BY v.data_viagem DESC, v.id_viagem DESC";
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var viagens = new List<ViagemResponse>();
        while (await reader.ReadAsync(cancellationToken))
        {
            viagens.Add(ReadViagem(reader));
        }

        return viagens;
    }

    public async Task<ViagemResponse> CreateAsync(CreateViagemRequest request, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "WITH gravada AS (INSERT INTO public.viagem (id_passageiro, id_trecho, data_viagem, pago, grupo_pagamento) " +
            "VALUES (@id_passageiro, @id_trecho, @data_viagem, @pago, " +
            "CASE WHEN @pago THEN nextval('public.viagem_grupo_pagamento_seq') END) RETURNING *) " +
            string.Format(SelectFromViagem, "gravada"));
        command.Parameters.AddWithValue("id_passageiro", request.IdPassageiro);
        command.Parameters.AddWithValue("id_trecho", request.IdTrecho);
        command.Parameters.AddWithValue("data_viagem", request.DataViagem);
        command.Parameters.AddWithValue("pago", request.Pago);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        await reader.ReadAsync(cancellationToken);
        return ReadViagem(reader);
    }

    public async Task<ViagemResponse?> UpdateAsync(decimal idViagem, UpdateViagemRequest request, CancellationToken cancellationToken)
    {
        // Marking as paid opens a new payment group (same sequence as billing); an already-paid
        // trip keeps its group, and unmarking clears it.
        await using var command = dataSource.CreateCommand(
            "WITH gravada AS (UPDATE public.viagem SET id_passageiro = @id_passageiro, id_trecho = @id_trecho, " +
            "data_viagem = @data_viagem, pago = @pago, grupo_pagamento = CASE " +
            "WHEN NOT @pago THEN NULL " +
            "WHEN grupo_pagamento IS NOT NULL THEN grupo_pagamento " +
            "ELSE nextval('public.viagem_grupo_pagamento_seq') END " +
            "WHERE id_viagem = @id_viagem RETURNING *) " +
            string.Format(SelectFromViagem, "gravada"));
        command.Parameters.AddWithValue("id_passageiro", request.IdPassageiro);
        command.Parameters.AddWithValue("id_trecho", request.IdTrecho);
        command.Parameters.AddWithValue("data_viagem", request.DataViagem);
        command.Parameters.AddWithValue("pago", request.Pago);
        command.Parameters.AddWithValue("id_viagem", idViagem);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadViagem(reader) : null;
    }

    public async Task<bool> DeleteAsync(decimal idViagem, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "DELETE FROM public.viagem WHERE id_viagem = @id_viagem");
        command.Parameters.AddWithValue("id_viagem", idViagem);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }

    /// <summary>
    /// Bills all the given trips under one new group number. All or nothing: returns null
    /// (and changes nothing) if any trip does not exist or is already paid.
    /// </summary>
    public async Task<FaturamentoResponse?> FaturarAsync(IReadOnlyCollection<decimal> idsViagem, CancellationToken cancellationToken)
    {
        var ids = idsViagem.Distinct().ToArray();
        await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // The CTE is evaluated once, so every billed trip gets the same nextval.
        await using var command = new NpgsqlCommand(
            "WITH grupo AS (SELECT nextval('public.viagem_grupo_pagamento_seq') AS numero) " +
            "UPDATE public.viagem v SET pago = true, grupo_pagamento = grupo.numero FROM grupo " +
            "WHERE v.id_viagem = ANY(@ids) AND NOT v.pago " +
            "RETURNING v.grupo_pagamento", connection, transaction);
        command.Parameters.AddWithValue("ids", ids);

        decimal? grupo = null;
        var quantidade = 0;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                grupo = reader.GetDecimal(0);
                quantidade++;
            }
        }

        if (quantidade != ids.Length)
        {
            await transaction.RollbackAsync(cancellationToken);
            return null;
        }

        await transaction.CommitAsync(cancellationToken);
        return new FaturamentoResponse(grupo!.Value, quantidade);
    }

    /// <summary>
    /// Reverses billing for the whole group of each given trip; paid trips without a group
    /// (legacy data) are reversed individually.
    /// </summary>
    public async Task<int> CancelarFaturamentoAsync(IReadOnlyCollection<decimal> idsViagem, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "UPDATE public.viagem SET pago = false, grupo_pagamento = NULL " +
            "WHERE pago AND (id_viagem = ANY(@ids) OR grupo_pagamento IN (" +
            "SELECT grupo_pagamento FROM public.viagem WHERE id_viagem = ANY(@ids) AND grupo_pagamento IS NOT NULL))");
        command.Parameters.AddWithValue("ids", idsViagem.Distinct().ToArray());
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static ViagemResponse ReadViagem(NpgsqlDataReader reader) =>
        new(
            reader.GetDecimal(0),
            reader.GetFieldValue<DateOnly>(1),
            reader.GetDecimal(2),
            reader.GetString(3),
            reader.GetDecimal(4),
            reader.GetString(5).Trim(),
            reader.GetString(6).Trim(),
            reader.GetDecimal(7),
            reader.GetBoolean(8),
            reader.IsDBNull(9) ? null : reader.GetDecimal(9));
}
