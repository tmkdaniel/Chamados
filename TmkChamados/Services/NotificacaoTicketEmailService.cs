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

            var destinatarioIds = MontarDestinatarios(criadoPorId, novo.ResponsavelId);
            var assunto = $"Ticket #{ticketId} atualizado: {novo.Titulo}";
            var corpo = MontarCorpo(ticketId, novo.Titulo, alteracoes);

            EnviarParaDestinatarios(ticketId, destinatarioIds, usuarioQueAlterouId, assunto, _ => corpo);
        }

        public void NotificarNovoAndamento(int ticketId, string ticketTitulo, int criadoPorId, int? responsavelId, int autorAndamentoId, bool temAnexo)
        {
            var destinatarioIds = MontarDestinatarios(criadoPorId, responsavelId);
            var assunto = $"Ticket #{ticketId} tem um novo andamento: {ticketTitulo}";

            // O nome do autor só é conhecido dentro do escopo de DI criado por EnviarParaDestinatarios
            // (precisa de IUsuarioService), por isso o corpo é montado via callback em vez de já pronto.
            EnviarParaDestinatarios(ticketId, destinatarioIds, autorAndamentoId, assunto, usuarioService =>
            {
                var autor = usuarioService.Obter(autorAndamentoId);
                return MontarCorpoNovoAndamento(ticketId, ticketTitulo, autor?.Nome ?? "um usuário", temAnexo);
            });
        }

        private static List<int> MontarDestinatarios(int criadoPorId, int? responsavelId)
        {
            var destinatarioIds = new List<int> { criadoPorId };
            if (responsavelId.HasValue && responsavelId.Value != criadoPorId)
            {
                destinatarioIds.Add(responsavelId.Value);
            }

            return destinatarioIds;
        }

        // Helper compartilhado por NotificarAlteracao e NotificarNovoAndamento: resolve destinatários
        // (exclui quem agiu, exclui quem não tem e-mail) e envia, fora do escopo de DI da requisição
        // (ver design.md - Decisão 2 e Risco correspondente: a requisição original não espera o envio
        // terminar, então é preciso um escopo novo para resolver serviços scoped com segurança).
        // `corpoFactory` recebe o IUsuarioService do escopo porque NotificarNovoAndamento precisa dele
        // para resolver o nome do autor antes de montar o corpo do e-mail.
        private void EnviarParaDestinatarios(int ticketId, List<int> destinatarioIds, int idQueAgiu, string assunto, Func<IUsuarioService, string> corpoFactory)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();
                    var corpo = corpoFactory(usuarioService);

                    foreach (var destinatarioId in destinatarioIds)
                    {
                        if (destinatarioId == idQueAgiu)
                        {
                            continue;
                        }

                        var destinatario = usuarioService.Obter(destinatarioId);
                        if (destinatario is null || string.IsNullOrWhiteSpace(destinatario.Email))
                        {
                            continue;
                        }

                        _ = await emailSender.EnviarAsync(destinatario.Email!, assunto, corpo);
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

        // O texto do andamento propositalmente não entra no corpo do e-mail (ver design.md -
        // Decisão 4), seguindo o mesmo padrão já usado para "Descrição foi alterada" em
        // CalcularAlteracoes, que também não reproduz conteúdo livre no e-mail.
        private static string MontarCorpoNovoAndamento(int ticketId, string titulo, string nomeAutor, bool temAnexo)
        {
            var linhas = new List<string>
            {
                $"Um novo andamento foi adicionado ao ticket #{ticketId} - {titulo} por {nomeAutor}."
            };

            if (temAnexo)
            {
                linhas.Add("Um anexo foi incluído.");
            }

            return string.Join(Environment.NewLine, linhas);
        }
    }
}
