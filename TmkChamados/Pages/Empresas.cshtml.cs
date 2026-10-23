using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class EmpresasModel : PageModel
    {
        private readonly IEmpresaService _empresaService;
        private readonly IOrcamentoService _orcamentoService;

        public EmpresasModel(IEmpresaService empresaService, IOrcamentoService orcamentoService)
        {
            _empresaService = empresaService;
            _orcamentoService = orcamentoService;
        }

        public IReadOnlyList<Empresa> Empresas { get; private set; } = Array.Empty<Empresa>();

        [TempData]
        public string? ErroExclusao { get; set; }

        public IActionResult OnGet()
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            Empresas = _empresaService.Listar();
            return Page();
        }

        public IActionResult OnPostExcluir(int id)
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            var empresa = _empresaService.Obter(id);
            if (empresa is not null && empresa.Sistema)
            {
                ErroExclusao = "Não é possível excluir esta empresa: ela é uma empresa de sistema.";
            }
            else if (_orcamentoService.ExisteOrcamentoDaEmpresa(id))
            {
                ErroExclusao = "Não é possível excluir esta empresa: ela possui orçamentos.";
            }
            else if (!_empresaService.Excluir(id))
            {
                ErroExclusao = "Não é possível excluir esta empresa: ela possui usuários associados.";
            }

            return RedirectToPage();
        }
    }
}
