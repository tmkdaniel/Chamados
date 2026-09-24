using Microsoft.AspNetCore.Identity;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly List<Usuario> _usuarios = new();
        private int _proximoId = 1;
        private readonly object _lock = new();
        private readonly PasswordHasher<Usuario> _hasher = new();

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

        public bool NomeEmUso(string nome, int? ignorarId = null)
        {
            lock (_lock)
            {
                return _usuarios.Any(u =>
                    u.Id != ignorarId &&
                    string.Equals(u.Nome, nome, StringComparison.OrdinalIgnoreCase));
            }
        }

        public Usuario Criar(string nome, string senha)
        {
            lock (_lock)
            {
                var usuario = new Usuario
                {
                    Id = _proximoId++,
                    Nome = nome
                };
                usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
                _usuarios.Add(usuario);
                return usuario;
            }
        }

        public bool Atualizar(int id, string nome, string? senha)
        {
            lock (_lock)
            {
                var usuario = _usuarios.FirstOrDefault(u => u.Id == id);
                if (usuario is null)
                {
                    return false;
                }

                usuario.Nome = nome;
                if (!string.IsNullOrEmpty(senha))
                {
                    usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
                }

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

        public Usuario? ValidarCredenciais(string nome, string senha)
        {
            lock (_lock)
            {
                var usuario = _usuarios.FirstOrDefault(u => string.Equals(u.Nome, nome, StringComparison.OrdinalIgnoreCase));
                if (usuario is null)
                {
                    return null;
                }

                var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
                return resultado == PasswordVerificationResult.Success ? usuario : null;
            }
        }
    }
}
