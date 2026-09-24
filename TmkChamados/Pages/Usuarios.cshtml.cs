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

        public IReadOnlyList<Usuario> Usuarios { get; private set; } = Array.Empty<Usuario>();

        [TempData]
        public string? ErroExclusao { get; set; }

        public void OnGet()
        {
            Usuarios = _usuarioService.Listar();
        }

        public IActionResult OnPostExcluir(int id)
        {
            var emUso = _chamadoService.Listar().Any(c => c.CriadoPorId == id || c.ResponsavelId == id);
            var ultimoUsuario = _usuarioService.Listar().Count <= 1;

            if (emUso)
            {
                ErroExclusao = "Não é possível excluir este usuário: ele está em uso como Criado por ou Responsável em algum chamado.";
            }
            else if (ultimoUsuario)
            {
                ErroExclusao = "Não é possível excluir este usuário: deve existir pelo menos um usuário cadastrado no sistema.";
            }
            else
            {
                _usuarioService.Excluir(id);
            }

            return RedirectToPage();
        }
    }
}
