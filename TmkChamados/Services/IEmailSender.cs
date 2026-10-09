namespace TmkChamados.Services
{
    public interface IEmailSender
    {
        Task EnviarAsync(string destinatario, string assunto, string corpo);
    }
}
