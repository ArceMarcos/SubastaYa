using Microsoft.EntityFrameworkCore;
using SubastaYa.Data;
using SubastaYa.Services;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

builder.Services.AddControllers();

builder.Services.AddHostedService<CierreSubastasService>();

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.Run();