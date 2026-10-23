using System.Globalization;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class NotificacaoOrcamentoEmailService : INotificacaoOrcamentoEmailService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<NotificacaoOrcamentoEmailService> _logger;

        public NotificacaoOrcamentoEmailService(IServiceScopeFactory scopeFactory, ILogger<NotificacaoOrcamentoEmailService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        public void Notificar(int orcamentoId, EventoEmailOrcamento evento, int atorId, DetalheNotificacaoOrcamento? detalhe = null)
        {
            // Escopo de DI novo: a requisição não espera o envio terminar (mesmo desenho das notificações de ticket).
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var orcamentoService = scope.ServiceProvider.GetRequiredService<IOrcamentoService>();
                    var usuarioService = scope.ServiceProvider.GetRequiredService<IUsuarioService>();
                    var empresaService = scope.ServiceProvider.GetRequiredService<IEmpresaService>();
                    var emailSender = scope.ServiceProvider.GetRequiredService<IEmailSender>();

                    var orcamento = orcamentoService.Obter(orcamentoId);
                    var usuarios = usuarioService.Listar();
                    var ator = usuarios.FirstOrDefault(u => u.Id == atorId);
                    if (orcamento is null || ator is null)
                    {
                        return;
                    }

                    var destinatarios = ResolverDestinatarios(evento, ator, orcamento.EmpresaId, usuarios);
                    if (destinatarios.Count == 0)
                    {
                        return;
                    }

                    var empresa = empresaService.Obter(orcamento.EmpresaId)?.Nome ?? "-";
                    var versao = detalhe?.VersaoNumero is int numero
                        ? orcamentoService.ListarVersoes(orcamentoId).FirstOrDefault(v => v.Numero == numero)
                        : null;

                    var assunto = $"Orçamento #{orcamento.Id} - {RotuloAssunto(evento)}: {orcamento.Titulo}";
                    var corpo = MontarCorpo(evento, orcamento, empresa, ator.Nome, versao, detalhe?.TicketId);

                    foreach (var destinatario in destinatarios)
                    {
                        try
                        {
                            _ = await emailSender.EnviarAsync(destinatario.Email!, assunto, corpo);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Falha ao enviar notificação do orçamento {OrcamentoId} para o usuário {UsuarioId}.", orcamentoId, destinatario.Id);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Falha ao enviar notificação por e-mail do orçamento {OrcamentoId}.", orcamentoId);
                }
            });
        }

        // Eventos de Gerente avisam os Masters; a cotação avisa os Gerentes da empresa; pergunta e resposta
        // avisam o lado oposto ao do autor. Nunca o próprio autor, o usuário de sistema ou quem não tem e-mail.
        // Em memória (poucos usuários), sem Contains sobre coleção.
        internal static List<Usuario> ResolverDestinatarios(EventoEmailOrcamento evento, Usuario ator, int empresaDoOrcamentoId, IEnumerable<Usuario> usuarios)
        {
            var avisaMasters = evento switch
            {
                EventoEmailOrcamento.Cotado => false,
                EventoEmailOrcamento.Pergunta or EventoEmailOrcamento.Resposta => ator.Tipo == TipoUsuario.Gerente,
                _ => true
            };

            return usuarios
                .Where(u => u.Id != ator.Id && !u.Sistema && !string.IsNullOrWhiteSpace(u.Email))
                .Where(u => avisaMasters
                    ? u.Tipo == TipoUsuario.Master
                    : u.Tipo == TipoUsuario.Gerente && u.EmpresaId == empresaDoOrcamentoId)
                .ToList();
        }

        private static string RotuloAssunto(EventoEmailOrcamento evento)
        {
            return evento switch
            {
                EventoEmailOrcamento.Enviado => "enviado ao Master",
                EventoEmailOrcamento.Cotado => "nova cotação",
                EventoEmailOrcamento.AjusteSolicitado => "ajuste solicitado",
                EventoEmailOrcamento.Aprovado => "aprovado",
                EventoEmailOrcamento.Recusado => "recusado",
                EventoEmailOrcamento.Cancelado => "cancelado",
                EventoEmailOrcamento.Pergunta => "nova pergunta",
                _ => "nova resposta"
            };
        }

        // Textos livres (motivo, comentário, pergunta, resposta) propositalmente não entram no corpo, como nas
        // notificações de ticket: o e-mail só informa que existem.
        private static string MontarCorpo(EventoEmailOrcamento evento, Orcamento orcamento, string empresa, string nomeAtor, VersaoOrcamento? versao, int? ticketId)
        {
            var identificacao = $"orçamento #{orcamento.Id} - {orcamento.Titulo} ({empresa})";
            var linhas = new List<string>();

            switch (evento)
            {
                case EventoEmailOrcamento.Enviado:
                    linhas.Add($"{nomeAtor} enviou o {identificacao} para cotação.");
                    break;
                case EventoEmailOrcamento.Cotado:
                    linhas.Add($"{nomeAtor} registrou a cotação{(versao is null ? string.Empty : $" V{versao.Numero}")} do {identificacao}.");
                    if (versao is not null)
                    {
                        linhas.Add($"Valor: {versao.Valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"))}");
                        linhas.Add($"Prazo: {versao.PrazoDias} dia(s)");
                        linhas.Add($"Validade: {versao.Validade:dd/MM/yyyy}");
                    }

                    break;
                case EventoEmailOrcamento.AjusteSolicitado:
                    linhas.Add($"{nomeAtor} solicitou ajuste na cotação do {identificacao}.");
                    linhas.Add("O motivo está registrado no histórico do orçamento.");
                    break;
                case EventoEmailOrcamento.Aprovado:
                    linhas.Add($"{nomeAtor} aprovou o {identificacao}.");
                    if (ticketId.HasValue)
                    {
                        linhas.Add($"Ticket gerado: #{ticketId}");
                    }

                    break;
                case EventoEmailOrcamento.Recusado:
                    linhas.Add($"{nomeAtor} recusou o {identificacao}.");
                    linhas.Add("O motivo está registrado no histórico do orçamento.");
                    break;
                case EventoEmailOrcamento.Cancelado:
                    linhas.Add($"{nomeAtor} cancelou o {identificacao}.");
                    break;
                case EventoEmailOrcamento.Pergunta:
                    linhas.Add($"{nomeAtor} fez uma nova pergunta no {identificacao}.");
                    break;
                default:
                    linhas.Add($"{nomeAtor} respondeu a uma pergunta no {identificacao}.");
                    break;
            }

            return string.Join(Environment.NewLine, linhas);
        }
    }
}
