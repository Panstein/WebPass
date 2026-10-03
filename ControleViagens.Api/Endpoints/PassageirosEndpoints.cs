using ControleViagens.Api.Contracts;
using ControleViagens.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace ControleViagens.Api.Endpoints;

public static class PassageirosEndpoints
{
    public static IEndpointRouteBuilder MapPassageirosEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/passageiros", async Task<Results<Ok<IReadOnlyList<PassageiroResponse>>, ProblemHttpResult>> (
            IPassageiroService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return TypedResults.Ok(await service.GetAllAsync(cancellationToken));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Passageiros")
                    .LogError(exception, "Falha ao consultar passageiros.");
                return TypedResults.Problem(
                    title: "Não foi possível consultar os passageiros.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetPassageiros")
        .WithSummary("Lista os passageiros cadastrados.")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapGet("/api/passageiros/busca", async Task<Results<Ok<IReadOnlyList<PassageiroResponse>>, ProblemHttpResult>> (
            string? nome,
            int? limite,
            IPassageiroService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return TypedResults.Ok(await service.SearchByNomeAsync(
                    nome ?? string.Empty, Math.Clamp(limite ?? 10, 1, 20), cancellationToken));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Passageiros")
                    .LogError(exception, "Falha ao buscar passageiros.");
                return TypedResults.Problem(
                    title: "Não foi possível buscar os passageiros.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("SearchPassageiros")
        .WithSummary("Busca passageiros pelo nome (até 20 resultados).")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPost("/api/passageiros", async Task<Results<Created<PassageiroResponse>, ProblemHttpResult>> (
            CreatePassageiroRequest request,
            IPassageiroService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var nome = request.Nome.Trim();
                if (nome.Length == 0)
                {
                    return TypedResults.Problem(
                        title: "Informe o nome do passageiro.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var passageiro = await service.CreateAsync(nome, cancellationToken);
                return TypedResults.Created($"/api/passageiros/{passageiro.IdPass}", passageiro);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Passageiros")
                    .LogError(exception, "Falha ao cadastrar passageiro.");
                return TypedResults.Problem(
                    title: "Não foi possível cadastrar o passageiro.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("CreatePassageiro")
        .WithSummary("Cadastra um passageiro.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPut("/api/passageiros/{idPass:decimal}", async Task<Results<Ok<PassageiroResponse>, NotFound, ProblemHttpResult>> (
            decimal idPass,
            UpdatePassageiroRequest request,
            IPassageiroService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var nome = request.Nome.Trim();
                if (nome.Length == 0)
                {
                    return TypedResults.Problem(
                        title: "Informe o nome do passageiro.",
                        statusCode: StatusCodes.Status400BadRequest);
                }

                var updated = await service.UpdateAsync(idPass, nome, cancellationToken);
                return updated
                    ? TypedResults.Ok(new PassageiroResponse(idPass, nome))
                    : TypedResults.NotFound();
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Passageiros")
                    .LogError(exception, "Falha ao atualizar passageiro {IdPass}.", idPass);
                return TypedResults.Problem(
                    title: "Não foi possível atualizar o passageiro.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("UpdatePassageiro")
        .WithSummary("Atualiza o nome de um passageiro.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapDelete("/api/passageiros/{idPass:decimal}", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (
            decimal idPass,
            IPassageiroService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                var deleted = await service.DeleteAsync(idPass, cancellationToken);
                return deleted ? TypedResults.NoContent() : TypedResults.NotFound();
            }
            catch (PostgresException exception) when (exception.SqlState == PostgresErrorCodes.ForeignKeyViolation)
            {
                return TypedResults.Problem(
                    title: "O passageiro possui viagens registradas e não pode ser excluído.",
                    statusCode: StatusCodes.Status409Conflict);
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Passageiros")
                    .LogError(exception, "Falha ao excluir passageiro {IdPass}.", idPass);
                return TypedResults.Problem(
                    title: "Não foi possível excluir o passageiro.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("DeletePassageiro")
        .WithSummary("Exclui um passageiro pelo identificador.")
        .ProducesProblem(StatusCodes.Status409Conflict)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}