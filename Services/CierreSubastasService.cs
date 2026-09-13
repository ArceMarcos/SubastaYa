using Microsoft.EntityFrameworkCore;
using SubastaYa.Data;

namespace SubastaYa.Services;

public class CierreSubastasService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CierreSubastasService> _logger;

    public CierreSubastasService(IServiceProvider serviceProvider, ILogger<CierreSubastasService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Este bucle se ejecutará infinitamente mientras la API esté encendida
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Ejecutando revisión de subastas vencidas...");
            await ProcesarSubastasVencidas();
            
            // Pausa el proceso por 30 segundos antes de volver a revisar
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }

    private async Task ProcesarSubastasVencidas()
    {
        // Como el servicio corre en segundo plano, necesitamos crear un "Scope" para acceder a la base de datos
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Buscar subastas que estén activas pero cuyo tiempo ya expiró
        var subastasVencidas = await context.Subastas
            .Where(s => s.Activa && s.FechaFin <= DateTime.UtcNow)
            .ToListAsync();

        foreach (var subasta in subastasVencidas)
        {
            _logger.LogInformation($"Cerrando subasta ID: {subasta.Id}");

            // 2. Buscar la oferta ganadora (la más alta)
            var pujaGanadora = await context.Pujas
                .Include(p => p.Ofertante)
                .ThenInclude(u => u.Billetera)
                .Where(p => p.SubastaId == subasta.Id)
                .OrderByDescending(p => p.Monto)
                .FirstOrDefaultAsync();

            if (pujaGanadora != null && pujaGanadora.Ofertante?.Billetera != null)
            {
                // 3. Buscar al vendedor para pagarle
                var vendedor = await context.Usuarios
                    .Include(u => u.Billetera)
                    .FirstOrDefaultAsync(u => u.Id == subasta.VendedorId);

                if (vendedor?.Billetera != null)
                {
                    // 4. Transferencia definitiva de fondos
                    // Le quitamos el dinero retenido definitivamente al comprador
                    pujaGanadora.Ofertante.Billetera.SaldoRetenido -= pujaGanadora.Monto;
                    pujaGanadora.Ofertante.Billetera.SaldoTotal -= pujaGanadora.Monto;
                    
                    // Le ingresamos el dinero como disponible al vendedor
                    vendedor.Billetera.SaldoTotal += pujaGanadora.Monto;
                    vendedor.Billetera.SaldoDisponible += pujaGanadora.Monto;
                    
                    _logger.LogInformation($"Dinero transferido. ${pujaGanadora.Monto} enviados al vendedor ID: {vendedor.Id}");
                }
            }

            // 5. Marcamos la subasta como finalizada para que no se vuelva a procesar
            subasta.Activa = false;
        }

        // 6. Guardar todos los cambios en la base de datos
        if (subastasVencidas.Any())
        {
            await context.SaveChangesAsync();
        }
    }
}