using TmkChamados.Data;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class AndamentoService : IAndamentoService
    {
        private readonly TmkChamadosDbContext _dbContext;
        private readonly ITicketService _ticketService;
        private readonly INotificacaoTicketEmailService _notificacaoService;

        public AndamentoService(TmkChamadosDbContext dbContext, ITicketService ticketService, INotificacaoTicketEmailService notificacaoService)
        {
            _dbContext = dbContext;
            _ticketService = ticketService;
            _notificacaoService = notificacaoService;
        }

        public Andamento Criar(int ticketId, string texto, int criadoPorId, byte[]? anexoConteudo = null, string? anexoNomeArquivo = null, string? anexoContentType = null)
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

            var temAnexo = anexoConteudo is not null && anexoNomeArquivo is not null && anexoContentType is not null;
            if (temAnexo)
            {
                _dbContext.AnexosAndamento.Add(new AnexoAndamento
                {
                    AndamentoId = andamento.Id,
                    NomeArquivoOriginal = anexoNomeArquivo!,
                    ContentType = anexoContentType!,
                    TamanhoBytes = anexoConteudo!.LongLength,
                    Conteudo = anexoConteudo
                });
                _dbContext.SaveChanges();
            }

            var ticket = _ticketService.Obter(ticketId);
            if (ticket is not null)
            {
                _notificacaoService.NotificarNovoAndamento(ticket.Id, ticket.Titulo, ticket.CriadoPorId, ticket.ResponsavelId, criadoPorId, temAnexo);
            }

            return andamento;
        }

        public IReadOnlyList<Andamento> ListarPorTicket(int ticketId)
        {
            return _dbContext.Andamentos
                .Where(a => a.TicketId == ticketId)
                .OrderBy(a => a.DataCriacao)
                .ToList();
        }

        public IReadOnlyDictionary<int, string> ObterNomesAnexosPorTicket(int ticketId)
        {
            // Projeção explícita: nunca traz a coluna Conteudo (varbinary) só para
            // mostrar o link de download na listagem de Andamentos (ver design.md - Decisão 2).
            // Join direto por TicketId em vez de Contains() sobre uma lista de Ids: EF Core
            // traduziria Contains() sobre parâmetro de lista para OPENJSON(...) WITH (...),
            // que exige SQL Server 2016+/nível de compatibilidade 130+ e falhou no banco real.
            return (from anexo in _dbContext.AnexosAndamento
                    join andamento in _dbContext.Andamentos on anexo.AndamentoId equals andamento.Id
                    where andamento.TicketId == ticketId
                    select new { anexo.AndamentoId, anexo.NomeArquivoOriginal })
                .ToDictionary(a => a.AndamentoId, a => a.NomeArquivoOriginal);
        }

        public bool ExisteAndamentoDe(int usuarioId)
        {
            return _dbContext.Andamentos.Any(a => a.CriadoPorId == usuarioId);
        }

        public Andamento? Obter(int id)
        {
            return _dbContext.Andamentos.FirstOrDefault(a => a.Id == id);
        }

        public AnexoAndamento? ObterAnexo(int andamentoId)
        {
            return _dbContext.AnexosAndamento.FirstOrDefault(a => a.AndamentoId == andamentoId);
        }
    }
}
