using System.Security.Claims;
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

        public IReadOnlyList<Usuario> UsuariosMaster { get; private set; } = Array.Empty<Usuario>();

        public bool EmEdicao => Form.Id.HasValue;

        public void OnGet(int? editId)
        {
            UsuariosMaster = _usuarioService.Listar().Where(u => u.Tipo == TipoUsuario.Master).ToList();

            if (editId.HasValue)
            {
                var chamado = _chamadoService.Obter(editId.Value);
                if (chamado is not null)
                {
                    Form = new ChamadoFormModel
                    {
                        Id = chamado.Id,
                        Titulo = chamado.Titulo,
                        Descricao = chamado.Descricao,
                        Status = chamado.Status,
                        ResponsavelId = chamado.ResponsavelId
                    };
                }
            }
        }

        public async Task<IActionResult> OnPostAdicionarAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var usuarioAutenticado = ObterUsuarioAutenticado();
            if (usuarioAutenticado is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Login");
            }

            _chamadoService.Criar(Form.Titulo, Form.Descricao ?? string.Empty, usuarioAutenticado.Id);
            return RedirectToPage("/Chamados");
        }

        public IActionResult OnPostAtualizar()
        {
            ValidarResponsavelMaster();

            if (!ModelState.IsValid)
            {
                UsuariosMaster = _usuarioService.Listar().Where(u => u.Tipo == TipoUsuario.Master).ToList();
                return Page();
            }

            if (Form.Id.HasValue)
            {
                var chamadoExistente = _chamadoService.Obter(Form.Id.Value);
                if (chamadoExistente is not null)
                {
                    _chamadoService.Atualizar(Form.Id.Value, Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, chamadoExistente.CriadoPorId, Form.ResponsavelId);
                }
            }

            return RedirectToPage("/Chamados");
        }

        private Usuario? ObterUsuarioAutenticado()
        {
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (idClaim is null || !int.TryParse(idClaim, out var id))
            {
                return null;
            }

            return _usuarioService.Obter(id);
        }

        private void ValidarResponsavelMaster()
        {
            if (!Form.ResponsavelId.HasValue)
            {
                return;
            }

            var responsavel = _usuarioService.Obter(Form.ResponsavelId.Value);
            if (responsavel is null || responsavel.Tipo != TipoUsuario.Master)
            {
                ModelState.AddModelError(nameof(Form.ResponsavelId), "O Responsável deve ser um usuário do tipo Master.");
            }
        }
    }

    public class ChamadoFormModel
    {
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; } = string.Empty;

        public StatusChamado Status { get; set; } = StatusChamado.Aberto;

        public int? ResponsavelId { get; set; }
    }
}
