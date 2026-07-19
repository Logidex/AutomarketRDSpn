using AutoMarket.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace AutoMarket.Infrastructure.Data;

// Heredamos de DbContext para obtener todos los poderes de Entity Framework
public class ApplicationDbContext : DbContext
{
    // Este constructor recibe las credenciales de conexión que configuraremos más adelante
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<Anuncio> Anuncios { get; set; }
    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<PerfilDealer> PerfilesDealers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Anuncio>(b =>
        {
            // Definimos el comparador para que EF Core sepa rastrear los cambios de la lista
            var fotosComparer = new ValueComparer<List<string>>(
                (c1, c2) => c1!.SequenceEqual(c2!), // Compara si los elementos son iguales
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())), // Genera el código Hash
                c => c.ToList() // Crea una copia limpia para la snapshot de EF Core
            );

            b.Property<List<string>>("_fotos")
                .HasColumnName("Fotos")
                .HasConversion(
                    v => string.Join(',', v),
                    v => !string.IsNullOrEmpty(v)
                        ? v.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList()
                        : new List<string>()
                )
                .Metadata.SetValueComparer(fotosComparer); // 👈 Le asignamos el comparador aquí
        });

        modelBuilder.Entity<Usuario>(b =>
        {
            b.Property(u => u.Nombre).HasMaxLength(100);
            b.Property(u => u.Apellido).HasMaxLength(100);
            b.Property(u => u.Email).HasMaxLength(150);

            // Email Unico
            b.HasIndex(u => u.Email).IsUnique();

            // -----------------------------------------------------
            // RELACIÓN 1 A MUCHOS: Usuario -> Anuncios
            // -----------------------------------------------------
            b.HasMany(u => u.Anuncios)
             .WithOne(a => a.Usuario)
             .HasForeignKey(a => a.UsuarioId)
             .OnDelete(DeleteBehavior.Cascade);

            b.HasOne(u => u.PerfilDealer)
             .WithOne(u => u.Usuario)
             .HasForeignKey<PerfilDealer>(p => p.UsuarioId);
        });

        modelBuilder.Entity<PerfilDealer>(b =>
        {
            b.HasKey(p => p.UsuarioId);
            b.Property(p => p.NombreAgencia).HasMaxLength(150);
        });
    }

}