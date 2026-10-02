using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IEmpresaService
    {
        IReadOnlyList<Empresa> Listar();

        Empresa? Obter(int id);

        Empresa Criar(string nome);

        bool Atualizar(int id, string nome);

        bool Excluir(int id);
    }
}
