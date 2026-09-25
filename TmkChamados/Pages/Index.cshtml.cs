using System.Security.Claims;
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

        private readonly IChamadoService _chamadoService;
        private readonly IUsuarioService _usuarioService;

        public IndexModel(IChamadoService chamadoService, IUsuarioService usuarioService)
        {
            _chamadoService = chamadoService;
            _usuarioService = usuarioService;
        }

        public ResumoChamados ResumoResponsavel { get; private set; } = ResumoChamados.Vazio();

        public ResumoChamados ResumoCriador { get; private set; } = ResumoChamados.Vazio();

        public async Task<IActionResult> OnGetAsync()
        {
            var usuarioAutenticado = ObterUsuarioAutenticado();
            if (usuarioAutenticado is null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToPage("/Login");
            }

            var chamados = _chamadoService.Listar();

            var comoResponsavel = chamados.Where(c => c.ResponsavelId == usuarioAutenticado.Id).ToList();
            var comoCriador = chamados.Where(c => c.CriadoPorId == usuarioAutenticado.Id).ToList();

            ResumoResponsavel = ConstruirResumo(comoResponsavel);
            ResumoCriador = ConstruirResumo(comoCriador);

            return Page();
        }

        private static ResumoChamados ConstruirResumo(IReadOnlyList<Chamado> chamados)
        {
            var inicioJanela = DateTime.Now.Date.AddDays(-(DiasJanela - 1));
            var chamadosNaJanela = chamados.Where(c => c.DataCriacao.Date >= inicioJanela).ToList();

            var diasHistograma = Enumerable.Range(0, DiasJanela)
                .Select(offset => inicioJanela.AddDays(offset))
                .ToList();

            return new ResumoChamados
            {
                QuantidadeAbertos = chamados.Count(c => c.Status == StatusChamado.Aberto),
                QuantidadeEmAndamento = chamados.Count(c => c.Status == StatusChamado.EmAndamento),
                PizzaAbertos = chamadosNaJanela.Count(c => c.Status == StatusChamado.Aberto),
                PizzaEmAndamento = chamadosNaJanela.Count(c => c.Status == StatusChamado.EmAndamento),
                PizzaConcluidos = chamadosNaJanela.Count(c => c.Status == StatusChamado.Concluido),
                HistogramaRotulos = diasHistograma.Select(d => d.ToString("dd/MM")).ToList(),
                HistogramaValores = diasHistograma
                    .Select(dia => chamadosNaJanela.Count(c => c.DataCriacao.Date == dia))
                    .ToList()
            };
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
    }

    public class ResumoChamados
    {
        public int QuantidadeAbertos { get; set; }

        public int QuantidadeEmAndamento { get; set; }

        public int PizzaAbertos { get; set; }

        public int PizzaEmAndamento { get; set; }

        public int PizzaConcluidos { get; set; }

        public List<string> HistogramaRotulos { get; set; } = new();

        public List<int> HistogramaValores { get; set; } = new();

        public static ResumoChamados Vazio() => new();
    }
}
