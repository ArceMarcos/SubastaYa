using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SubastaYa.Data;
using SubastaYa.Models;
using Microsoft.AspNetCore.Authorization;

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

    [HttpGet]
public async Task<IActionResult> ObtenerSubastasActivas()
{
    var subastas = await _context.Subastas
        .Include(s => s.Articulo)
        .Where(s => s.Activa && s.FechaFin > DateTime.UtcNow)
        .Select(s => new {
            s.Id,
            s.Articulo.Nombre,
            s.Articulo.Descripcion,
            s.PrecioActual,
            s.FechaFin
        })
        .ToListAsync();

    return Ok(subastas);
}

[Authorize] // Exige Token JWT
[HttpPost]
public async Task<IActionResult> CrearSubasta([FromBody] CrearSubastaRequest peticion)
{
    // 1. Extraemos el ID del usuario desde el Token
    var idReclamado = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    if (!int.TryParse(idReclamado, out int vendedorIdSeguro))
        return Unauthorized("Token inválido o corrupto.");

    // 2. Buscamos al vendedor para validar que exista
    var vendedor = await _context.Usuarios.FindAsync(vendedorIdSeguro);
    if (vendedor == null)
        return NotFound("Vendedor no encontrado.");

    // 3. Creamos el artículo
    var nuevoArticulo = new Articulo
    {
        Nombre = peticion.NombreArticulo,
        Descripcion = peticion.DescripcionArticulo
    };
    _context.Articulos.Add(nuevoArticulo);
    await _context.SaveChangesAsync();

    // 4. Creamos la subasta (usando el ID seguro del token)
    var nuevaSubasta = new Subasta
    {
        VendedorId = vendedorIdSeguro,
        ArticuloId = nuevoArticulo.Id,
        PrecioBase = peticion.PrecioBase,
        PrecioActual = peticion.PrecioBase,
        FechaInicio = DateTime.UtcNow,
        FechaFin = peticion.FechaFin,
        Activa = true
    };

    _context.Subastas.Add(nuevaSubasta);
    await _context.SaveChangesAsync();

    return CreatedAtAction(nameof(CrearSubasta), new { id = nuevaSubasta.Id }, nuevaSubasta);
}

    [Authorize]
    [HttpPost("{id}/pujar")]
    public async Task<IActionResult> RealizarPuja(int id, [FromBody] PujaRequest peticion)
    {
        // 1. Extraemos el ID del usuario de forma segura desde el Token JWT
        var idReclamado = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        
        if (!int.TryParse(idReclamado, out int ofertanteIdSeguro))
            return Unauthorized("Token inválido o corrupto.");

        // 2. Validar que la subasta exista y esté activa
        var subasta = await _context.Subastas.FirstOrDefaultAsync(s => s.Id == id);
        if (subasta == null || !subasta.Activa || subasta.FechaFin < DateTime.UtcNow)
            return BadRequest("La subasta no está activa o ya finalizó.");

        // 3. Validar que la oferta supere el precio actual
        if (peticion.Monto <= subasta.PrecioActual)
            return BadRequest($"La oferta debe ser mayor al precio actual (${subasta.PrecioActual}).");

        // 4. Buscar al usuario y su billetera usando el ID SEGURO del Token
        var ofertante = await _context.Usuarios
            .Include(u => u.Billetera)
            .FirstOrDefaultAsync(u => u.Id == ofertanteIdSeguro); 

        if (ofertante == null || ofertante.Billetera == null)
            return NotFound("Usuario no encontrado.");

        // 5. Validar que tenga saldo suficiente
        if (ofertante.Billetera.SaldoDisponible < peticion.Monto)
            return BadRequest("Saldo disponible insuficiente para realizar esta oferta.");

        // 6. Buscar la puja más alta anterior (si existe) para devolverle el dinero
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

        // 7. Retener el dinero del NUEVO ganador
        ofertante.Billetera.SaldoDisponible -= peticion.Monto;
        ofertante.Billetera.SaldoRetenido += peticion.Monto;

        // 8. Actualizar el precio de la subasta y crear el registro de la puja
        subasta.PrecioActual = peticion.Monto;

        var nuevaPuja = new Puja
        {
            SubastaId = id,
            OfertanteId = ofertanteIdSeguro, // Usamos el ID seguro del token
            Monto = peticion.Monto,
            FechaHora = DateTime.UtcNow
        };

        _context.Pujas.Add(nuevaPuja);

        // 9. Guardar TODOS los cambios juntos
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
    public string NombreArticulo { get; set; } = string.Empty;
    public string DescripcionArticulo { get; set; } = string.Empty;
    public decimal PrecioBase { get; set; }
    public DateTime FechaFin { get; set; }
}

public class PujaRequest
{
            public decimal Monto { get; set; }
}