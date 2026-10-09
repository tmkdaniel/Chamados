using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IOptions<EmailSettings> settings, ILogger<SmtpEmailSender> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task EnviarAsync(string destinatario, string assunto, string corpo)
        {
            try
            {
                var mensagem = new MimeMessage();
                mensagem.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
                mensagem.To.Add(MailboxAddress.Parse(destinatario));
                mensagem.Subject = assunto;
                mensagem.Body = new TextPart("plain") { Text = corpo };

                var opcoesSeguranca = _settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None;

                using var cliente = new SmtpClient();
                await cliente.ConnectAsync(_settings.Host, _settings.Port, opcoesSeguranca);
                if (!string.IsNullOrEmpty(_settings.Username))
                {
                    await cliente.AuthenticateAsync(_settings.Username, _settings.Password ?? string.Empty);
                }

                await cliente.SendAsync(mensagem);
                await cliente.DisconnectAsync(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha ao enviar e-mail de notificação para {Destinatario}.", destinatario);
            }
        }
    }
}
