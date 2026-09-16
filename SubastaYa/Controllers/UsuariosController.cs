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
        Expires = DateTime.UtcNow.AddHours(2), // El token dura 2 horas
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

[Authorize]
[HttpGet("billetera")]
public async Task<IActionResult> ObtenerMiBilletera()
{
    var idReclamado = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    if (!int.TryParse(idReclamado, out int usuarioId)) 
        return Unauthorized("Token inválido.");

    var usuario = await _context.Usuarios
        .Include(u => u.Billetera)
        .FirstOrDefaultAsync(u => u.Id == usuarioId);
        
    if (usuario == null || usuario.Billetera == null) 
        return NotFound("Billetera no encontrada.");

    return Ok(new {
        saldoDisponible = usuario.Billetera.SaldoDisponible,
        saldoRetenido = usuario.Billetera.SaldoRetenido
    });
}

[Authorize] // Exigimos el Token
[HttpPost("depositar")] // La URL ahora será simplemente /api/usuarios/depositar
public async Task<IActionResult> Depositar([FromBody] DepositoRequest peticion)
{
    // 1. Extraemos el ID del dueño del token
    var idReclamado = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    
    if (!int.TryParse(idReclamado, out int usuarioIdSeguro))
        return Unauthorized("Token inválido o corrupto.");

    if (peticion.Monto <= 0)
        return BadRequest("El monto a depositar debe ser mayor a cero.");

    // 2. Buscamos al usuario y su billetera usando el ID seguro
    var usuario = await _context.Usuarios
        .Include(u => u.Billetera)
        .FirstOrDefaultAsync(u => u.Id == usuarioIdSeguro);

    if (usuario == null || usuario.Billetera == null)
        return NotFound("Usuario o billetera no encontrados.");

    // 3. Sumamos el dinero
    usuario.Billetera.SaldoDisponible += peticion.Monto;
    usuario.Billetera.SaldoTotal += peticion.Monto;

    await _context.SaveChangesAsync();

    return Ok(new 
    { 
        Mensaje = "Depósito exitoso", 
        NuevoSaldo = usuario.Billetera.SaldoDisponible 
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

