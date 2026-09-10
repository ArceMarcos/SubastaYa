using System.ComponentModel.DataAnnotations;

namespace SubastaYa.Models;

public class Billetera
{
    public int Id { get; set; }
    public int UsuarioId { get; set; }
    public Usuario? Propietario { get; set; }

    // Manejo atómico de saldos (Escrow)
    public decimal SaldoTotal { get; set; } = 0;
    public decimal SaldoRetenido { get; set; } = 0;
    public decimal SaldoDisponible { get; set; } = 0;

    // Token para Optimistic Locking requerido por la cátedra
    [Timestamp]
    public uint Version { get; set; } 
}