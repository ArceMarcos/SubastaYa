using Microsoft.AspNetCore.Mvc;
using SubastaYa.Data;
using SubastaYa.Models;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Authorization;
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

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest peticion)
    {
    // 1. Buscamos al usuario por correo y contraseña (sin encriptar por ahora para simplificar el TP)
    var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => 
        u.CorreoElectronico == peticion.CorreoElectronico && 
        u.ContrasenaHash == peticion.Contrasena);

    if (usuario == null) 
        return Unauthorized("Credenciales incorrectas.");

    // 2. Si es válido, preparamos la información que irá dentro del token (el ID del usuario)
    var tokenHandler = new JwtSecurityTokenHandler();
    var key = Encoding.UTF8.GetBytes("SubastaYa_ClaveSuperSecreta_IngenieriaSoftware_2026_UNAJ");
    
    var tokenDescriptor = new SecurityTokenDescriptor
    {
        Subject = new ClaimsIdentity(new[] 
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id.ToString()),
            new Claim(ClaimTypes.Email, usuario.CorreoElectronico)
        }),
        Expires = DateTime.UtcNow.AddHours(2), // El token durará 2 horas
        SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
    };

    // 3. Generamos el token y se lo mandamos
    var token = tokenHandler.CreateToken(tokenDescriptor);
    return Ok(new { Token = tokenHandler.WriteToken(token) });
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

public class LoginRequest
{
    public string CorreoElectronico { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}