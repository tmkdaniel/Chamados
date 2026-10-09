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

        public Andamento Criar(int ticketId, string texto, int criadoPorId)
        {
            var andamento = new Andamento
            {
                TicketId = ticketId,
                Texto = texto,
                DataCriacao = DateTime.Now,
                CriadoPorId = criadoPorId
            };

            _dbContext.Andamentos.Add(andamento);
            _dbContext.SaveChanges();
            return andamento;
        }

        public IReadOnlyList<Andamento> ListarPorTicket(int ticketId)
        {
            return _dbContext.Andamentos
                .Where(a => a.TicketId == ticketId)
                .OrderBy(a => a.DataCriacao)
                .ToList();
        }
    }
}
