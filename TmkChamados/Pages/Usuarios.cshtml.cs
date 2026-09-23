using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class UsuariosModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;
        private readonly IChamadoService _chamadoService;

        public UsuariosModel(IUsuarioService usuarioService, IChamadoService chamadoService)
        {
            _usuarioService = usuarioService;
            _chamadoService = chamadoService;
        }

        [BindProperty]
        public UsuarioFormModel Form { get; set; } = new();

        public IReadOnlyList<Usuario> Usuarios { get; private set; } = Array.Empty<Usuario>();

        public bool EmEdicao => Form.Id.HasValue;

        [TempData]
        public string? ErroExclusao { get; set; }

        public void OnGet(int? editId)
        {
            Usuarios = _usuarioService.Listar();

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
            if (!ModelState.IsValid)
            {
                Usuarios = _usuarioService.Listar();
                return Page();
            }

            _usuarioService.Criar(Form.Nome);
            return RedirectToPage();
        }

        public IActionResult OnPostAtualizar()
        {
            if (!ModelState.IsValid)
            {
                Usuarios = _usuarioService.Listar();
                return Page();
            }

            if (Form.Id.HasValue)
            {
                _usuarioService.Atualizar(Form.Id.Value, Form.Nome);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            var emUso = _chamadoService.Listar().Any(c => c.CriadoPorId == id || c.ResponsavelId == id);
            if (emUso)
            {
                ErroExclusao = "Não é possível excluir este usuário: ele está em uso como Criado por ou Responsável em algum chamado.";
            }
            else
            {
                _usuarioService.Excluir(id);
            }

            return RedirectToPage();
        }
    }

    public class UsuarioFormModel
    {
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;
    }
}
