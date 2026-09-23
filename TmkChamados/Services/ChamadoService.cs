using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class ChamadoService : IChamadoService
    {
        private readonly List<Chamado> _chamados = new();
        private int _proximoId = 1;
        private readonly object _lock = new();

        public IReadOnlyList<Chamado> Listar()
        {
            lock (_lock)
            {
                return _chamados.ToList();
            }
        }

        public IReadOnlyList<Chamado> Listar(FiltroChamados filtro)
        {
            lock (_lock)
            {
                IEnumerable<Chamado> query = _chamados;

                if (filtro.Status.HasValue)
                {
                    query = query.Where(c => c.Status == filtro.Status.Value);
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

                return query.ToList();
            }
        }

        public Chamado? Obter(int id)
        {
            lock (_lock)
            {
                return _chamados.FirstOrDefault(c => c.Id == id);
            }
        }

        public Chamado Criar(string titulo, string descricao, StatusChamado status, int criadoPorId, int responsavelId)
        {
            lock (_lock)
            {
                var agora = DateTime.Now;
                var chamado = new Chamado
                {
                    Id = _proximoId++,
                    Titulo = titulo,
                    Descricao = descricao,
                    Status = status,
                    DataCriacao = agora,
                    DataUltimaModificacao = agora,
                    CriadoPorId = criadoPorId,
                    ResponsavelId = responsavelId
                };
                _chamados.Add(chamado);
                return chamado;
            }
        }

        public bool Atualizar(int id, string titulo, string descricao, StatusChamado status, int criadoPorId, int responsavelId)
        {
            lock (_lock)
            {
                var chamado = _chamados.FirstOrDefault(c => c.Id == id);
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
                return true;
            }
        }

        public bool Excluir(int id)
        {
            lock (_lock)
            {
                var chamado = _chamados.FirstOrDefault(c => c.Id == id);
                if (chamado is null)
                {
                    return false;
                }

                return _chamados.Remove(chamado);
            }
        }
    }
}
