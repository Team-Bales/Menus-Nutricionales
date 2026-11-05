using Menus.Api.Actor;
using Menus.Api.Errores;
using Menus.Application;
using Menus.Application.Comun;
using Menus.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Capa de aplicación (ADR-010): controladores, errores como ProblemDetails y quién actúa.
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeExcepciones>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorActual, ActorDesdeCabecera>();

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

app.UseExceptionHandler();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
