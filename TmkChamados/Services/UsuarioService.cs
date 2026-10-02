using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using TmkChamados.Data;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly TmkChamadosDbContext _dbContext;
        private readonly PasswordHasher<Usuario> _hasher = new();

        public UsuarioService(TmkChamadosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IReadOnlyList<Usuario> Listar()
        {
            return _dbContext.Usuarios.Include(u => u.Empresa).OrderBy(u => u.Id).ToList();
        }

        public Usuario? Obter(int id)
        {
            return _dbContext.Usuarios.FirstOrDefault(u => u.Id == id);
        }

        public bool NomeEmUso(string nome, int? ignorarId = null)
        {
            return _dbContext.Usuarios.Any(u =>
                u.Id != ignorarId &&
                u.Nome.ToLower() == nome.ToLower());
        }

        public Usuario Criar(string nome, string senha, TipoUsuario tipo, int empresaId)
        {
            var usuario = new Usuario
            {
                Nome = nome,
                Tipo = tipo,
                EmpresaId = empresaId
            };
            usuario.SenhaHash = _hasher.HashPassword(usuario, senha);

            _dbContext.Usuarios.Add(usuario);
            _dbContext.SaveChanges();
            return usuario;
        }

        public bool Atualizar(int id, string nome, string? senha, TipoUsuario tipo, int empresaId)
        {
            var usuario = _dbContext.Usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario is null)
            {
                return false;
            }

            usuario.Nome = nome;
            usuario.Tipo = tipo;
            usuario.EmpresaId = empresaId;
            if (!string.IsNullOrEmpty(senha))
            {
                usuario.SenhaHash = _hasher.HashPassword(usuario, senha);
            }

            _dbContext.SaveChanges();
            return true;
        }

        public bool Excluir(int id)
        {
            var usuario = _dbContext.Usuarios.FirstOrDefault(u => u.Id == id);
            if (usuario is null)
            {
                return false;
            }

            _dbContext.Usuarios.Remove(usuario);
            _dbContext.SaveChanges();
            return true;
        }

        public Usuario? ValidarCredenciais(string nome, string senha)
        {
            var usuario = _dbContext.Usuarios.FirstOrDefault(u => u.Nome.ToLower() == nome.ToLower());
            if (usuario is null)
            {
                return null;
            }

            var resultado = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, senha);
            return resultado == PasswordVerificationResult.Success ? usuario : null;
        }
    }
}
