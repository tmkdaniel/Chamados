namespace TmkChamados.Services
{
    // Evento de orçamento que gera e-mail. Enum próprio: pergunta e resposta não são eventos do histórico.
    public enum EventoEmailOrcamento
    {
        Enviado,
        Cotado,
        AjusteSolicitado,
        Aprovado,
        Recusado,
        Cancelado,
        Pergunta,
        Resposta
    }

    // VersaoNumero: versão emitida (Cotado). TicketId: ticket gerado (Aprovado).
    public record DetalheNotificacaoOrcamento(int? VersaoNumero = null, int? TicketId = null);

    public interface INotificacaoOrcamentoEmailService
    {
        // Best-effort e assíncrono: nunca lança, nunca bloqueia a ação que o disparou. Deve ser chamado
        // somente depois que a operação foi confirmada.
        void Notificar(int orcamentoId, EventoEmailOrcamento evento, int atorId, DetalheNotificacaoOrcamento? detalhe = null);
    }
}
