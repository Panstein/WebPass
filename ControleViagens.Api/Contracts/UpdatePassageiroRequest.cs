using System.ComponentModel.DataAnnotations;

namespace ControleViagens.Api.Contracts;

/// <summary>Payload used to update a passenger name.</summary>
public sealed record UpdatePassageiroRequest
{
    /// <summary>Passenger's required name.</summary>
    [Required]
    public required string Nome { get; init; }
}