using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class UsuariosFormModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;

        public UsuariosFormModel(IUsuarioService usuarioService)
        {
            _usuarioService = usuarioService;
        }

        [BindProperty]
        public UsuarioFormModel Form { get; set; } = new();

        public bool EmEdicao => Form.Id.HasValue;

        public void OnGet(int? editId)
        {
            if (editId.HasValue)
            {
                var usuario = _usuarioService.Obter(editId.Value);
                if (usuario is not null)
                {
                    Form = new UsuarioFormModel
                    {
                        Id = usuario.Id,
                        Nome = usuario.Nome
                    };
                }
            }
        }

        public IActionResult OnPostAdicionar()
        {
            ValidarSenhaObrigatoria();
            ValidarNomeUnico(ignorarId: null);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            _usuarioService.Criar(Form.Nome, Form.Senha!);
            return RedirectToPage("/Usuarios");
        }

        public IActionResult OnPostAtualizar()
        {
            ValidarNomeUnico(ignorarId: Form.Id);

            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (Form.Id.HasValue)
            {
                _usuarioService.Atualizar(Form.Id.Value, Form.Nome, Form.Senha);
            }

            return RedirectToPage("/Usuarios");
        }

        private void ValidarSenhaObrigatoria()
        {
            if (string.IsNullOrEmpty(Form.Senha))
            {
                ModelState.AddModelError(nameof(Form.Senha), "A senha é obrigatória.");
            }
        }

        private void ValidarNomeUnico(int? ignorarId)
        {
            if (!string.IsNullOrEmpty(Form.Nome) && _usuarioService.NomeEmUso(Form.Nome, ignorarId))
            {
                ModelState.AddModelError(nameof(Form.Nome), "Esse nome já está em uso por outro usuário.");
            }
        }
    }

    public class UsuarioFormModel
    {
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        public string? Senha { get; set; }
    }
}
