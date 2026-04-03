using AspNetCoreRateLimit;
using Microsoft.EntityFrameworkCore;
using NarrativeEngine.Api.Extensions;
using NarrativeEngine.Api.Middleware;
using NarrativeEngine.Infrastructure.Data;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, cfg) => cfg.ReadFrom.Configuration(ctx.Configuration));

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default"))
        .UseCamelCaseNamingConvention()
    );

// Dependency Injections
builder.Services.AddNarrativeEngineApi(builder.Configuration);
builder.Services.AddAuthInjection(builder.Configuration);
builder.Services.AddTimeInjection(builder.Configuration);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddMemoryCache();
builder.Services.AddHealthChecks();
var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();

app.UseRateLimiter();
app.UseIpRateLimiting();
app.UseClientRateLimiting();

app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Narrative Engine API v1"));

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHealthChecks("/health");

app.Run();
