namespace ControleViagens.Api.Contracts;

/// <summary>Result of billing: the group number shared by all billed trips.</summary>
public sealed record FaturamentoResponse(decimal GrupoPagamento, int Quantidade);

/// <summary>Result of a billing reversal: how many trips went back to unpaid.</summary>
public sealed record CancelamentoFaturamentoResponse(int Quantidade);
