using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IChamadoService
    {
        IReadOnlyList<Chamado> Listar();

        IReadOnlyList<Chamado> Listar(FiltroChamados filtro);

        Chamado? Obter(int id);

        Chamado Criar(string titulo, string descricao, StatusChamado status, int criadoPorId, int responsavelId);

        bool Atualizar(int id, string titulo, string descricao, StatusChamado status, int criadoPorId, int responsavelId);

        bool Excluir(int id);
    }
}
