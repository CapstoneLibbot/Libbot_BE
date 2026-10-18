using LiBot.Api.Hubs;
using LiBot.Api.Middleware;
using LiBot.Api.Services;
using LiBot.Infrastructure;
using LiBot.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

var dotEnvSettings = DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddInMemoryCollection(dotEnvSettings);

builder.Services.AddControllers();
builder.Services.AddSignalR();
builder.Services.AddCors(options => options.AddPolicy("Frontend", policy =>
{
    var origins = builder.Configuration.GetSection("CorsAllowedOrigins").Get<string[]>()
        ?? ["http://localhost:5173"];
    policy.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
}));
builder.Services.AddSingleton<RobotRuntimeService>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<RobotRuntimeService>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "LiBot.Api", Version = "v1" }));
builder.Services.AddInfrastructure(builder.Configuration);

var app = builder.Build();

await app.Services.InitializeLiBotDatabaseAsync();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseHttpsRedirection();
}

app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("Frontend");
app.UseAuthorization();

app.MapControllers();
app.MapHub<RobotHub>("/robotHub");

app.Run();
