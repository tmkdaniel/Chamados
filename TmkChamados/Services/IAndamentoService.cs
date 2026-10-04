using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IAndamentoService
    {
        Andamento Criar(int chamadoId, string texto, int criadoPorId);

        IReadOnlyList<Andamento> ListarPorChamado(int chamadoId);
    }
}
