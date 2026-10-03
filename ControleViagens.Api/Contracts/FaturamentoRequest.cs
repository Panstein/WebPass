using System.ComponentModel.DataAnnotations;

namespace ControleViagens.Api.Contracts;

/// <summary>Trips selected on the billing screen.</summary>
public sealed record FaturamentoRequest
{
    /// <summary>Identifiers of the selected trips.</summary>
    [Required]
    [MinLength(1)]
    public required decimal[] IdsViagem { get; init; }
}
