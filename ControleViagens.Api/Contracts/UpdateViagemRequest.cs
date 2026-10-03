using System.ComponentModel.DataAnnotations;

namespace ControleViagens.Api.Contracts;

/// <summary>Payload used to update a trip.</summary>
public sealed record UpdateViagemRequest
{
    /// <summary>Passenger identifier; must exist in passageiros.</summary>
    [Required]
    public required decimal IdPassageiro { get; init; }

    /// <summary>Route segment identifier; must exist in trechos.</summary>
    [Required]
    public required decimal IdTrecho { get; init; }

    /// <summary>Trip date.</summary>
    [Required]
    public required DateOnly DataViagem { get; init; }

    /// <summary>Whether the trip has been paid; defaults to false.</summary>
    public bool Pago { get; init; }
}
