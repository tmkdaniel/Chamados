using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly List<Usuario> _usuarios = new();
        private int _proximoId = 1;
        private readonly object _lock = new();

        public IReadOnlyList<Usuario> Listar()
        {
            lock (_lock)
            {
                return _usuarios.ToList();
            }
        }

        public Usuario? Obter(int id)
        {
            lock (_lock)
            {
                return _usuarios.FirstOrDefault(u => u.Id == id);
            }
        }

        public Usuario Criar(string nome)
        {
            lock (_lock)
            {
                var usuario = new Usuario
                {
                    Id = _proximoId++,
                    Nome = nome
                };
                _usuarios.Add(usuario);
                return usuario;
            }
        }

        public bool Atualizar(int id, string nome)
        {
            lock (_lock)
            {
                var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
                if (usuario is null)
                {
                    return false;
                }

                usuario.Nome = nome;
                return true;
            }
        }

        public bool Excluir(int id)
        {
            lock (_lock)
            {
                var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
                if (usuario is null)
                {
                    return false;
                }

                return _usuarios.Remove(usuario);
            }
        }
    }
}
