using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class EmpresasModel : PageModel
    {
        private readonly IEmpresaService _empresaService;

        public EmpresasModel(IEmpresaService empresaService)
        {
            _empresaService = empresaService;
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

            if (!_empresaService.Excluir(id))
            {
                ErroExclusao = "Não é possível excluir esta empresa: ela possui usuários associados.";
            }

            return RedirectToPage();
        }
    }
}
