namespace ControleViagens.Api.Contracts;

/// <summary>Passenger record returned by the API.</summary>
public sealed record PassageiroResponse(decimal IdPass, string Nome);