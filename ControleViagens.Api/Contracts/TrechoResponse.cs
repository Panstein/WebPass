namespace ControleViagens.Api.Contracts;

/// <summary>Represents a route segment returned by the API.</summary>
public sealed record TrechoResponse(decimal IdTrecho, string Origem, string Destino, decimal Valor);