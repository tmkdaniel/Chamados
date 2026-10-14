using TmkChamados.Models;

namespace TmkChamados.Services
{
    public interface IAndamentoService
    {
        Andamento Criar(int ticketId, string texto, int criadoPorId, byte[]? anexoConteudo = null, string? anexoNomeArquivo = null, string? anexoContentType = null);

        IReadOnlyList<Andamento> ListarPorTicket(int ticketId);

        IReadOnlyDictionary<int, string> ObterNomesAnexosPorTicket(int ticketId);

        bool ExisteAndamentoDe(int usuarioId);

        Andamento? Obter(int id);

        AnexoAndamento? ObterAnexo(int andamentoId);
    }
}
