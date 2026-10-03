using ControleViagens.Api.Contracts;
using ControleViagens.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace ControleViagens.Api.Endpoints;

public static class ViagensEndpoints
{
    private const string ReferenciaInvalida = "O passageiro ou o trecho informado não está cadastrado.";

    public static IEndpointRouteBuilder MapViagensEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/viagens", async Task<Results<Ok<IReadOnlyList<ViagemResponse>>, ProblemHttpResult>> (
            DateOnly? dataInicial,
            DateOnly? dataFinal,
            decimal? idPassageiro,
            decimal? idTrecho,
            bool? pago,
            IViagemService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (dataInicial > dataFinal)
            {
                return TypedResults.Problem(
                    title: "A data inicial não pode ser posterior à data final.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            try
            {
                var filtro = new ViagemFiltro(dataInicial, dataFinal, idPassageiro, idTrecho, pago);
                return TypedResults.Ok(await service.GetAllAsync(filtro, cancellationToken));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Viagens")
                    .LogError(exception, "Falha ao consultar viagens.");
                return TypedResults.Problem(
                    title: "Não foi possível consultar as viagens.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetViagens")
        .WithSummary("Lista as viagens com passageiro e trecho, com filtros opcionais por período, passageiro, trecho e situação de pagamento.")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPost("/api/viagens", async Task<Results<Created<ViagemResponse>, ProblemHttpResult>> (
            CreateViagemRequest request,
            IViagemService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var viagem = await service.CreateAsync(request, cancellationToken);
                return TypedResults.Created($"/api/viagens/{viagem.IdViagem}", viagem);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                return TypedResults.Problem(title: ReferenciaInvalida, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Viagens")
                    .LogError(exception, "Falha ao cadastrar viagem.");
                return TypedResults.Problem(
                    title: "Não foi possível cadastrar a viagem.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("CreateViagem")
        .WithSummary("Cadastra uma viagem.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPut("/api/viagens/{idViagem:decimal}", async Task<Results<Ok<ViagemResponse>, NotFound, ProblemHttpResult>> (
            decimal idViagem,
            UpdateViagemRequest request,
            IViagemService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var viagem = await service.UpdateAsync(idViagem, request, cancellationToken);
                return viagem is null ? TypedResults.NotFound() : TypedResults.Ok(viagem);
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                return TypedResults.Problem(title: ReferenciaInvalida, statusCode: StatusCodes.Status400BadRequest);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Viagens")
                    .LogError(exception, "Falha ao atualizar viagem {IdViagem}.", idViagem);
                return TypedResults.Problem(
                    title: "Não foi possível atualizar a viagem.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("UpdateViagem")
        .WithSummary("Atualiza passageiro, trecho e data de uma viagem.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapDelete("/api/viagens/{idViagem:decimal}", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (
            decimal idViagem,
            IViagemService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await service.DeleteAsync(idViagem, cancellationToken);
                return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Viagens")
                    .LogError(exception, "Falha ao excluir viagem {IdViagem}.", idViagem);
                return TypedResults.Problem(
                    title: "Não foi possível excluir a viagem.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("DeleteViagem")
        .WithSummary("Exclui uma viagem pelo identificador.")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPost("/api/viagens/faturamento", async Task<Results<Ok<FaturamentoResponse>, ProblemHttpResult>> (
            FaturamentoRequest request,
            IViagemService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var faturamento = await service.FaturarAsync(request.IdsViagem, cancellationToken);
                return faturamento is null
                    ? TypedResults.Problem(
                        title: "Alguma das viagens selecionadas não existe mais ou já foi paga. Nada foi faturado.",
                        statusCode: StatusCodes.Status409Conflict)
                    : TypedResults.Ok(faturamento);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Viagens")
                    .LogError(exception, "Falha ao faturar viagens.");
                return TypedResults.Problem(
                    title: "Não foi possível faturar as viagens.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("FaturarViagens")
        .WithSummary("Marca as viagens como pagas sob um novo número de grupo de pagamento, igual para todas.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPost("/api/viagens/faturamento/cancelamento", async Task<Results<Ok<CancelamentoFaturamentoResponse>, ProblemHttpResult>> (
            FaturamentoRequest request,
            IViagemService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var quantidade = await service.CancelarFaturamentoAsync(request.IdsViagem, cancellationToken);
                return TypedResults.Ok(new CancelamentoFaturamentoResponse(quantidade));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Viagens")
                    .LogError(exception, "Falha ao cancelar faturamento.");
                return TypedResults.Problem(
                    title: "Não foi possível cancelar o faturamento.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("CancelarFaturamento")
        .WithSummary("Estorna o faturamento dos grupos das viagens informadas: pago = false e grupo de pagamento nulo.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}
