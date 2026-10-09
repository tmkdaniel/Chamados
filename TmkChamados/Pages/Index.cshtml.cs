using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class IndexModel : PageModel
    {
        private const int DiasJanela = 30;

        private readonly ITicketService _ticketService;
        private readonly IUsuarioService _usuarioService;

        public IndexModel(ITicketService ticketService, IUsuarioService usuarioService)
        {
            _ticketService = ticketService;
            _usuarioService = usuarioService;
        }

        public string TituloSecao { get; private set; } = string.Empty;

        public ResumoTickets Resumo { get; private set; } = ResumoTickets.Vazio();

        public bool IsMaster { get; private set; }

        public GraficoEmpresa GraficoAbertosPorEmpresa { get; private set; } = GraficoEmpresa.Vazio();

        public GraficoPrioridade GraficoPorPrioridade { get; private set; } = GraficoPrioridade.Vazio();

        public GraficoDiasParaPrazo GraficoPorDiasParaPrazo { get; private set; } = GraficoDiasParaPrazo.Vazio();

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

            IReadOnlyList<Ticket> ticketsDaSecao;
            switch (tipoUsuario.Value)
            {
                case TipoUsuario.Master:
                    TituloSecao = "Como Responsável";
                    ticketsDaSecao = _ticketService.Listar(
                        new FiltroTickets { ResponsavelId = usuarioId.Value },
                        TipoUsuario.Master, usuarioId.Value, empresaId.Value);
                    break;
                case TipoUsuario.Gerente:
                    TituloSecao = "Tickets da Empresa";
                    ticketsDaSecao = _ticketService.Listar(
                        new FiltroTickets(), TipoUsuario.Gerente, usuarioId.Value, empresaId.Value);
                    break;
                default:
                    TituloSecao = "Como Criador";
                    ticketsDaSecao = _ticketService.Listar(
                        new FiltroTickets(), TipoUsuario.Usuario, usuarioId.Value, empresaId.Value);
                    break;
            }

            Resumo = ConstruirResumo(ticketsDaSecao);

            if (IsMaster)
            {
                GraficoAbertosPorEmpresa = ConstruirGraficoAbertosPorEmpresa(usuarioId.Value, empresaId.Value);
            }

            GraficoPorPrioridade = ConstruirGraficoPorPrioridade(tipoUsuario.Value, usuarioId.Value, empresaId.Value);
            GraficoPorDiasParaPrazo = ConstruirGraficoPorDiasParaPrazo(tipoUsuario.Value, usuarioId.Value, empresaId.Value);

            return Page();
        }

        private static ResumoTickets ConstruirResumo(IReadOnlyList<Ticket> tickets)
        {
            var inicioJanela = DateTime.Now.Date.AddDays(-(DiasJanela - 1));
            var ticketsNaJanela = tickets.Where(c => c.DataCriacao.Date >= inicioJanela).ToList();

            var diasHistograma = Enumerable.Range(0, DiasJanela)
                .Select(offset => inicioJanela.AddDays(offset))
                .ToList();

            return new ResumoTickets
            {
                QuantidadeAbertos = tickets.Count(c => c.Status == StatusTicket.Aberto),
                QuantidadeEmAndamento = tickets.Count(c => c.Status == StatusTicket.EmAndamento),
                PizzaAbertos = ticketsNaJanela.Count(c => c.Status == StatusTicket.Aberto),
                PizzaEmAndamento = ticketsNaJanela.Count(c => c.Status == StatusTicket.EmAndamento),
                PizzaConcluidos = ticketsNaJanela.Count(c => c.Status == StatusTicket.Concluido),
                HistogramaRotulos = diasHistograma.Select(d => d.ToString("dd/MM")).ToList(),
                HistogramaValores = diasHistograma
                    .Select(dia => ticketsNaJanela.Count(c => c.DataCriacao.Date == dia))
                    .ToList()
            };
        }

        private GraficoEmpresa ConstruirGraficoAbertosPorEmpresa(int usuarioId, int empresaId)
        {
            var todosAbertos = _ticketService.Listar(
                new FiltroTickets { Status = StatusTicket.Aberto },
                TipoUsuario.Master, usuarioId, empresaId);

            var empresaPorUsuarioId = _usuarioService.Listar()
                .ToDictionary(u => u.Id, u => u.Empresa?.Nome ?? "-");

            var agrupado = todosAbertos
                .GroupBy(c => empresaPorUsuarioId.TryGetValue(c.CriadoPorId, out var nome) ? nome : "-")
                .OrderBy(g => g.Key)
                .ToList();

            return new GraficoEmpresa
            {
                Rotulos = agrupado.Select(g => g.Key).ToList(),
                Valores = agrupado.Select(g => g.Count()).ToList()
            };
        }

        private GraficoPrioridade ConstruirGraficoPorPrioridade(TipoUsuario tipoUsuario, int usuarioId, int empresaId)
        {
            var abertosOuEmAndamento = _ticketService.Listar(
                new FiltroTickets { StatusAbertoOuEmAndamento = true },
                tipoUsuario, usuarioId, empresaId);

            var prioridades = Enum.GetValues<PrioridadeTicket>();

            return new GraficoPrioridade
            {
                Rotulos = prioridades.Select(p => p.ToString()).ToList(),
                Valores = prioridades.Select(p => abertosOuEmAndamento.Count(c => c.Prioridade == p)).ToList()
            };
        }

        private GraficoDiasParaPrazo ConstruirGraficoPorDiasParaPrazo(TipoUsuario tipoUsuario, int usuarioId, int empresaId)
        {
            var abertosOuEmAndamento = _ticketService.Listar(
                new FiltroTickets { StatusAbertoOuEmAndamento = true },
                tipoUsuario, usuarioId, empresaId);

            var hoje = DateTime.Now.Date;
            var faixas = new[] { "Atrasado", "Hoje", "1-3 dias", "4-7 dias", "8-15 dias", "16+ dias", "Sem Prazo" };

            var valoresPorFaixa = faixas.ToDictionary(f => f, _ => 0);

            foreach (var ticket in abertosOuEmAndamento)
            {
                var faixa = ClassificarFaixaDeDiasParaPrazo(ticket.DataPrazo, hoje);
                valoresPorFaixa[faixa]++;
            }

            return new GraficoDiasParaPrazo
            {
                Rotulos = faixas.ToList(),
                Valores = faixas.Select(f => valoresPorFaixa[f]).ToList()
            };
        }

        private static string ClassificarFaixaDeDiasParaPrazo(DateTime? dataPrazo, DateTime hoje)
        {
            if (!dataPrazo.HasValue)
            {
                return "Sem Prazo";
            }

            var dias = (dataPrazo.Value.Date - hoje).Days;

            return dias switch
            {
                < 0 => "Atrasado",
                0 => "Hoje",
                >= 1 and <= 3 => "1-3 dias",
                >= 4 and <= 7 => "4-7 dias",
                >= 8 and <= 15 => "8-15 dias",
                _ => "16+ dias"
            };
        }
    }

    public class ResumoTickets
    {
        public int QuantidadeAbertos { get; set; }

        public int QuantidadeEmAndamento { get; set; }

        public int PizzaAbertos { get; set; }

        public int PizzaEmAndamento { get; set; }

        public int PizzaConcluidos { get; set; }

        public List<string> HistogramaRotulos { get; set; } = new();

        public List<int> HistogramaValores { get; set; } = new();

        public static ResumoTickets Vazio() => new();
    }

    public class GraficoEmpresa
    {
        public List<string> Rotulos { get; set; } = new();

        public List<int> Valores { get; set; } = new();

        public static GraficoEmpresa Vazio() => new();
    }

    public class GraficoPrioridade
    {
        public List<string> Rotulos { get; set; } = new();

        public List<int> Valores { get; set; } = new();

        public static GraficoPrioridade Vazio() => new();
    }

    public class GraficoDiasParaPrazo
    {
        public List<string> Rotulos { get; set; } = new();

        public List<int> Valores { get; set; } = new();

        public static GraficoDiasParaPrazo Vazio() => new();
    }
}
