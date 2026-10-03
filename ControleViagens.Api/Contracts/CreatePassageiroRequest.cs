using System.ComponentModel.DataAnnotations;

namespace ControleViagens.Api.Contracts;

/// <summary>Payload used to create a passenger.</summary>
public sealed record CreatePassageiroRequest
{
    /// <summary>Passenger's required name.</summary>
    [Required]
    public required string Nome { get; init; }
}