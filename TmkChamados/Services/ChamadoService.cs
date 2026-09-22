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

        public Chamado? Obter(int id)
        {
            lock (_lock)
            {
                return _chamados.FirstOrDefault(c => c.Id == id);
            }
        }

        public Chamado Criar(string titulo, string descricao, StatusChamado status)
        {
            lock (_lock)
            {
                var chamado = new Chamado
                {
                    Id = _proximoId++,
                    Titulo = titulo,
                    Descricao = descricao,
                    Status = status
                };
                _chamados.Add(chamado);
                return chamado;
            }
        }

        public bool Atualizar(int id, string titulo, string descricao, StatusChamado status)
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
