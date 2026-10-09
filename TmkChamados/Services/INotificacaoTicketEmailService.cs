namespace TmkChamados.Services
{
    public interface INotificacaoTicketEmailService
    {
        void NotificarAlteracao(int ticketId, int criadoPorId, TicketSnapshotNotificacao anterior, TicketSnapshotNotificacao novo, int usuarioQueAlterouId);
    }
}
