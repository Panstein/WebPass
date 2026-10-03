using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace ControleViagens.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/health", async Task<Results<Ok<DatabaseHealthResponse>, ProblemHttpResult>> (
            NpgsqlDataSource dataSource,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                await using var connection = await dataSource.OpenConnectionAsync(cancellationToken);
                await using var command = new NpgsqlCommand("SELECT 1", connection);
                await command.ExecuteScalarAsync(cancellationToken);

                return TypedResults.Ok(new DatabaseHealthResponse("conectado", DateTimeOffset.UtcNow));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("DatabaseHealth")
                    .LogWarning(exception, "Falha ao verificar a conexão com o PostgreSQL.");
                return TypedResults.Problem(
                    title: "Banco PostgreSQL indisponível",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetDatabaseHealth")
        .WithSummary("Testa a conexão com o banco PostgreSQL.")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }
}

/// <summary>Resultado do teste de conexão com o banco de dados.</summary>
public sealed record DatabaseHealthResponse(string Status, DateTimeOffset CheckedAt);