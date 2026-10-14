using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class TicketsFormModel : PageModel
    {
        private readonly ITicketService _ticketService;
        private readonly IUsuarioService _usuarioService;
        private readonly IProtecaoEnvioDuplicadoService _protecaoEnvioDuplicado;

        public TicketsFormModel(ITicketService ticketService, IUsuarioService usuarioService, IProtecaoEnvioDuplicadoService protecaoEnvioDuplicado)
        {
            _ticketService = ticketService;
            _usuarioService = usuarioService;
            _protecaoEnvioDuplicado = protecaoEnvioDuplicado;
        }

        [BindProperty]
        public TicketFormModel Form { get; set; } = new();

        [BindProperty]
        public string? TokenEnvio { get; set; }

        public void OnGet()
        {
            TokenEnvio = _protecaoEnvioDuplicado.GerarToken();
        }

        public async Task<IActionResult> OnPostAdicionarAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var usuarioAutenticado = _usuarioService.Obter(User.ObterUsuarioId() ?? 0);
            if (usuarioAutenticado is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Login");
            }

            // Protege contra duplo clique: a segunda submissão com o mesmo token é ignorada
            // silenciosamente (ver design.md - Decisão 3), em vez de criar um segundo ticket.
            if (!_protecaoEnvioDuplicado.TentarRegistrar(TokenEnvio))
            {
                return RedirectToPage("/Tickets");
            }

            _ticketService.Criar(Form.Classificacao!.Value, Form.Titulo, Form.Descricao ?? string.Empty, PrioridadeTicket.Normal, null, usuarioAutenticado.Id);
            return RedirectToPage("/Tickets");
        }
    }

    public class TicketFormModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A classificação é obrigatória.")]
        public ClassificacaoTicket? Classificacao { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; } = string.Empty;
    }
}
