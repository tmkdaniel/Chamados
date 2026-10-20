namespace TmkChamados.Services
{
    public interface IEmailTicketImportService
    {
        Task ImportarAsync(CancellationToken cancellationToken);
    }
}
