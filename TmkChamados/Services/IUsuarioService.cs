using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IUsuarioService
    {
        IReadOnlyList<Usuario> Listar();

        Usuario? Obter(int id);

        Usuario Criar(string nome);

        bool Atualizar(int id, string nome);

        bool Excluir(int id);
    }
}
