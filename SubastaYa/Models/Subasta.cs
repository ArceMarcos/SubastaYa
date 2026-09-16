namespace SubastaYa.Models;

public class Subasta
{
    public int Id { get; set; }
    
    // Relación con el producto
    public int ArticuloId { get; set; }
    public Articulo? Articulo { get; set; }

    // Relación con el usuario que publica la subasta
    public int VendedorId { get; set; }
    public Usuario? Vendedor { get; set; }

    public decimal PrecioBase { get; set; }
    public decimal PrecioActual { get; set; }
    
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public bool Activa { get; set; } = true;
}