using AutoMarket.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace AutoMarket.Infrastructure.Data;

// Heredamos de DbContext para obtener todos los poderes de Entity Framework
public class ApplicationDbContext : DbContext
{
    // Este constructor recibe las credenciales de conexión que configuraremos más adelante
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Anuncio> Anuncios { get; set; }
}