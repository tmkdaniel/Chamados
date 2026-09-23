using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IChamadoService _chamadoService;
        private readonly IUsuarioService _usuarioService;

        public IndexModel(IChamadoService chamadoService, IUsuarioService usuarioService)
        {
            _chamadoService = chamadoService;
            _usuarioService = usuarioService;
        }

        [BindProperty(SupportsGet = true)]
        public FiltroFormModel Filtro { get; set; } = new();

        public IReadOnlyList<Chamado> Chamados { get; private set; } = Array.Empty<Chamado>();

        public IReadOnlyList<Usuario> Usuarios { get; private set; } = Array.Empty<Usuario>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public void OnGet()
        {
            Chamados = _chamadoService.Listar(ConstruirFiltro());
            Usuarios = _usuarioService.Listar();
            NomesPorId = Usuarios.ToDictionary(u => u.Id, u => u.Nome);
        }

        public IActionResult OnPostExcluir(int id)
        {
            _chamadoService.Excluir(id);
            return RedirectToPage();
        }

        private FiltroChamados ConstruirFiltro()
        {
            return new FiltroChamados
            {
                Status = Filtro.Status,
                CriadoPorId = Filtro.CriadoPorId,
                ResponsavelId = Filtro.ResponsavelId,
                CriadoDe = Filtro.CriadoDe,
                CriadoAte = Filtro.CriadoAte,
                ModificadoDe = Filtro.ModificadoDe,
                ModificadoAte = Filtro.ModificadoAte
            };
        }
    }

    public class FiltroFormModel
    {
        public StatusChamado? Status { get; set; }

        public int? CriadoPorId { get; set; }

        public int? ResponsavelId { get; set; }

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
