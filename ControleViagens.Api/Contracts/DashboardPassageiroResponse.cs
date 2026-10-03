namespace ControleViagens.Api.Contracts;

/// <summary>Billing of one passenger in the period: value of all trips, paid or not.</summary>
public sealed record DashboardPassageiroResponse(
    decimal IdPassageiro,
    string Nome,
    int Viagens,
    decimal Faturamento,
    decimal FaturamentoPago);
