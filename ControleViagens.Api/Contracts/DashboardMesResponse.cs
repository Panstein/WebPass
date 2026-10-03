namespace ControleViagens.Api.Contracts;

/// <summary>
/// One month of the dashboard: trips and billing (value of all trips, paid or not) against
/// the month's goals; goals are null when none was set.
/// </summary>
public sealed record DashboardMesResponse(
    int Mes,
    int Viagens,
    decimal Faturamento,
    decimal FaturamentoPago,
    int? MetaViagens,
    decimal? MetaFaturamento);
