using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class ChamadosFormModel : PageModel
    {
        private readonly IChamadoService _chamadoService;
        private readonly IUsuarioService _usuarioService;

        public ChamadosFormModel(IChamadoService chamadoService, IUsuarioService usuarioService)
        {
            _chamadoService = chamadoService;
            _usuarioService = usuarioService;
        }

        [BindProperty]
        public ChamadoFormModel Form { get; set; } = new();

        public void OnGet()
        {
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

            _chamadoService.Criar(Form.Titulo, Form.Descricao ?? string.Empty, Form.Prioridade!.Value, Form.DataPrazo, usuarioAutenticado.Id);
            return RedirectToPage("/Chamados");
        }
    }

    public class ChamadoFormModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A prioridade é obrigatória.")]
        public PrioridadeChamado? Prioridade { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DataPrazo { get; set; }
    }
}
