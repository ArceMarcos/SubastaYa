using Microsoft.EntityFrameworkCore;
using SubastaYa.Models;

namespace SubastaYa.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Billetera> Billeteras { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración estricta de la relación 1 a 1 entre Usuario y Billetera
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Billetera)
            .WithOne(b => b.Propietario)
            .HasForeignKey<Billetera>(b => b.UsuarioId);
    }
}