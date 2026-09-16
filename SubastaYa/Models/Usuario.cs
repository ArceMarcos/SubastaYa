namespace SubastaYa.Models;

public class Usuario
{
    public int Id { get; set; }
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string ContrasenaHash { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; } = DateTime.UtcNow;

    // Relación de navegación
    public Billetera? Billetera { get; set; }
}