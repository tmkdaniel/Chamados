using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class EmpresasFormModel : PageModel
    {
        private readonly IEmpresaService _empresaService;

        public EmpresasFormModel(IEmpresaService empresaService)
        {
            _empresaService = empresaService;
        }

        [BindProperty]
        public EmpresaFormModel Form { get; set; } = new();

        public bool EmEdicao => Form.Id.HasValue;

        public IActionResult OnGet(int? editId)
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            if (editId.HasValue)
            {
                var empresa = _empresaService.Obter(editId.Value);
                if (empresa is not null)
                {
                    Form = new EmpresaFormModel
                    {
                        Id = empresa.Id,
                        Nome = empresa.Nome
                    };
                }
            }

            return Page();
        }

        public IActionResult OnPostAdicionar()
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _empresaService.Criar(Form.Nome);
            return RedirectToPage("/Empresas");
        }

        public IActionResult OnPostAtualizar()
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Form.Id.HasValue)
            {
                _empresaService.Atualizar(Form.Id.Value, Form.Nome);
            }

            return RedirectToPage("/Empresas");
        }
    }

    public class EmpresaFormModel
    {
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;
    }
}
