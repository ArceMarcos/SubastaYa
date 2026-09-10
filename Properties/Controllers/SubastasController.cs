using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Data;
using SubastaYa.Models;

namespace SubastaYa.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SubastasController : ControllerBase
{
    private readonly AppDbContext _context;

    public SubastasController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> CrearSubasta([FromBody] CrearSubastaRequest peticion)
    {
        var vendedor = await _context.Usuarios.FindAsync(peticion.VendedorId);
        if (vendedor == null) 
            return NotFound("Vendedor no encontrado.");

        if (peticion.PrecioBase < 0)
            return BadRequest("El precio base no puede ser negativo.");

        var nuevaSubasta = new Subasta
        {
            Articulo = new Articulo
            {
                Nombre = peticion.NombreArticulo,
                Descripcion = peticion.DescripcionArticulo
            },
            VendedorId = peticion.VendedorId,
            PrecioBase = peticion.PrecioBase,
            PrecioActual = peticion.PrecioBase,
            FechaInicio = DateTime.UtcNow,
            FechaFin = peticion.FechaFin,
            Activa = true
        };

        _context.Subastas.Add(nuevaSubasta);
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            Mensaje = "Subasta publicada con éxito", 
            SubastaId = nuevaSubasta.Id,
            ArticuloId = nuevaSubasta.Articulo.Id
        });
    }

    [HttpPost("{id}/pujar")]
    public async Task<IActionResult> RealizarPuja(int id, [FromBody] PujaRequest peticion)
    {
        // 1. Validar que la subasta exista y esté activa
        var subasta = await _context.Subastas.FirstOrDefaultAsync(s => s.Id == id);
        if (subasta == null || !subasta.Activa || subasta.FechaFin < DateTime.UtcNow)
            return BadRequest("La subasta no está activa o ya finalizó.");

        // 2. Validar que la oferta supere el precio actual
        if (peticion.Monto <= subasta.PrecioActual)
            return BadRequest($"La oferta debe ser mayor al precio actual (${subasta.PrecioActual}).");

        // 3. Buscar al usuario y su billetera
        var ofertante = await _context.Usuarios
            .Include(u => u.Billetera)
            .FirstOrDefaultAsync(u => u.Id == peticion.OfertanteId); 

        if (ofertante == null || ofertante.Billetera == null)
            return NotFound("Usuario no encontrado.");

        // 4. Validar que tenga saldo suficiente
        if (ofertante.Billetera.SaldoDisponible < peticion.Monto)
            return BadRequest("Saldo disponible insuficiente para realizar esta oferta.");

        // 5. Buscar la puja más alta anterior (si existe) para devolverle el dinero
        var pujaAnterior = await _context.Pujas
            .Include(p => p.Ofertante)
            .ThenInclude(u => u.Billetera)
            .Where(p => p.SubastaId == id)
            .OrderByDescending(p => p.Monto)
            .FirstOrDefaultAsync();

        if (pujaAnterior != null && pujaAnterior.Ofertante != null && pujaAnterior.Ofertante.Billetera != null)
        {
            // Le devolvemos el dinero al que acaba de perder el primer lugar
            pujaAnterior.Ofertante.Billetera.SaldoRetenido -= pujaAnterior.Monto;
            pujaAnterior.Ofertante.Billetera.SaldoDisponible += pujaAnterior.Monto;
        }

        // 6. Retener el dinero del NUEVO ganador
        ofertante.Billetera.SaldoDisponible -= peticion.Monto;
        ofertante.Billetera.SaldoRetenido += peticion.Monto;

        // 7. Actualizar el precio de la subasta y crear el registro de la puja
        subasta.PrecioActual = peticion.Monto;

        var nuevaPuja = new Puja
        {
            SubastaId = id,
            OfertanteId = peticion.OfertanteId,
            Monto = peticion.Monto,
            FechaHora = DateTime.UtcNow
        };

        _context.Pujas.Add(nuevaPuja);

        // 8. Guardar TODOS los cambios juntos (Transacción atómica de Entity Framework)
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            Mensaje = "Puja realizada con éxito", 
            NuevoPrecio = subasta.PrecioActual,
            SaldoRetenido = peticion.Monto,
            SaldoDisponible = ofertante.Billetera.SaldoDisponible
        });
    }
}

// DTOs
public class CrearSubastaRequest
{
    public int VendedorId { get; set; }
    public string NombreArticulo { get; set; } = string.Empty;
    public string DescripcionArticulo { get; set; } = string.Empty;
    public decimal PrecioBase { get; set; }
    public DateTime FechaFin { get; set; }
}

public class PujaRequest
{
    public int OfertanteId { get; set; } 
    public decimal Monto { get; set; }
}