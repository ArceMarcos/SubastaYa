namespace SubastaYa.Models;

public class Puja
{
    public int Id { get; set; }
    
    public int SubastaId { get; set; }
    public Subasta? Subasta { get; set; }

    // Relación con el usuario que hace la oferta
    public int OfertanteId { get; set; }
    public Usuario? Ofertante { get; set; }

    public decimal Monto { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.UtcNow;
}