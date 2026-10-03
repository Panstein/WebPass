using System.ComponentModel.DataAnnotations;

namespace ControleViagens.Api.Contracts;

/// <summary>Payload used to update a route segment.</summary>
public sealed record UpdateTrechoRequest
{
    /// <summary>Origin, limited by the database to 20 characters.</summary>
    [Required, StringLength(20)]
    public required string Origem { get; init; }

    /// <summary>Destination, limited by the database to 20 characters.</summary>
    [Required, StringLength(20)]
    public required string Destino { get; init; }

    /// <summary>Route value between zero and 1000, stored as PostgreSQL money.</summary>
    [Range(typeof(decimal), "0", "1000", ParseLimitsInInvariantCulture = true)]
    public required decimal Valor { get; init; }
}