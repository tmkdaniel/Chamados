namespace TmkChamados.Services
{
    public interface IEmailSender
    {
        Task<bool> EnviarAsync(string destinatario, string assunto, string corpo);
    }
}
