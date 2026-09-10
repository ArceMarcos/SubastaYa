using Microsoft.AspNetCore.Mvc;
using SubastaYa.Data;
using SubastaYa.Models;
using Microsoft.EntityFrameworkCore;

namespace SubastaYa.Controllers;

[Route("api/[controller]")]
[ApiController]
public class UsuariosController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsuariosController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("registro")]
    public async Task<IActionResult> RegistrarUsuario([FromBody] Usuario nuevoUsuario)
    {
        // Regla de negocio: Todo usuario nuevo nace con una billetera en 0
        nuevoUsuario.Billetera = new Billetera 
        { 
            SaldoTotal = 0, 
            SaldoRetenido = 0, 
            SaldoDisponible = 0 
        };
        
        _context.Usuarios.Add(nuevoUsuario);
        await _context.SaveChangesAsync();

        return Ok(new 
        { 
            Mensaje = "Usuario y Billetera creados con éxito", 
            UsuarioId = nuevoUsuario.Id 
        });
    }

    [HttpPost("{id}/depositar")]
public async Task<IActionResult> Depositar(int id, [FromBody] DepositoRequest peticion)
{
    if (peticion.Monto <= 0) 
        return BadRequest("El monto a depositar debe ser mayor a cero.");

    // Buscamos al usuario e incluimos su billetera vinculada en la misma consulta
    var usuario = await _context.Usuarios
        .Include(u => u.Billetera)
        .FirstOrDefaultAsync(u => u.Id == id);

    if (usuario == null || usuario.Billetera == null) 
        return NotFound("Usuario no encontrado.");

    // Acreditamos el dinero
    usuario.Billetera.SaldoTotal += peticion.Monto;
    usuario.Billetera.SaldoDisponible += peticion.Monto;

    await _context.SaveChangesAsync();

    return Ok(new 
    { 
        Mensaje = "Depósito exitoso", 
        SaldoDisponible = usuario.Billetera.SaldoDisponible 
    });
}
}
public class DepositoRequest
{
    public decimal Monto { get; set; }
}