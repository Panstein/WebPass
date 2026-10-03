using ControleViagens.Api.Contracts;
using Npgsql;

namespace ControleViagens.Api.Services;

public sealed class DashboardService(NpgsqlDataSource dataSource) : IDashboardService
{
    /// <summary>Always returns the 12 months of the year, with zeros for months without trips.</summary>
    public async Task<IReadOnlyList<DashboardMesResponse>> GetAnoAsync(int ano, CancellationToken cancellationToken)
    {
        // trechos.valor is money; cast to numeric so it reads as decimal.
        await using var command = dataSource.CreateCommand(
            "WITH realizado AS (" +
            "SELECT EXTRACT(MONTH FROM v.data_viagem)::int AS mes, COUNT(*)::int AS viagens, " +
            "SUM(t.valor::numeric) AS faturamento, " +
            "SUM(t.valor::numeric) FILTER (WHERE v.pago) AS faturamento_pago " +
            "FROM public.viagem v JOIN public.trechos t ON t.id_trecho = v.id_trecho " +
            "WHERE v.data_viagem >= make_date(@ano, 1, 1) AND v.data_viagem < make_date(@ano + 1, 1, 1) " +
            "GROUP BY 1) " +
            "SELECT m.mes, COALESCE(r.viagens, 0), COALESCE(r.faturamento, 0), COALESCE(r.faturamento_pago, 0), " +
            "meta.meta_viagens, meta.meta_faturamento " +
            "FROM generate_series(1, 12) AS m(mes) " +
            "LEFT JOIN realizado r ON r.mes = m.mes " +
            "LEFT JOIN public.meta meta ON meta.ano = @ano AND meta.mes = m.mes " +
            "ORDER BY m.mes");
        command.Parameters.AddWithValue("ano", ano);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var meses = new List<DashboardMesResponse>(12);
        while (await reader.ReadAsync(cancellationToken))
        {
            meses.Add(new DashboardMesResponse(
                reader.GetInt32(0),
                reader.GetInt32(1),
                reader.GetDecimal(2),
                reader.GetDecimal(3),
                reader.IsDBNull(4) ? null : reader.GetInt32(4),
                reader.IsDBNull(5) ? null : reader.GetDecimal(5)));
        }

        return meses;
    }

    public async Task SaveMetaAsync(int ano, int mes, UpdateMetaRequest request, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "INSERT INTO public.meta (ano, mes, meta_viagens, meta_faturamento) " +
            "VALUES (@ano, @mes, @meta_viagens, @meta_faturamento) " +
            "ON CONFLICT (ano, mes) DO UPDATE SET meta_viagens = EXCLUDED.meta_viagens, " +
            "meta_faturamento = EXCLUDED.meta_faturamento");
        command.Parameters.AddWithValue("ano", ano);
        command.Parameters.AddWithValue("mes", mes);
        command.Parameters.AddWithValue("meta_viagens", request.MetaViagens);
        command.Parameters.AddWithValue("meta_faturamento", request.MetaFaturamento);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<bool> DeleteMetaAsync(int ano, int mes, CancellationToken cancellationToken)
    {
        await using var command = dataSource.CreateCommand(
            "DELETE FROM public.meta WHERE ano = @ano AND mes = @mes");
        command.Parameters.AddWithValue("ano", ano);
        command.Parameters.AddWithValue("mes", mes);
        return await command.ExecuteNonQueryAsync(cancellationToken) > 0;
    }
}
