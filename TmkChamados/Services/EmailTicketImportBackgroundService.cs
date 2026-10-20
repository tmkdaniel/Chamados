using Microsoft.Extensions.Options;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class EmailTicketImportBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IOptions<ImportacaoEmailSettings> _settings;
        private readonly ILogger<EmailTicketImportBackgroundService> _logger;

        public EmailTicketImportBackgroundService(
            IServiceScopeFactory scopeFactory,
            IOptions<ImportacaoEmailSettings> settings,
            ILogger<EmailTicketImportBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var intervalo = TimeSpan.FromMinutes(Math.Max(1, _settings.Value.IntervaloMinutos));

                if (string.IsNullOrWhiteSpace(_settings.Value.Host))
                {
                    // Caixa de e-mail não configurada: a funcionalidade fica inativa sem erro
                    // (ver design.md - Decisão 2), permitindo ambientes que ainda não a
                    // configuraram (ex.: desenvolvimento local) continuarem funcionando normalmente.
                    await Task.Delay(intervalo, stoppingToken);
                    continue;
                }

                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var importService = scope.ServiceProvider.GetRequiredService<IEmailTicketImportService>();
                    await importService.ImportarAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Falha no ciclo de importação de tickets por e-mail.");
                }

                await Task.Delay(intervalo, stoppingToken);
            }
        }
    }
}
