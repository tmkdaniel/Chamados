using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class UsuariosModel : PageModel
    {
        private readonly IUsuarioService _usuarioService;
        private readonly ITicketService _ticketService;

        public UsuariosModel(IUsuarioService usuarioService, ITicketService ticketService)
        {
            _usuarioService = usuarioService;
            _ticketService = ticketService;
        }

        public IReadOnlyList<Usuario> Usuarios { get; private set; } = Array.Empty<Usuario>();

        [TempData]
        public string? ErroExclusao { get; set; }

        public IActionResult OnGet()
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            Usuarios = _usuarioService.Listar();
            return Page();
        }

        public IActionResult OnPostExcluir(int id)
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            var emUso = _ticketService.Listar().Any(c => c.CriadoPorId == id || c.ResponsavelId == id);
            var ultimoUsuario = _usuarioService.Listar().Count <= 1;

            if (emUso)
            {
                ErroExclusao = "Não é possível excluir este usuário: ele está em uso como Criado por ou Responsável em algum ticket.";
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
