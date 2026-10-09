using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class NotificacaoTicketEmailService : INotificacaoTicketEmailService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacaoTicketEmailService> _logger;

        public NotificacaoTicketEmailService(IServiceScopeFactory scopeFactory, ILogger<NotificacaoTicketEmailService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void NotificarAlteracao(int ticketId, int criadoPorId, TicketSnapshotNotificacao anterior, TicketSnapshotNotificacao novo, int usuarioQueAlterouId)
        {
            var alteracoes = CalcularAlteracoes(anterior, novo);
            if (alteracoes.Count == 0)
            {
                return;
            }

            var destinatarioIds = new List<int> { criadoPorId };
            if (novo.ResponsavelId.HasValue && novo.ResponsavelId.Value != criadoPorId)
            {
                destinatarioIds.Add(novo.ResponsavelId.Value);
            }

            var assunto = $"Ticket #{ticketId} atualizado: {novo.Titulo}";
            var corpo = MontarCorpo(ticketId, novo.Titulo, alteracoes);

            // Disparado fora do escopo de DI da requisição (ver design.md - Decisão 2 e Risco
            // correspondente): a requisição original não espera o envio terminar, então é preciso
            // um escopo novo para resolver serviços scoped (DbContext/IUsuarioService) com segurança.
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    foreach (var destinatarioId in destinatarioIds)
                    {
                        if (destinatarioId == usuarioQueAlterouId)
                        {
                            continue;
                        }

                        var destinatario = usuarioService.Obter(destinatarioId);
                        if (destinatario is null || string.IsNullOrWhiteSpace(destinatario.Email))
                        {
                            continue;
                        }

                        await emailSender.EnviarAsync(destinatario.Email!, assunto, corpo);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao enviar notificação por e-mail do ticket {TicketId}.", ticketId);
                }
            });
        }

        private static List<string> CalcularAlteracoes(TicketSnapshotNotificacao anterior, TicketSnapshotNotificacao novo)
        {
            var alteracoes = new List<string>();

            if (anterior.Classificacao != novo.Classificacao)
            {
                alteracoes.Add($"Classificação: \"{ClassificacaoTicketInfo.ObterLabel(anterior.Classificacao)}\" -> \"{ClassificacaoTicketInfo.ObterLabel(novo.Classificacao)}\"");
            }

            if (anterior.Titulo != novo.Titulo)
            {
                alteracoes.Add($"Título: \"{anterior.Titulo}\" -> \"{novo.Titulo}\"");
            }

            if (anterior.Descricao != novo.Descricao)
            {
                alteracoes.Add("Descrição foi alterada");
            }

            if (anterior.Status != novo.Status)
            {
                alteracoes.Add($"Status: \"{anterior.Status}\" -> \"{novo.Status}\"");
            }

            if (anterior.Prioridade != novo.Prioridade)
            {
                alteracoes.Add($"Prioridade: \"{anterior.Prioridade}\" -> \"{novo.Prioridade}\"");
            }

            if (anterior.DataPrazo != novo.DataPrazo)
            {
                alteracoes.Add($"Data de Prazo: \"{FormatarData(anterior.DataPrazo)}\" -> \"{FormatarData(novo.DataPrazo)}\"");
            }

            if (anterior.ResponsavelId != novo.ResponsavelId)
            {
                alteracoes.Add("Responsável foi alterado");
            }

            return alteracoes;
        }

        private static string FormatarData(DateTime? data)
        {
            return data.HasValue ? data.Value.ToString("dd/MM/yyyy") : "(sem prazo)";
        }

        private static string MontarCorpo(int ticketId, string titulo, List<string> alteracoes)
        {
            var linhas = new List<string>
            {
                $"O ticket #{ticketId} - {titulo} foi atualizado.",
                string.Empty,
                "Alterações:"
            };
            linhas.AddRange(alteracoes.Select(a => $"- {a}"));
            return string.Join(Environment.NewLine, linhas);
        }
    }
}
