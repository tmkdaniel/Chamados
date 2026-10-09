using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class TicketsModel : PageModel
    {
        private const string FiltroResponsavelSemResponsavel = "none";
        private const string FiltroStatusAbertoOuEmAndamento = "AbertoEmAndamento";

        private readonly ITicketService _ticketService;
        private readonly IUsuarioService _usuarioService;
        private readonly IEmpresaService _empresaService;

        public TicketsModel(ITicketService ticketService, IUsuarioService usuarioService, IEmpresaService empresaService)
        {
            _ticketService = ticketService;
            _usuarioService = usuarioService;
            _empresaService = empresaService;
        }

        [BindProperty(SupportsGet = true)]
        public FiltroFormModel Filtro { get; set; } = new();

        public IReadOnlyList<Ticket> Tickets { get; private set; } = Array.Empty<Ticket>();

        public IReadOnlyList<Usuario> Usuarios { get; private set; } = Array.Empty<Usuario>();

        public IReadOnlyList<Usuario> UsuariosMaster { get; private set; } = Array.Empty<Usuario>();

        public IReadOnlyList<Usuario> UsuariosDaEmpresa { get; private set; } = Array.Empty<Usuario>();

        public IReadOnlyList<Usuario> UsuariosParaFiltroCriadoPorMaster { get; private set; } = Array.Empty<Usuario>();

        public IReadOnlyList<Empresa> Empresas { get; private set; } = Array.Empty<Empresa>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public Dictionary<int, string> EmpresaPorUsuarioId { get; private set; } = new();

        public bool IsMaster { get; private set; }

        public bool IsGerente { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var tipoUsuario = User.ObterTipoUsuario();
            var usuarioId = User.ObterUsuarioId();
            var empresaId = User.ObterEmpresaId();

            if (tipoUsuario is null || usuarioId is null || empresaId is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Login");
            }

            IsMaster = tipoUsuario.Value == TipoUsuario.Master;
            IsGerente = tipoUsuario.Value == TipoUsuario.Gerente;

            if (!Request.Query.ContainsKey("Filtro.StatusFiltro"))
            {
                Filtro.StatusFiltro = FiltroStatusAbertoOuEmAndamento;
            }

            Tickets = _ticketService.Listar(ConstruirFiltro(), tipoUsuario.Value, usuarioId.Value, empresaId.Value);
            Usuarios = _usuarioService.Listar();
            UsuariosMaster = Usuarios.Where(u => u.Tipo == TipoUsuario.Master).ToList();
            UsuariosDaEmpresa = Usuarios.Where(u => u.EmpresaId == empresaId.Value).ToList();
            UsuariosParaFiltroCriadoPorMaster = Filtro.EmpresaId.HasValue
                ? Usuarios.Where(u => u.EmpresaId == Filtro.EmpresaId.Value).ToList()
                : Usuarios;
            Empresas = _empresaService.Listar();
            NomesPorId = Usuarios.ToDictionary(u => u.Id, u => u.Nome);
            EmpresaPorUsuarioId = Usuarios.ToDictionary(u => u.Id, u => u.Empresa?.Nome ?? "-");
            return Page();
        }

        public IActionResult OnPostExcluir(int id)
        {
            if (!User.EhMaster())
            {
                return Forbid();
            }

            _ticketService.Excluir(id);
            return RedirectToPage();
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

        private FiltroTickets ConstruirFiltro()
        {
            var filtro = new FiltroTickets
            {
                Classificacao = Filtro.Classificacao,
                Prioridade = Filtro.Prioridade,
                CriadoPorId = Filtro.CriadoPorId,
                EmpresaId = Filtro.EmpresaId,
                CriadoDe = Filtro.CriadoDe,
                CriadoAte = Filtro.CriadoAte,
                ModificadoDe = Filtro.ModificadoDe,
                ModificadoAte = Filtro.ModificadoAte
            };

            if (string.Equals(Filtro.StatusFiltro, FiltroStatusAbertoOuEmAndamento, StringComparison.OrdinalIgnoreCase))
            {
                filtro.StatusAbertoOuEmAndamento = true;
            }
            else if (!string.IsNullOrEmpty(Filtro.StatusFiltro) && Enum.TryParse<StatusTicket>(Filtro.StatusFiltro, out var status))
            {
                filtro.Status = status;
            }

            if (string.Equals(Filtro.ResponsavelFiltro, FiltroResponsavelSemResponsavel, StringComparison.OrdinalIgnoreCase))
            {
                filtro.SemResponsavel = true;
            }
            else if (!string.IsNullOrEmpty(Filtro.ResponsavelFiltro) && int.TryParse(Filtro.ResponsavelFiltro, out var responsavelId))
            {
                filtro.ResponsavelId = responsavelId;
            }

            return filtro;
        }
    }

    public class FiltroFormModel
    {
        public ClassificacaoTicket? Classificacao { get; set; }

        public string? StatusFiltro { get; set; }

        public PrioridadeTicket? Prioridade { get; set; }

        public int? CriadoPorId { get; set; }

        public int? EmpresaId { get; set; }

        public string? ResponsavelFiltro { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? CriadoDe { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? CriadoAte { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? ModificadoDe { get; set; }

        [System.ComponentModel.DataAnnotations.DataType(System.ComponentModel.DataAnnotations.DataType.Date)]
        public DateTime? ModificadoAte { get; set; }
    }
}
