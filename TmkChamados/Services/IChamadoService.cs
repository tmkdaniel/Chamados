using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IChamadoService
    {
        IReadOnlyList<Chamado> Listar();

        IReadOnlyList<Chamado> Listar(FiltroChamados filtro, TipoUsuario tipoUsuario, int usuarioId, int empresaId);

        Chamado? Obter(int id);

        bool PodeAcessar(Chamado chamado, TipoUsuario tipoUsuario, int usuarioId, int empresaId);

        Chamado Criar(string titulo, string descricao, PrioridadeChamado prioridade, DateTime? dataPrazo, int criadoPorId);

        bool Atualizar(int id, string titulo, string descricao, StatusChamado status, PrioridadeChamado prioridade, DateTime? dataPrazo, int criadoPorId, int? responsavelId);

        bool Excluir(int id);
    }
}
