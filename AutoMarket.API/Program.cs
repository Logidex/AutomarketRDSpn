using AutoMarket.Application.Services;
using AutoMarket.Core.Interfaces;
using AutoMarket.Infrastructure.Repositories;
using AutoMarket.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Le decimos al servidor que active el uso de Controladores
builder.Services.AddControllers();

builder.Services.AddScoped<AnuncioService>();
builder.Services.AddScoped<IAnuncioRepository, AnuncioRepository>();

builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql("Host=localhost;Port=5432;Database=AutoMarketDB;Username=admin;Password=Secreto123!"));

builder.Services.AddOpenApi();

var app = builder.Build();
app.MapOpenApi();
app.MapScalarApiReference();

app.UseStaticFiles();
// 3. Activa el mapeo de las URLs (como /api/anuncios)
app.MapControllers();
// 4. Enciende el servidor y lo deja escuchando
app.Run();