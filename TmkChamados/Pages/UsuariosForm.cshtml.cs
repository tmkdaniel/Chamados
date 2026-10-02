using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class UsuariosFormModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IEmpresaService _empresaService;

        public UsuariosFormModel(IUsuarioService usuarioService, IEmpresaService empresaService)
        {
            _usuarioService = usuarioService;
            _empresaService = empresaService;
        }

        [BindProperty]
        public UsuarioFormModel Form { get; set; } = new();

        public IReadOnlyList<Empresa> Empresas { get; private set; } = Array.Empty<Empresa>();

        public bool EmEdicao => Form.Id.HasValue;

        public void OnGet(int? editId)
        {
            Empresas = _empresaService.Listar();

            if (editId.HasValue)
            {
                var usuario = _usuarioService.Obter(editId.Value);
                if (usuario is not null)
                {
                    Form = new UsuarioFormModel
                    {
                        Id = usuario.Id,
                        Nome = usuario.Nome,
                        Tipo = usuario.Tipo,
                        EmpresaId = usuario.EmpresaId
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
                Empresas = _empresaService.Listar();
                return Page();
            }

            _usuarioService.Criar(Form.Nome, Form.Senha!, Form.Tipo!.Value, Form.EmpresaId!.Value);
            return RedirectToPage("/Usuarios");
        }

        public IActionResult OnPostAtualizar()
        {
            ValidarNomeUnico(ignorarId: Form.Id);

            if (!ModelState.IsValid)
            {
                Empresas = _empresaService.Listar();
                return Page();
            }

            if (Form.Id.HasValue)
            {
                _usuarioService.Atualizar(Form.Id.Value, Form.Nome, Form.Senha, Form.Tipo!.Value, Form.EmpresaId!.Value);
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

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O tipo é obrigatório.")]
        public TipoUsuario? Tipo { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A empresa é obrigatória.")]
        public int? EmpresaId { get; set; }
    }
}
