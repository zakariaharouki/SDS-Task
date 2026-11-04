using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using SchemeTracker.Api.Dtos;
using SchemeTracker.Api.Interfaces;
using SchemeTracker.Api.Models;
using SchemeTracker.Api.Options;
using SchemeTracker.Api.Repositories;
using SchemeTracker.Api.Services;
using SchemeTracker.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers(options =>
    {
        options.Filters.Add<ValidationExceptionFilter>();
        options.Filters.Add<ConflictExceptionFilter>();
        options.ModelBinderProviders.Insert(0, new SchemeStatusModelBinderProvider());
    })
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow)
    .ConfigureApiBehaviorOptions(options =>
        options.InvalidModelStateResponseFactory = InvalidRequestResponse.Create);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();

builder.Services.AddCors();
builder.Services.AddOptions<CorsOptions>()
    .Configure<IConfiguration, IHostEnvironment>((options, configuration, environment) =>
        options.AddDefaultPolicy(policy => policy
            .WithOrigins(AllowedOrigins.Parse(configuration[AllowedOrigins.ConfigurationKey], environment.IsDevelopment()))
            .WithMethods("GET", "POST", "PUT")
            .AllowAnyHeader()));

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IValidator<ISchemeRequest>, SchemeRequestValidator>();
builder.Services.AddSingleton<IValidator<SchemeListQuery>, SchemeListQueryValidator>();

// MongoDB: fail at startup if the connection settings are missing.
builder.Services.AddOptions<MongoOptions>()
    .Bind(builder.Configuration.GetSection(MongoOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddSingleton<IMongoClient>(services =>
    new MongoClient(services.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));

builder.Services.AddSingleton<IMongoCollection<Scheme>>(services =>
{
    var options = services.GetRequiredService<IOptions<MongoOptions>>().Value;
    return services.GetRequiredService<IMongoClient>()
        .GetDatabase(options.Database)
        .GetCollection<Scheme>("schemes");
});

builder.Services.AddHostedService<SchemeIndexInitializer>();

builder.Services.AddScoped<ISchemeRepository, SchemeRepository>();
builder.Services.AddScoped<ISchemeService, SchemeService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    // Generic 500 with a trace id. Development keeps the detailed error page.
    app.UseExceptionHandler();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors();

// Render handles HTTPS before requests reach the app, so only redirect in Development.
if (app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

app.Run();

// Lets the test project host the API with WebApplicationFactory<Program>.
public partial class Program;
