using ControleViagens.Api.Contracts;
using ControleViagens.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace ControleViagens.Api.Endpoints;

public static class TrechosEndpoints
{
    public static IEndpointRouteBuilder MapTrechosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/trechos", async Task<Results<Ok<IReadOnlyList<TrechoResponse>>, ProblemHttpResult>> (
            ITrechoService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return TypedResults.Ok(await service.GetAllAsync(cancellationToken));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Trechos")
                    .LogError(exception, "Falha ao consultar trechos.");
                return TypedResults.Problem(
                    title: "Não foi possível consultar os trechos.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetTrechos")
        .WithSummary("Lista os trechos cadastrados.")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapGet("/api/trechos/busca", async Task<Results<Ok<IReadOnlyList<TrechoResponse>>, ProblemHttpResult>> (
            string? origem,
            int? limite,
            ITrechoService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return TypedResults.Ok(await service.SearchByOrigemAsync(
                    origem ?? string.Empty, Math.Clamp(limite ?? 10, 1, 20), cancellationToken));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Trechos")
                    .LogError(exception, "Falha ao buscar trechos.");
                return TypedResults.Problem(
                    title: "Não foi possível buscar os trechos.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("SearchTrechos")
        .WithSummary("Busca trechos pela origem (até 20 resultados).")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPost("/api/trechos", async Task<Results<Created<TrechoResponse>, ProblemHttpResult>> (
            CreateTrechoRequest request,
            ITrechoService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var trecho = await service.CreateAsync(request, cancellationToken);
                return TypedResults.Created($"/api/trechos/{trecho.IdTrecho}", trecho);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Trechos")
                    .LogError(exception, "Falha ao cadastrar trecho.");
                return TypedResults.Problem(
                    title: "Não foi possível cadastrar o trecho.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("CreateTrecho")
        .WithSummary("Cadastra um trecho.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPut("/api/trechos/{idTrecho:decimal}", async Task<Results<Ok<TrechoResponse>, NotFound, ProblemHttpResult>> (
            decimal idTrecho,
            UpdateTrechoRequest request,
            ITrechoService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var trecho = await service.UpdateAsync(idTrecho, request, cancellationToken);
                return trecho is null ? TypedResults.NotFound() : TypedResults.Ok(trecho);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Trechos")
                    .LogError(exception, "Falha ao atualizar trecho {IdTrecho}.", idTrecho);
                return TypedResults.Problem(
                    title: "Não foi possível atualizar o trecho.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("UpdateTrecho")
        .WithSummary("Atualiza origem, destino e valor de um trecho.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapDelete("/api/trechos/{idTrecho:decimal}", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (
            decimal idTrecho,
            ITrechoService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await service.DeleteAsync(idTrecho, cancellationToken);
                return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                return TypedResults.Problem(
                    title: "O trecho possui viagens registradas e não pode ser excluído.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Trechos")
                    .LogError(exception, "Falha ao excluir trecho {IdTrecho}.", idTrecho);
                return TypedResults.Problem(
                    title: "Não foi possível excluir o trecho.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("DeleteTrecho")
        .WithSummary("Exclui um trecho pelo identificador.")
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}