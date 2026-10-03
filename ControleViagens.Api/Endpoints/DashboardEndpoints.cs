using ControleViagens.Api.Contracts;
using ControleViagens.Api.Services;
using Microsoft.AspNetCore.Http.HttpResults;
using Npgsql;

namespace ControleViagens.Api.Endpoints;

public static class DashboardEndpoints
{
    private const int AnoMinimo = 2000;
    private const int AnoMaximo = 2100;

    public static IEndpointRouteBuilder MapDashboardEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/dashboard/{ano:int}", async Task<Results<Ok<IReadOnlyList<DashboardMesResponse>>, ProblemHttpResult>> (
            int ano,
            IDashboardService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (ano is < AnoMinimo or > AnoMaximo)
            {
                return AnoInvalido();
            }

            try
            {
                return TypedResults.Ok(await service.GetAnoAsync(ano, cancellationToken));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Dashboard")
                    .LogError(exception, "Falha ao consultar o dashboard de {Ano}.", ano);
                return TypedResults.Problem(
                    title: "Não foi possível consultar o dashboard.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetDashboard")
        .WithSummary("Viagens e faturamento de cada mês do ano, com as metas do mês.")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapGet("/api/dashboard/{ano:int}/passageiros", async Task<Results<Ok<IReadOnlyList<DashboardPassageiroResponse>>, ProblemHttpResult>> (
            int ano,
            int? mes,
            IDashboardService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (ano is < AnoMinimo or > AnoMaximo || mes is < 1 or > 12)
            {
                return AnoInvalido();
            }

            try
            {
                return TypedResults.Ok(await service.GetPassageirosAsync(ano, mes, cancellationToken));
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Dashboard")
                    .LogError(exception, "Falha ao consultar o faturamento por passageiro de {Ano}.", ano);
                return TypedResults.Problem(
                    title: "Não foi possível consultar o faturamento por passageiro.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("GetDashboardPassageiros")
        .WithSummary("Faturamento por passageiro no ano ou, com mes, no mês; maior faturamento primeiro.")
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapPut("/api/metas/{ano:int}/{mes:int}", async Task<Results<NoContent, ProblemHttpResult>> (
            int ano,
            int mes,
            UpdateMetaRequest request,
            IDashboardService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            if (ano is < AnoMinimo or > AnoMaximo || mes is < 1 or > 12)
            {
                return AnoInvalido();
            }

            try
            {
                await service.SaveMetaAsync(ano, mes, request, cancellationToken);
                return TypedResults.NoContent();
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Dashboard")
                    .LogError(exception, "Falha ao gravar a meta de {Mes}/{Ano}.", mes, ano);
                return TypedResults.Problem(
                    title: "Não foi possível gravar a meta.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("SaveMeta")
        .WithSummary("Grava (inclui ou altera) as metas de viagens e faturamento do mês.")
        .ProducesValidationProblem()
        .ProducesProblem(StatusCodes.Status400BadRequest)
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        endpoints.MapDelete("/api/metas/{ano:int}/{mes:int}", async Task<Results<NoContent, NotFound, ProblemHttpResult>> (
            int ano,
            int mes,
            IDashboardService service,
            ILoggerFactory loggerFactory,
            CancellationToken cancellationToken) =>
        {
            try
            {
                return await service.DeleteMetaAsync(ano, mes, cancellationToken)
                    ? TypedResults.NoContent()
                    : TypedResults.NotFound();
            }
            catch (NpgsqlException exception)
            {
                loggerFactory.CreateLogger("Dashboard")
                    .LogError(exception, "Falha ao excluir a meta de {Mes}/{Ano}.", mes, ano);
                return TypedResults.Problem(
                    title: "Não foi possível excluir a meta.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        })
        .WithName("DeleteMeta")
        .WithSummary("Remove as metas do mês.")
        .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        return endpoints;
    }

    private static ProblemHttpResult AnoInvalido() =>
        TypedResults.Problem(
            title: $"Informe um ano entre {AnoMinimo} e {AnoMaximo} e um mês entre 1 e 12.",
            statusCode: StatusCodes.Status400BadRequest);
}
