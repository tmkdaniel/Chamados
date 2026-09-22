using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IChamadoService
    {
        IReadOnlyList<Chamado> Listar();

        Chamado? Obter(int id);

        Chamado Criar(string titulo, string descricao, StatusChamado status);

        bool Atualizar(int id, string titulo, string descricao, StatusChamado status);

        bool Excluir(int id);
    }
}
