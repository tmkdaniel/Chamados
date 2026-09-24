using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IUsuarioService
    {
        IReadOnlyList<Usuario> Listar();

        Usuario? Obter(int id);

        bool NomeEmUso(string nome, int? ignorarId = null);

        Usuario Criar(string nome, string senha);

        bool Atualizar(int id, string nome, string? senha);

        bool Excluir(int id);

        Usuario? ValidarCredenciais(string nome, string senha);
    }
}
