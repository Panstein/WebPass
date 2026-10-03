namespace ControleViagens.Api.Contracts;

/// <summary>Optional filters for listing trips; null values are ignored.</summary>
public sealed record ViagemFiltro(
    DateOnly? DataInicial,
    DateOnly? DataFinal,
    decimal? IdPassageiro,
    decimal? IdTrecho);
