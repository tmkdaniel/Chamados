namespace TmkChamados.Services
{
    public interface INotificacaoTicketEmailService
    {
        void NotificarAlteracao(int ticketId, int criadoPorId, TicketSnapshotNotificacao anterior, TicketSnapshotNotificacao novo, int usuarioQueAlterouId);

        void NotificarNovoAndamento(int ticketId, string ticketTitulo, int criadoPorId, int? responsavelId, int autorAndamentoId, bool temAnexo);
    }
}
