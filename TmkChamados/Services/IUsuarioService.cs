using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IUsuarioService
    {
        IReadOnlyList<Usuario> Listar();

        Usuario? Obter(int id);

        bool NomeEmUso(string nome, int? ignorarId = null);

        Usuario Criar(string nome, string senha, TipoUsuario tipo, int empresaId, string? email = null);

        bool Atualizar(int id, string nome, string? senha, TipoUsuario tipo, int empresaId, string? email = null);

        bool Excluir(int id);

        Usuario? ValidarCredenciais(string nome, string senha);
    }
}
