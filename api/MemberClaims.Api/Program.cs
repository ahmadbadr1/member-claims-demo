// Member Claims Demo
// Copyright (c) 2026 Ahmed Ibrahim (https://www.linkedin.com/in/ahmed-ibrahim-151a21182)
// Licensed under the MIT License. See LICENSE in the repository root.

using System.Text.Json.Serialization;
using MemberClaims.Api.Data;
using MemberClaims.Api.Errors;
using MemberClaims.Api.GraphQL;
using MemberClaims.Api.Services;
using MemberClaims.Api.Workers;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// ---------- Data ----------
// AddDbContext registers AppDbContext as Scoped: one instance per HTTP request.
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("Default"),
        sql => sql.EnableRetryOnFailure(maxRetryCount: 3)));

// ---------- Application services ----------
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddScoped<IMemberService, MemberService>();
builder.Services.AddHostedService<ClaimAutoReviewWorker>();

// ---------- Web API ----------
builder.Services
    .AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();

builder.Services.AddCors(options =>
    options.AddPolicy("web", policy => policy
        .WithOrigins(builder.Configuration["Web:Origin"] ?? "http://localhost:4200")
        .AllowAnyHeader()
        .AllowAnyMethod()));

// ---------- GraphQL (Hot Chocolate) ----------
builder.Services
    .AddGraphQLServer()
    .RegisterDbContext<AppDbContext>(DbContextKind.Synchronized)
    .AddQueryType<Query>()
    .AddProjections()
    .AddFiltering()
    .AddSorting();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("web");

app.MapControllers();
app.MapGraphQL();          // http://localhost:5080/graphql (includes the Banana Cake Pop / Nitro IDE)
app.MapHealthChecks("/health");

// Demo convenience: create the database and seed sample data on startup.
// In production I'd use EF Core migrations deployed as a reviewed SQL script instead.
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedData.InitializeAsync(db);
}

app.Run();

// Lets integration tests reference the app (WebApplicationFactory<Program>).
public partial class Program;
