using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class ChamadoViewModel : PageModel
    {
        private readonly IChamadoService _chamadoService;
        private readonly IAndamentoService _andamentoService;
        private readonly IUsuarioService _usuarioService;

        public ChamadoViewModel(IChamadoService chamadoService, IAndamentoService andamentoService, IUsuarioService usuarioService)
        {
            _chamadoService = chamadoService;
            _andamentoService = andamentoService;
            _usuarioService = usuarioService;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string? NovoAndamentoTexto { get; set; }

        public Chamado? Chamado { get; private set; }

        public IReadOnlyList<Andamento> Andamentos { get; private set; } = Array.Empty<Andamento>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            var acesso = await VerificarAcessoAsync();
            if (acesso is not null)
            {
                return acesso;
            }

            CarregarDados();
            return Page();
        }

        public async Task<IActionResult> OnPostAdicionarAndamentoAsync()
        {
            var acesso = await VerificarAcessoAsync();
            if (acesso is not null)
            {
                return acesso;
            }

            var usuarioId = User.ObterUsuarioId()!.Value;

            if (string.IsNullOrWhiteSpace(NovoAndamentoTexto))
            {
                ModelState.AddModelError(nameof(NovoAndamentoTexto), "O texto do andamento é obrigatório.");
                CarregarDados();
                return Page();
            }

            _andamentoService.Criar(Id, NovoAndamentoTexto, usuarioId);
            return RedirectToPage(new { Id });
        }

        public string ClasseCorStatus(StatusChamado status)
        {
            return status switch
            {
                StatusChamado.Aberto => "bg-danger",
                StatusChamado.EmAndamento => "bg-warning text-dark",
                StatusChamado.Concluido => "bg-success",
                StatusChamado.Cancelado => "bg-secondary",
                _ => "bg-light text-dark"
            };
        }

        private async Task<IActionResult?> VerificarAcessoAsync()
        {
            var tipoUsuario = User.ObterTipoUsuario();
            var usuarioId = User.ObterUsuarioId();
            var empresaId = User.ObterEmpresaId();

            if (tipoUsuario is null || usuarioId is null || empresaId is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Login");
            }

            var chamado = _chamadoService.Obter(Id);
            if (chamado is null || !_chamadoService.PodeAcessar(chamado, tipoUsuario.Value, usuarioId.Value, empresaId.Value))
            {
                return Forbid();
            }

            return null;
        }

        private void CarregarDados()
        {
            Chamado = _chamadoService.Obter(Id);
            Andamentos = _andamentoService.ListarPorChamado(Id);

            var usuarios = _usuarioService.Listar();
            NomesPorId = usuarios.ToDictionary(u => u.Id, u => u.Nome);
        }
    }
}
