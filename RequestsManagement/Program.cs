
using RequestsManagement.Repositories;
using RequestsManagement.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;
using RequestsManagement.Data;
using RequestsManagement.Services;
using RequestsManagement.Services.Interfaces;
using RequestsManagement.Seeders;

var builder = WebApplication.CreateBuilder(args);

// Controllers and Swagger
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

// Dependency Injection
builder.Services.AddScoped<IRequestRepository, RequestRepository>();
builder.Services.AddScoped<IRequestService, RequestService>();

// OpenAPI
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});


var app = builder.Build();
app.UseCors("AngularClient");

// Swagger - Development only
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

Console.WriteLine(
    "Arguments: " + string.Join(", ", args));


if (app.Environment.IsDevelopment() &&
    args.Contains("--seed"))
{
    using var scope = app.Services.CreateScope();

    var context = scope.ServiceProvider
        .GetRequiredService<AppDbContext>();

    await RequestsManagement.Seeders.RequestDataSeeder.SeedAsync(context);

    Console.WriteLine("Seed completed successfully.");

    return;
}
app.Run();
public partial class Program { }