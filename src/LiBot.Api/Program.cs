using LiBot.Api.Hubs;
using LiBot.Api.Middleware;
using LiBot.Api.VoiceTools;
using LiBot.Application.Models;
using LiBot.Application.Services.Robot;
using LiBot.Infrastructure;
using LiBot.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Models;
using ModelContextProtocol.AspNetCore;
using System.Security.Cryptography;
using System.Text;

var dotEnvSettings = DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddInMemoryCollection(dotEnvSettings);

builder.Services.AddControllers()
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var message = string.Join(
                "; ",
                context.ModelState.Values
                    .SelectMany(entry => entry.Errors)
                    .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                        ? "The request is invalid."
                        : error.ErrorMessage));
            return new BadRequestObjectResult(BaseResponse<object>.Failure(message));
        };
    });
builder.Services.AddSignalR();
builder.Services.AddOptions<VoiceToolsOptions>()
    .Bind(builder.Configuration.GetSection(VoiceToolsOptions.SectionName))
    .Validate(options => !string.IsNullOrWhiteSpace(options.ApiToken),
        "VoiceTools:ApiToken must be configured.")
    .ValidateOnStart();
builder.Services.AddScoped<LiBotVoiceTools>();
builder.Services.AddMcpServer()
    .WithHttpTransport(options => options.SessionMode = HttpServerSessionMode.Stateless)
    .WithTools<LiBotVoiceTools>();
builder.Services.AddHttpClient("RobotCamera", client =>
{
    client.Timeout = Timeout.InfiniteTimeSpan;
}).ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
{
    AllowAutoRedirect = false,
    ConnectTimeout = TimeSpan.FromSeconds(5)
});
builder.Services.AddSingleton<IPatrolNotificationPublisher, PatrolNotificationHubPublisher>();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    var origins = builder.Configuration.GetSection("CorsAllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"];
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));
builder.Services.AddSingleton<IRobotRuntimeEventPublisher, RobotRuntimeHubPublisher>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LiBot API",
        Version = "v1",
        Description = "API quản lý thư viện, workflow/mission và điều khiển robot LiBot. Mỗi nhóm endpoint có mô tả chức năng; xem XML documentation tại các operation để biết mục đích và cách sử dụng."
    });
    options.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory, "LiBot.Api.xml"), includeControllerXmlComments: true);
});
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeLiBotDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else if (app.Configuration.GetValue<bool>("HttpsRedirection:Enabled"))
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();
app.MapHub<RobotHub>("/robotHub");
app.MapMcp("/mcp/voice-tools")
    .AddEndpointFilter(async (context, next) =>
    {
        var configuredToken = context.HttpContext.RequestServices
            .GetRequiredService<Microsoft.Extensions.Options.IOptions<VoiceToolsOptions>>()
            .Value.ApiToken;
        var authorization = context.HttpContext.Request.Headers.Authorization.ToString();
        const string bearerPrefix = "Bearer ";

        if (!authorization.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase) ||
            !TokensMatch(authorization[bearerPrefix.Length..], configuredToken))
            return Results.Unauthorized();

        return await next(context);
    });

app.Run();

static bool TokensMatch(string presentedToken, string configuredToken)
{
    var presented = Encoding.UTF8.GetBytes(presentedToken);
    var configured = Encoding.UTF8.GetBytes(configuredToken);
    return CryptographicOperations.FixedTimeEquals(presented, configured);
}
