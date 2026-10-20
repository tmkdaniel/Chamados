using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class EmailTicketImportService : IEmailTicketImportService
    {
        private readonly ImportacaoEmailSettings _settings;
        private readonly ITicketService _ticketService;
        private readonly IUsuarioService _usuarioService;
        private readonly IAndamentoService _andamentoService;
        private readonly ILogger<EmailTicketImportService> _logger;

        public EmailTicketImportService(
            IOptions<ImportacaoEmailSettings> settings,
            ITicketService ticketService,
            IUsuarioService usuarioService,
            IAndamentoService andamentoService,
            ILogger<EmailTicketImportService> logger)
        {
            _settings = settings.Value;
            _ticketService = ticketService;
            _usuarioService = usuarioService;
            _andamentoService = andamentoService;
            _logger = logger;
        }

        public async Task ImportarAsync(CancellationToken cancellationToken)
        {
            using var client = new ImapClient();

            var opcoesSeguranca = _settings.EnableSsl ? SecureSocketOptions.Auto : SecureSocketOptions.None;
            await client.ConnectAsync(_settings.Host, _settings.Port, opcoesSeguranca, cancellationToken);

            if (!string.IsNullOrEmpty(_settings.Username))
            {
                await client.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty, cancellationToken);
            }

            var caixaEntrada = client.Inbox;
            await caixaEntrada.OpenAsync(FolderAccess.ReadWrite, cancellationToken);

            var uids = await caixaEntrada.SearchAsync(SearchQuery.NotSeen, cancellationToken);

            foreach (var uid in uids)
            {
                try
                {
                    var mensagem = await caixaEntrada.GetMessageAsync(uid, cancellationToken);
                    ProcessarMensagem(mensagem);
                }
                catch (Exception ex)
                {
                    // Um e-mail malformado/ilegível não trava os demais nem é retentado para
                    // sempre: é marcado como lido mesmo assim (ver design.md - Decisão 8).
                    _logger.LogError(ex, "Falha ao processar e-mail (uid {Uid}) na importação de tickets por e-mail.", uid);
                }
                finally
                {
                    await caixaEntrada.AddFlagsAsync(uid, MessageFlags.Seen, true, cancellationToken);
                }
            }

            await client.DisconnectAsync(true, cancellationToken);
        }

        private void ProcessarMensagem(MimeMessage mensagem)
        {
            var messageId = mensagem.MessageId;

            if (!string.IsNullOrEmpty(messageId) && _ticketService.ObterPorMessageId(messageId) is not null)
            {
                return;
            }

            var remetente = mensagem.From.Mailboxes.FirstOrDefault()?.Address;
            var criadoPorId = ResolverCriadoPorId(remetente);

            var titulo = mensagem.Subject ?? string.Empty;
            var descricao = ObterCorpoTextoPuro(mensagem);

            var ticket = _ticketService.Criar(ClassificacaoTicket.SemClassificacao, titulo, descricao, PrioridadeTicket.Normal, null, criadoPorId, messageId);

            foreach (var anexo in mensagem.Attachments.OfType<MimePart>())
            {
                AdicionarAnexoComoAndamento(ticket.Id, criadoPorId, anexo);
            }
        }

        private int ResolverCriadoPorId(string? remetente)
        {
            var usuarios = _usuarioService.Listar();

            if (!string.IsNullOrEmpty(remetente))
            {
                var correspondente = usuarios.FirstOrDefault(u =>
                    !string.IsNullOrEmpty(u.Email) && string.Equals(u.Email, remetente, StringComparison.OrdinalIgnoreCase));
                if (correspondente is not null)
                {
                    return correspondente.Id;
                }
            }

            var usuarioSistema = usuarios.First(u => u.Sistema);
            return usuarioSistema.Id;
        }

        private static string ObterCorpoTextoPuro(MimeMessage mensagem)
        {
            if (!string.IsNullOrEmpty(mensagem.TextBody))
            {
                return mensagem.TextBody;
            }

            if (!string.IsNullOrEmpty(mensagem.HtmlBody))
            {
                return ExtrairTextoDeHtml(mensagem.HtmlBody);
            }

            return string.Empty;
        }

        private static string ExtrairTextoDeHtml(string html)
        {
            var semTags = Regex.Replace(html, "<[^>]+>", " ");
            var decodificado = System.Net.WebUtility.HtmlDecode(semTags);
            return Regex.Replace(decodificado, @"\s+", " ").Trim();
        }

        private void AdicionarAnexoComoAndamento(int ticketId, int criadoPorId, MimePart anexo)
        {
            var nomeArquivo = anexo.FileName ?? string.Empty;
            var extensao = Path.GetExtension(nomeArquivo);

            if (string.IsNullOrEmpty(extensao) || !AnexoAndamentoRegras.ExtensoesAceitas.Contains(extensao) || anexo.Content is null)
            {
                return;
            }

            using var stream = new MemoryStream();
            anexo.Content.DecodeTo(stream);
            var conteudo = stream.ToArray();

            if (conteudo.LongLength > AnexoAndamentoRegras.TamanhoMaximoBytes)
            {
                return;
            }

            var contentType = anexo.ContentType?.MimeType ?? "application/octet-stream";
            _andamentoService.Criar(ticketId, "Anexo recebido por e-mail.", criadoPorId, conteudo, nomeArquivo, contentType);
        }
    }
}
