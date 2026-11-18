using Bat.AspNetCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bat.Regression.Tests;

/// <summary>Attribute whose name contains "Authenticate" (what <see cref="BatSwaggerAuthenticateFilter"/> looks for).</summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SampleAuthenticateAttribute : Attribute;

public class SwaggerSampleModel
{
    public string Name { get; set; }

    [SwaggerExclude]
    public string Secret { get; set; }
}

[ApiController, Route("api/sample")]
public class SwaggerSampleController : ControllerBase
{
    [HttpGet("open")] public string Open() => "ok";
    [HttpGet("secure"), Authorize] public string Secure() => "ok";
    [HttpGet("anonymous"), Authorize, AllowAnonymous] public string Anonymous() => "ok";
    [HttpGet("token"), SampleAuthenticate] public string Token() => "ok";
    [HttpPost("model")] public SwaggerSampleModel Model(SwaggerSampleModel model) => model;
}

[SwaggerExclude, ApiController, Route("api/hidden")]
public class SwaggerHiddenController : ControllerBase
{
    [HttpGet] public string Get() => "hidden";
}

/// <summary>
/// Serves a real swagger.json built with Bat's Swagger setup and filters (Swashbuckle 11 / Microsoft.OpenApi 3) from a local server.
/// </summary>
public sealed class SwaggerServer : IAsyncLifetime
{
    public static readonly SwaggerSetting Setting = new()
    {
        Name = "Bat Api",
        Title = "Bat regression",
        Version = "v1",
        Description = "test",
        JsonUrl = "/swagger/BatApi/swagger.json",
        TermsOfService = "https://example.com/terms",
        Contact = new SwaggerContact { Name = "Bat", Email = "bat@example.com", Url = "https://example.com" },
        License = new SwaggerLicense { Name = "MIT", Url = "https://example.com/license" },
    };

    private WebApplication _app;
    public JsonElement Document { get; private set; }
    public string Url { get; private set; }

    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Services.AddControllers().AddApplicationPart(typeof(SwaggerServer).Assembly);
        builder.Services.AddBatSwagger(Setting);
        builder.Services.Configure<SwaggerGenOptions>(o =>
        {
            o.AddSecurityDefinition(BatSwaggerAuthorizeFilter.BearerSchemeName, new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
            });
            o.OperationFilter<BatSwaggerAuthorizeFilter>();
            o.OperationFilter<BatSwaggerAuthenticateFilter>();
            o.SchemaFilter<BatSwaggerExcludeFilter>();
            o.DocumentFilter<BatSwaggerExcludeOperationFilter>();
        });

        _app = builder.Build();
        _app.Urls.Add("http://127.0.0.1:0");
        _app.UseBatSwaggerConfiguration(Setting);
        _app.MapControllers();
        await _app.StartAsync();

        Url = _app.Urls.First();
        using var client = new HttpClient();
        var json = await client.GetStringAsync($"{Url}{Setting.JsonUrl}");
        Document = JsonDocument.Parse(json).RootElement.Clone();
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();
}

public class SwaggerTests(SwaggerServer server) : IClassFixture<SwaggerServer>
{
    private JsonElement Paths => server.Document.GetProperty("paths");

    private JsonElement Get(string path) => Paths.GetProperty(path).GetProperty("get");

    [Fact]
    public void Document_HasInfoFromSwaggerSetting()
    {
        var info = server.Document.GetProperty("info");
        Assert.Equal("Bat regression", info.GetProperty("title").GetString());
        Assert.Equal("v1", info.GetProperty("version").GetString());
        Assert.Equal("MIT", info.GetProperty("license").GetProperty("name").GetString());
        Assert.StartsWith("3.", server.Document.GetProperty("openapi").GetString());
    }

    [Fact]
    public void AuthorizeFilter_AddsBearerRequirementAnd401_403()
    {
        var secure = Get("/api/sample/secure");
        Assert.True(secure.GetProperty("responses").TryGetProperty("401", out _));
        Assert.True(secure.GetProperty("responses").TryGetProperty("403", out _));
        var requirement = Assert.Single(secure.GetProperty("security").EnumerateArray());
        Assert.True(requirement.TryGetProperty("Bearer", out _));

        Assert.False(Get("/api/sample/open").TryGetProperty("security", out _));
        Assert.False(Get("/api/sample/anonymous").TryGetProperty("security", out _));
    }

    [Fact]
    public void AuthenticateFilter_AddsTokenHeader()
    {
        var parameter = Assert.Single(Get("/api/sample/token").GetProperty("parameters").EnumerateArray());
        Assert.Equal("Token", parameter.GetProperty("name").GetString());
        Assert.Equal("header", parameter.GetProperty("in").GetString());
        Assert.True(parameter.GetProperty("required").GetBoolean());

        Assert.False(Get("/api/sample/open").TryGetProperty("parameters", out _));
    }

    [Fact]
    public void ExcludeFilters_HideControllersAndProperties()
    {
        Assert.False(Paths.TryGetProperty("/api/hidden", out _), string.Join(", ", Paths.EnumerateObject().Select(p => p.Name)));
        Assert.True(Paths.TryGetProperty("/api/sample/open", out _));

        var properties = server.Document.GetProperty("components").GetProperty("schemas")
            .GetProperty(nameof(SwaggerSampleModel)).GetProperty("properties");
        Assert.True(properties.TryGetProperty("name", out _));
        Assert.False(properties.TryGetProperty("secret", out _), properties.GetRawText());
    }
}
