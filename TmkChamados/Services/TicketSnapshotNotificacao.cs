using TmkChamados.Models;

namespace TmkChamados.Services
{
    public record TicketSnapshotNotificacao(
        ClassificacaoTicket Classificacao,
        string Titulo,
        string Descricao,
        StatusTicket Status,
        PrioridadeTicket Prioridade,
        DateTime? DataPrazo,
        int? ResponsavelId);
}
