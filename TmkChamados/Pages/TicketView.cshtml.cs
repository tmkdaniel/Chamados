using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class TicketViewModel : PageModel
    {
        private readonly ITicketService _ticketService;
        private readonly IAndamentoService _andamentoService;
        private readonly IUsuarioService _usuarioService;

        public TicketViewModel(ITicketService ticketService, IAndamentoService andamentoService, IUsuarioService usuarioService)
        {
            _ticketService = ticketService;
            _andamentoService = andamentoService;
            _usuarioService = usuarioService;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string? NovoAndamentoTexto { get; set; }

        [BindProperty]
        public TicketEdicaoFormModel Form { get; set; } = new();

        public Ticket? Ticket { get; private set; }

        public IReadOnlyList<Andamento> Andamentos { get; private set; } = Array.Empty<Andamento>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public IReadOnlyList<Usuario> UsuariosMaster { get; private set; } = Array.Empty<Usuario>();

        public bool IsMaster { get; private set; }

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

            var ticketExistente = _ticketService.Obter(Id)!;

            // Prioridade e Data de Prazo só podem ser alteradas por Master (ver design.md -
            // Decisão 3): para quem não é Master, o servidor ignora o que veio no post e
            // preserva os valores atuais, em vez de confiar no campo estar oculto/desabilitado na UI.
            if (!IsMaster)
            {
                Form.Prioridade = ticketExistente.Prioridade;
                Form.DataPrazo = ticketExistente.DataPrazo;

                // O campo Prioridade não é enviado no POST quando exibido como somente leitura
                // (fora do Form vinculado), então o model binding o marca como ausente e o
                // [Required] acima falha antes deste bloco rodar; removido aqui porque o valor
                // já foi restaurado logo acima.
                ModelState.Remove($"{nameof(Form)}.{nameof(Form.Prioridade)}");
            }

            ValidarResponsavelMaster();

            if (!ModelState.IsValid)
            {
                CarregarDados();
                return Page();
            }

            _ticketService.Atualizar(Id, Form.Classificacao!.Value, Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, Form.Prioridade!.Value, Form.DataPrazo, ticketExistente.CriadoPorId, Form.ResponsavelId);

            return RedirectToPage(new { Id });
        }

        public string ClasseCorStatus(StatusTicket status)
        {
            return status switch
            {
                StatusTicket.Aberto => "bg-danger",
                StatusTicket.EmAndamento => "bg-warning text-dark",
                StatusTicket.Concluido => "bg-success",
                StatusTicket.Cancelado => "bg-secondary",
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

            var ticket = _ticketService.Obter(Id);
            if (ticket is null || !_ticketService.PodeAcessar(ticket, tipoUsuario.Value, usuarioId.Value, empresaId.Value))
            {
                return Forbid();
            }

            IsMaster = tipoUsuario.Value == TipoUsuario.Master;

            return null;
        }

        private void CarregarDados()
        {
            Ticket = _ticketService.Obter(Id);
            Andamentos = _andamentoService.ListarPorTicket(Id);

            var usuarios = _usuarioService.Listar();
            NomesPorId = usuarios.ToDictionary(u => u.Id, u => u.Nome);
            UsuariosMaster = usuarios.Where(u => u.Tipo == TipoUsuario.Master).ToList();
        }

        private void PreencherFormDeEdicao()
        {
            if (Ticket is null)
            {
                return;
            }

            Form = new TicketEdicaoFormModel
            {
                Classificacao = Ticket.Classificacao,
                Titulo = Ticket.Titulo,
                Descricao = Ticket.Descricao,
                Status = Ticket.Status,
                Prioridade = Ticket.Prioridade,
                DataPrazo = Ticket.DataPrazo,
                ResponsavelId = Ticket.ResponsavelId
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

    public class TicketEdicaoFormModel
    {
        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A classificação é obrigatória.")]
        public ClassificacaoTicket? Classificacao { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; } = string.Empty;

        public StatusTicket Status { get; set; } = StatusTicket.Aberto;

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "A prioridade é obrigatória.")]
        public PrioridadeTicket? Prioridade { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? DataPrazo { get; set; }

        public int? ResponsavelId { get; set; }
    }
}
