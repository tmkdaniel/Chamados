using Microsoft.EntityFrameworkCore;
using TmkChamados.Data;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class TicketService : ITicketService
    {
        private readonly TmkChamadosDbContext _dbContext;

        public TicketService(TmkChamadosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IReadOnlyList<Ticket> Listar()
        {
            return _dbContext.Tickets.OrderBy(c => c.Id).ToList();
        }

        public IReadOnlyList<Ticket> Listar(FiltroTickets filtro, TipoUsuario tipoUsuario, int usuarioId, int empresaId)
        {
            IQueryable<Ticket> query = _dbContext.Tickets;

            if (tipoUsuario == TipoUsuario.Gerente)
            {
                query = query.Where(c => _dbContext.Usuarios.Any(u => u.Id == c.CriadoPorId && u.EmpresaId == empresaId));
            }
            else if (tipoUsuario == TipoUsuario.Usuario)
            {
                query = query.Where(c => c.CriadoPorId == usuarioId);
            }

            if (filtro.StatusAbertoOuEmAndamento)
            {
                query = query.Where(c => c.Status == StatusTicket.Aberto || c.Status == StatusTicket.EmAndamento);
            }
            else if (filtro.Status.HasValue)
            {
                query = query.Where(c => c.Status == filtro.Status.Value);
            }

            if (filtro.Prioridade.HasValue)
            {
                query = query.Where(c => c.Prioridade == filtro.Prioridade.Value);
            }

            if (filtro.CriadoPorId.HasValue)
            {
                query = query.Where(c => c.CriadoPorId == filtro.CriadoPorId.Value);
            }

            if (filtro.EmpresaId.HasValue)
            {
                query = query.Where(c => _dbContext.Usuarios.Any(u => u.Id == c.CriadoPorId && u.EmpresaId == filtro.EmpresaId.Value));
            }

            if (filtro.SemResponsavel)
            {
                query = query.Where(c => c.ResponsavelId == null);
            }
            else if (filtro.ResponsavelId.HasValue)
            {
                query = query.Where(c => c.ResponsavelId == filtro.ResponsavelId.Value);
            }

            if (filtro.CriadoDe.HasValue)
            {
                query = query.Where(c => c.DataCriacao.Date >= filtro.CriadoDe.Value.Date);
            }

            if (filtro.CriadoAte.HasValue)
            {
                query = query.Where(c => c.DataCriacao.Date <= filtro.CriadoAte.Value.Date);
            }

            if (filtro.ModificadoDe.HasValue)
            {
                query = query.Where(c => c.DataUltimaModificacao.Date >= filtro.ModificadoDe.Value.Date);
            }

            if (filtro.ModificadoAte.HasValue)
            {
                query = query.Where(c => c.DataUltimaModificacao.Date <= filtro.ModificadoAte.Value.Date);
            }

            return query.OrderBy(c => c.Id).ToList();
        }

        public Ticket? Obter(int id)
        {
            return _dbContext.Tickets.FirstOrDefault(c => c.Id == id);
        }

        public bool PodeAcessar(Ticket ticket, TipoUsuario tipoUsuario, int usuarioId, int empresaId)
        {
            return tipoUsuario switch
            {
                TipoUsuario.Master => true,
                TipoUsuario.Gerente => _dbContext.Usuarios.Any(u => u.Id == ticket.CriadoPorId && u.EmpresaId == empresaId),
                TipoUsuario.Usuario => ticket.CriadoPorId == usuarioId,
                _ => false
            };
        }

        public Ticket Criar(string titulo, string descricao, PrioridadeTicket prioridade, DateTime? dataPrazo, int criadoPorId)
        {
            var agora = DateTime.Now;
            var ticket = new Ticket
            {
                Titulo = titulo,
                Descricao = descricao,
                Status = StatusTicket.Aberto,
                Prioridade = prioridade,
                DataPrazo = dataPrazo,
                DataCriacao = agora,
                DataUltimaModificacao = agora,
                CriadoPorId = criadoPorId,
                ResponsavelId = null
            };

            _dbContext.Tickets.Add(ticket);
            _dbContext.SaveChanges();
            return ticket;
        }

        public bool Atualizar(int id, string titulo, string descricao, StatusTicket status, PrioridadeTicket prioridade, DateTime? dataPrazo, int criadoPorId, int? responsavelId)
        {
            var ticket = _dbContext.Tickets.FirstOrDefault(c => c.Id == id);
            if (ticket is null)
            {
                return false;
            }

            var eraTerminal = EhStatusTerminal(ticket.Status);
            var seraTerminal = EhStatusTerminal(status);

            if (seraTerminal && !eraTerminal)
            {
                ticket.DataConclusao = DateTime.Now;
            }
            else if (!seraTerminal && eraTerminal)
            {
                ticket.DataConclusao = null;
            }

            ticket.Titulo = titulo;
            ticket.Descricao = descricao;
            ticket.Status = status;
            ticket.Prioridade = prioridade;
            ticket.DataPrazo = dataPrazo;
            ticket.CriadoPorId = criadoPorId;
            ticket.ResponsavelId = responsavelId;
            ticket.DataUltimaModificacao = DateTime.Now;

            _dbContext.SaveChanges();
            return true;
        }

        private static bool EhStatusTerminal(StatusTicket status)
        {
            return status == StatusTicket.Concluido || status == StatusTicket.Cancelado;
        }

        public bool Excluir(int id)
        {
            var ticket = _dbContext.Tickets.FirstOrDefault(c => c.Id == id);
            if (ticket is null)
            {
                return false;
            }

            _dbContext.Tickets.Remove(ticket);
            _dbContext.SaveChanges();
            return true;
        }
    }
}
