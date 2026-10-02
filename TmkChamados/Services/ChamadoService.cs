using Microsoft.EntityFrameworkCore;
using TmkChamados.Data;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class ChamadoService : IChamadoService
    {
        private readonly TmkChamadosDbContext _dbContext;

        public ChamadoService(TmkChamadosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IReadOnlyList<Chamado> Listar()
        {
            return _dbContext.Chamados.OrderBy(c => c.Id).ToList();
        }

        public IReadOnlyList<Chamado> Listar(FiltroChamados filtro, TipoUsuario tipoUsuario, int usuarioId, int empresaId)
        {
            IQueryable<Chamado> query = _dbContext.Chamados;

            if (tipoUsuario == TipoUsuario.Gerente)
            {
                query = query.Where(c => _dbContext.Usuarios.Any(u => u.Id == c.CriadoPorId && u.EmpresaId == empresaId));
            }
            else if (tipoUsuario == TipoUsuario.Usuario)
            {
                query = query.Where(c => c.CriadoPorId == usuarioId);
            }

            if (filtro.Status.HasValue)
            {
                query = query.Where(c => c.Status == filtro.Status.Value);
            }

            if (filtro.CriadoPorId.HasValue)
            {
                query = query.Where(c => c.CriadoPorId == filtro.CriadoPorId.Value);
            }

            if (filtro.ResponsavelId.HasValue)
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

        public Chamado? Obter(int id)
        {
            return _dbContext.Chamados.FirstOrDefault(c => c.Id == id);
        }

        public Chamado Criar(string titulo, string descricao, StatusChamado status, int criadoPorId, int responsavelId)
        {
            var agora = DateTime.Now;
            var chamado = new Chamado
            {
                Titulo = titulo,
                Descricao = descricao,
                Status = status,
                DataCriacao = agora,
                DataUltimaModificacao = agora,
                CriadoPorId = criadoPorId,
                ResponsavelId = responsavelId
            };

            _dbContext.Chamados.Add(chamado);
            _dbContext.SaveChanges();
            return chamado;
        }

        public bool Atualizar(int id, string titulo, string descricao, StatusChamado status, int criadoPorId, int responsavelId)
        {
            var chamado = _dbContext.Chamados.FirstOrDefault(c => c.Id == id);
            if (chamado is null)
            {
                return false;
            }

            chamado.Titulo = titulo;
            chamado.Descricao = descricao;
            chamado.Status = status;
            chamado.CriadoPorId = criadoPorId;
            chamado.ResponsavelId = responsavelId;
            chamado.DataUltimaModificacao = DateTime.Now;

            _dbContext.SaveChanges();
            return true;
        }

        public bool Excluir(int id)
        {
            var chamado = _dbContext.Chamados.FirstOrDefault(c => c.Id == id);
            if (chamado is null)
            {
                return false;
            }

            _dbContext.Chamados.Remove(chamado);
            _dbContext.SaveChanges();
            return true;
        }
    }
}
