using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface ITicketService
    {
        IReadOnlyList<Ticket> Listar();

        IReadOnlyList<Ticket> Listar(FiltroTickets filtro, TipoUsuario tipoUsuario, int usuarioId, int empresaId);

        Ticket? Obter(int id);

        bool PodeAcessar(Ticket ticket, TipoUsuario tipoUsuario, int usuarioId, int empresaId);

        Ticket Criar(ClassificacaoTicket classificacao, string titulo, string descricao, PrioridadeTicket prioridade, DateTime? dataPrazo, int criadoPorId);

        bool Atualizar(int id, ClassificacaoTicket classificacao, string titulo, string descricao, StatusTicket status, PrioridadeTicket prioridade, DateTime? dataPrazo, int criadoPorId, int? responsavelId, int usuarioQueAlterouId);

        bool Excluir(int id);
    }
}
