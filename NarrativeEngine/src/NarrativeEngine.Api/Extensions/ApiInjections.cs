using Asp.Versioning;
using Microsoft.OpenApi.Models;
using NarrativeEngine.Api.Conventions;
using NarrativeEngine.Api.Security;
using NarrativeEngine.Api.Services.Auth;
using NarrativeEngine.Api.Services.Interfaces;
using NarrativeEngine.Domain.Common;

namespace NarrativeEngine.Api.Extensions;

public static class ApiInjections
{
    public static IServiceCollection AddNarrativeEngineApi(this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserAccessor, CurrentUserAccessor>();
        services.AddScoped<IUserLifecycleService, UserLifecycleService>();

        var apiRoutePrefix = configuration["Api:RoutePrefix"] ?? "api/v{version:apiVersion}";
        services.AddControllers(options =>
        {
            options.Conventions.Add(new ApiPrefixConvention(apiRoutePrefix));
        });

        services.AddApiVersioning(options =>
        {
            options.DefaultApiVersion = new ApiVersion(1, 0);
            options.AssumeDefaultVersionWhenUnspecified = true;
            options.ReportApiVersions = true;
            options.ApiVersionReader = ApiVersionReader.Combine(
                new UrlSegmentApiVersionReader(),
                new HeaderApiVersionReader("x-api-version")
            );
        }).AddApiExplorer(options =>
        {
            options.GroupNameFormat = "'v'VVV";
            options.SubstituteApiVersionInUrl = true;
        });

       services.AddEndpointsApiExplorer();
       services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo { Title = "Narrative Engine API", Version = "v1" });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                [new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                }] = []
            });
        }); 
       
       return services;
    }
}