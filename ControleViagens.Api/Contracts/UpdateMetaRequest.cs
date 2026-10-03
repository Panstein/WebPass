using System.ComponentModel.DataAnnotations;

namespace ControleViagens.Api.Contracts;

/// <summary>Monthly goals for number of trips and billing.</summary>
public sealed record UpdateMetaRequest
{
    [Range(0, int.MaxValue)]
    public required int MetaViagens { get; init; }

    [Range(typeof(decimal), "0", "9999999999.99", ParseLimitsInInvariantCulture = true)]
    public required decimal MetaFaturamento { get; init; }
}
