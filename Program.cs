using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using SubastaYa.Data;
using SubastaYa.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();
builder.Services.AddHostedService<CierreSubastasService>();

// --- NUEVO: CONFIGURACIÓN JWT ---
var claveSecreta = "SubastaYa_ClaveSuperSecreta_IngenieriaSoftware_2026_UNAJ";
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveSecreta))
        };
    });
// ---------------------------------

var app = builder.Build();

// --- NUEVO: ACTIVAR AUTENTICACIÓN ---
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllers();

app.Run();