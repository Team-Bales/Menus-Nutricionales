using Menus.Api.Actor;
using Menus.Api.Cors;
using Menus.Api.Errores;
using Menus.Application;
using Menus.Application.Comun;
using Menus.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Capa de aplicación (ADR-010): controladores, errores como ProblemDetails y quién actúa.
// Sin el [Required] implícito de los tipos no anulables, un campo vacío o ausente llega a FluentValidation y
// responde 422 con el formato del ADR-010 (§3), en lugar de un 400 de MVC con otro formato.
builder.Services.AddControllers(o => o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true);
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ManejadorDeExcepciones>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IActorActual, ActorDesdeCabecera>();
builder.Services.AddCorsDelCliente(builder.Configuration);

// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

var app = builder.Build();

// Antes que todo lo demás: el navegador tiene que poder leer también los errores (404, 409, 422), y la comprobación
// previa (OPTIONS) no puede llegar a la redirección a HTTPS.
app.UseCors(CorsDelCliente.Politica);

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
