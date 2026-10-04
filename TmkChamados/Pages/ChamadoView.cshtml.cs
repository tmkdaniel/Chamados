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

        [BindProperty]
        public ChamadoEdicaoFormModel Form { get; set; } = new();

        public Chamado? Chamado { get; private set; }

        public IReadOnlyList<Andamento> Andamentos { get; private set; } = Array.Empty<Andamento>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public IReadOnlyList<Usuario> UsuariosMaster { get; private set; } = Array.Empty<Usuario>();

        public async Task<IActionResult> OnGetAsync()
        {
            var acesso = await VerificarAcessoAsync();
            if (acesso is not null)
            {
                return acesso;
            }

            CarregarDados();
            PreencherFormDeEdicao();
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

            // O formulário de Andamento não envia os campos de Form (Título, Prioridade, etc.),
            // então a validação automática do model binding acusaria erros irrelevantes a esta ação.
            ModelState.Clear();

            if (string.IsNullOrWhiteSpace(NovoAndamentoTexto))
            {
                ModelState.AddModelError(nameof(NovoAndamentoTexto), "O texto do andamento é obrigatório.");
                CarregarDados();
                PreencherFormDeEdicao();
                return Page();
            }

            _andamentoService.Criar(Id, NovoAndamentoTexto, usuarioId);
            return RedirectToPage(new { Id });
        }

        public async Task<IActionResult> OnPostAtualizarAsync()
        {
            var acesso = await VerificarAcessoAsync();
            if (acesso is not null)
            {
                return acesso;
            }

            ValidarResponsavelMaster();

            if (!ModelState.IsValid)
            {
                CarregarDados();
                return Page();
            }

            var chamadoExistente = _chamadoService.Obter(Id)!;
            _chamadoService.Atualizar(Id, Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, Form.Prioridade!.Value, Form.DataPrazo, chamadoExistente.CriadoPorId, Form.ResponsavelId);

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
            UsuariosMaster = usuarios.Where(u => u.Tipo == TipoUsuario.Master).ToList();
        }

        private void PreencherFormDeEdicao()
        {
            if (Chamado is null)
            {
                return;
            }

            Form = new ChamadoEdicaoFormModel
            {
                Titulo = Chamado.Titulo,
                Descricao = Chamado.Descricao,
                Status = Chamado.Status,
                Prioridade = Chamado.Prioridade,
                DataPrazo = Chamado.DataPrazo,
                ResponsavelId = Chamado.ResponsavelId
            };
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

    public class ChamadoEdicaoFormModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; } = string.Empty;

        public StatusChamado Status { get; set; } = StatusChamado.Aberto;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A prioridade é obrigatória.")]
        public PrioridadeChamado? Prioridade { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DataPrazo { get; set; }

        public int? ResponsavelId { get; set; }
    }
}
