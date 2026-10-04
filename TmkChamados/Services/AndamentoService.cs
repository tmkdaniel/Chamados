using TmkChamados.Data;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class AndamentoService : IAndamentoService
    {
        private readonly TmkChamadosDbContext _dbContext;

        public AndamentoService(TmkChamadosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Andamento Criar(int chamadoId, string texto, int criadoPorId)
        {
            var andamento = new Andamento
            {
                ChamadoId = chamadoId,
                Texto = texto,
                DataCriacao = DateTime.Now,
                CriadoPorId = criadoPorId
            };

            _dbContext.Andamentos.Add(andamento);
            _dbContext.SaveChanges();
            return andamento;
        }

        public IReadOnlyList<Andamento> ListarPorChamado(int chamadoId)
        {
            return _dbContext.Andamentos
                .Where(a => a.ChamadoId == chamadoId)
                .OrderBy(a => a.DataCriacao)
                .ToList();
        }
    }
}
