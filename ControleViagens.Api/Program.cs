using ControleViagens.Api.Endpoints;
using ControleViagens.Api.Services;
using Microsoft.AspNetCore.StaticFiles;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddValidation();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration["FrontendOrigin"] ?? "http://localhost:5200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

var connectionString = builder.Configuration.GetConnectionString("WebPass")
    ?? throw new InvalidOperationException("A connection string 'WebPass' não foi configurada.");
builder.Services.AddSingleton(Npgsql.NpgsqlDataSource.Create(ToNpgsqlConnectionString(connectionString)));
builder.Services.AddScoped<IPassageiroService, PassageiroService>();
builder.Services.AddScoped<ITrechoService, TrechoService>();
builder.Services.AddScoped<IViagemService, ViagemService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

var app = builder.Build();
var clientIndexPath = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
var hasPublishedClient = File.Exists(clientIndexPath);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".dat"] = "application/octet-stream";
var clientFileOptions = new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider,
    OnPrepareResponse = context =>
    {
        // Files without a fingerprint must be revalidated, otherwise the browser can keep
        // an old service worker manifest and the PWA never picks up a new publication.
        if (context.File.Name is "index.html" or "service-worker.js" or "service-worker-assets.js" or "manifest.webmanifest")
        {
            context.Context.Response.Headers.CacheControl = "no-cache";
        }
    }
};
if (hasPublishedClient)
{
    app.UseDefaultFiles();
    app.UseStaticFiles(clientFileOptions);
}

app.MapHealthEndpoints();
app.MapPassageirosEndpoints();
app.MapTrechosEndpoints();
app.MapViagensEndpoints();
app.MapDashboardEndpoints();
if (hasPublishedClient)
{
    app.MapFallbackToFile("index.html", clientFileOptions);
}

app.Run();

// Npgsql does not parse postgresql:// URIs (the format Neon and Render hand out), so convert them.
static string ToNpgsqlConnectionString(string value)
{
    if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase)
        && !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
    {
        return value;
    }

    var uri = new Uri(value);
    var userInfo = uri.UserInfo.Split(':', 2);
    var csb = new Npgsql.NpgsqlConnectionStringBuilder
    {
        Host = uri.Host,
        Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
        Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
        Username = Uri.UnescapeDataString(userInfo[0]),
        Password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : null
    };

    var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(uri.Query);
    if (query.TryGetValue("sslmode", out var sslMode))
    {
        csb.SslMode = Enum.Parse<Npgsql.SslMode>(sslMode.ToString(), ignoreCase: true);
    }
    if (query.TryGetValue("channel_binding", out var channelBinding))
    {
        csb.ChannelBinding = Enum.Parse<Npgsql.ChannelBinding>(channelBinding.ToString(), ignoreCase: true);
    }

    return csb.ConnectionString;
}
