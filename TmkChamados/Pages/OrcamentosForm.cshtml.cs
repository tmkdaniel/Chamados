using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class OrcamentoFormModel
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [MaxLength(200, ErrorMessage = "O título deve ter no máximo 200 caracteres.")]
        public string? Titulo { get; set; }

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string? Descricao { get; set; }
    }

    public class OrcamentosFormModel : PageModel
    {
        private readonly IOrcamentoService _orcamentoService;
        private readonly IProtecaoEnvioDuplicadoService _protecaoEnvioDuplicado;

        public OrcamentosFormModel(IOrcamentoService orcamentoService, IProtecaoEnvioDuplicadoService protecaoEnvioDuplicado)
        {
            _orcamentoService = orcamentoService;
            _protecaoEnvioDuplicado = protecaoEnvioDuplicado;
        }

        [BindProperty]
        public OrcamentoFormModel Form { get; set; } = new();

        [BindProperty]
        public string? TokenEnvio { get; set; }

        public IActionResult OnGet()
        {
            if (!EhGerente())
            {
                return Forbid();
            }

            TokenEnvio = _protecaoEnvioDuplicado.GerarToken();
            return Page();
        }

        public IActionResult OnPostAdicionar()
        {
            if (!EhGerente())
            {
                return Forbid();
            }

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Protege contra duplo clique: a segunda submissão com o mesmo token é ignorada
            // silenciosamente, em vez de criar um segundo orçamento.
            if (!_protecaoEnvioDuplicado.TentarRegistrar(TokenEnvio))
            {
                return RedirectToPage("/Orcamentos");
            }

            var orcamento = _orcamentoService.Criar(
                Form.Titulo!,
                Form.Descricao!,
                User.ObterEmpresaId()!.Value,
                User.ObterUsuarioId()!.Value);

            return RedirectToPage("/OrcamentoView", new { id = orcamento.Id });
        }

        private bool EhGerente()
        {
            return User.ObterTipoUsuario() == TipoUsuario.Gerente
                && User.ObterUsuarioId() is not null
                && User.ObterEmpresaId() is not null;
        }
    }
}
