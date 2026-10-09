using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IAndamentoService
    {
        Andamento Criar(int ticketId, string texto, int criadoPorId);

        IReadOnlyList<Andamento> ListarPorTicket(int ticketId);
    }
}
