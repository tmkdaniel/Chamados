using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class EmpresasFormModel : PageModel
    {
        private readonly IEmpresaService _empresaService;
        private readonly IProtecaoEnvioDuplicadoService _protecaoEnvioDuplicado;

        public EmpresasFormModel(IEmpresaService empresaService, IProtecaoEnvioDuplicadoService protecaoEnvioDuplicado)
        {
            _empresaService = empresaService;
            _protecaoEnvioDuplicado = protecaoEnvioDuplicado;
        }

        [BindProperty]
        public EmpresaFormModel Form { get; set; } = new();

        [BindProperty]
        public string? TokenEnvio { get; set; }

        public bool EmEdicao => Form.Id.HasValue;

        public IActionResult OnGet(int? editId)
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            TokenEnvio = _protecaoEnvioDuplicado.GerarToken();

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

            ValidarNomeUnico(ignorarId: null);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Protege contra duplo clique: a segunda submissão com o mesmo token é ignorada
            // silenciosamente (ver design.md - Decisão 3), em vez de criar uma segunda empresa.
            if (!_protecaoEnvioDuplicado.TentarRegistrar(TokenEnvio))
            {
                return RedirectToPage("/Empresas");
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

            ValidarNomeUnico(ignorarId: Form.Id);

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

        private void ValidarNomeUnico(int? ignorarId)
        {
            if (!string.IsNullOrEmpty(Form.Nome) && _empresaService.NomeEmUso(Form.Nome, ignorarId))
            {
                ModelState.AddModelError(nameof(Form.Nome), "Esse nome já está em uso por outra empresa.");
            }
        }
    }

    public class EmpresaFormModel
    {
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;
    }
}
