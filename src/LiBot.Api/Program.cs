using LiBot.Api.Hubs;
using LiBot.Api.Middleware;
using LiBot.Application.Services.Robot;
using LiBot.Infrastructure;
using LiBot.Infrastructure.Persistence;
using Microsoft.OpenApi.Models;

var dotEnvSettings = DotEnvLoader.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddInMemoryCollection(dotEnvSettings);

builder.Services.AddControllers();
builder.Services.AddSignalR();
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

app.Run();
