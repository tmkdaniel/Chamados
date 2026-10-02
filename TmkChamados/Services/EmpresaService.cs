using TmkChamados.Data;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class EmpresaService : IEmpresaService
    {
        private readonly TmkChamadosDbContext _dbContext;

        public EmpresaService(TmkChamadosDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public IReadOnlyList<Empresa> Listar()
        {
            return _dbContext.Empresas.OrderBy(e => e.Id).ToList();
        }

        public Empresa? Obter(int id)
        {
            return _dbContext.Empresas.FirstOrDefault(e => e.Id == id);
        }

        public Empresa Criar(string nome)
        {
            var empresa = new Empresa
            {
                Nome = nome
            };

            _dbContext.Empresas.Add(empresa);
            _dbContext.SaveChanges();
            return empresa;
        }

        public bool Atualizar(int id, string nome)
        {
            var empresa = _dbContext.Empresas.FirstOrDefault(e => e.Id == id);
            if (empresa is null)
            {
                return false;
            }

            empresa.Nome = nome;
            _dbContext.SaveChanges();
            return true;
        }

        public bool Excluir(int id)
        {
            var empresa = _dbContext.Empresas.FirstOrDefault(e => e.Id == id);
            if (empresa is null)
            {
                return false;
            }

            var emUso = _dbContext.Usuarios.Any(u => u.EmpresaId == id);
            if (emUso)
            {
                return false;
            }

            _dbContext.Empresas.Remove(empresa);
            _dbContext.SaveChanges();
            return true;
        }
    }
}
