using Microsoft.EntityFrameworkCore;
using SubastaYa.Models;

namespace SubastaYa.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Usuario> Usuarios { get; set; }
    public DbSet<Billetera> Billeteras { get; set; }
    public DbSet<Articulo> Articulos { get; set; }
    public DbSet<Subasta> Subastas { get; set; }
    public DbSet<Puja> Pujas { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Relación 1 a 1: Usuario - Billetera
        modelBuilder.Entity<Usuario>()
            .HasOne(u => u.Billetera)
            .WithOne(b => b.Propietario)
            .HasForeignKey<Billetera>(b => b.UsuarioId);

        // Evitar borrado en cascada para mantener el historial si un usuario se elimina
        modelBuilder.Entity<Subasta>()
            .HasOne(s => s.Vendedor)
            .WithMany()
            .HasForeignKey(s => s.VendedorId)
            .OnDelete(DeleteBehavior.Restrict); 

        modelBuilder.Entity<Puja>()
            .HasOne(p => p.Ofertante)
            .WithMany()
            .HasForeignKey(p => p.OfertanteId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}