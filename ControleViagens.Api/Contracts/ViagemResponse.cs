namespace ControleViagens.Api.Contracts;

/// <summary>Represents a trip with its passenger and route segment data.</summary>
public sealed record ViagemResponse(
    decimal IdViagem,
    DateOnly DataViagem,
    decimal IdPassageiro,
    string NomePassageiro,
    decimal IdTrecho,
    string Origem,
    string Destino,
    decimal Valor,
    bool Pago,
    decimal? GrupoPagamento);
