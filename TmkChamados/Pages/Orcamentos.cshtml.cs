using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class OrcamentosModel : PageModel
    {
        private readonly IOrcamentoService _orcamentoService;
        private readonly IUsuarioService _usuarioService;
        private readonly IEmpresaService _empresaService;

        public OrcamentosModel(IOrcamentoService orcamentoService, IUsuarioService usuarioService, IEmpresaService empresaService)
        {
            _orcamentoService = orcamentoService;
            _usuarioService = usuarioService;
            _empresaService = empresaService;
        }

        [BindProperty(SupportsGet = true)]
        public FiltroOrcamentos Filtro { get; set; } = new();

        public IReadOnlyList<Orcamento> Orcamentos { get; private set; } = Array.Empty<Orcamento>();

        public IReadOnlyList<Empresa> Empresas { get; private set; } = Array.Empty<Empresa>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public Dictionary<int, string> EmpresaPorId { get; private set; } = new();

        // OrcamentoId -> perguntas pendentes (só orçamentos em negociação).
        public IReadOnlyDictionary<int, int> PendentesPorId { get; private set; } = new Dictionary<int, int>();

        public bool IsMaster { get; private set; }

        public bool IsGerente { get; private set; }

        public IActionResult OnGet()
        {
            var tipoUsuario = User.ObterTipoUsuario();
            var empresaId = User.ObterEmpresaId();
            if (!User.EhMasterOuGerente() || tipoUsuario is null || empresaId is null)
            {
                return Forbid();
            }

            IsMaster = tipoUsuario.Value == TipoUsuario.Master;
            IsGerente = tipoUsuario.Value == TipoUsuario.Gerente;

            Orcamentos = _orcamentoService.Listar(Filtro, tipoUsuario.Value, empresaId.Value);
            PendentesPorId = _orcamentoService.ContarPendentesPorOrcamento(tipoUsuario.Value, empresaId.Value);
            Empresas = _empresaService.Listar();
            NomesPorId = _usuarioService.Listar().ToDictionary(u => u.Id, u => u.Nome);
            EmpresaPorId = Empresas.ToDictionary(e => e.Id, e => e.Nome);
            return Page();
        }
    }
}
