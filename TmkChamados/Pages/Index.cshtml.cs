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

        [BindProperty]
        public ChamadoFormModel Form { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public FiltroFormModel Filtro { get; set; } = new();

        public IReadOnlyList<Chamado> Chamados { get; private set; } = Array.Empty<Chamado>();

        public IReadOnlyList<Usuario> Usuarios { get; private set; } = Array.Empty<Usuario>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public bool EmEdicao => Form.Id.HasValue;

        public void OnGet(int? editId)
        {
            Chamados = _chamadoService.Listar(ConstruirFiltro());
            Usuarios = _usuarioService.Listar();
            NomesPorId = Usuarios.ToDictionary(u => u.Id, u => u.Nome);

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
                        CriadoPorId = chamado.CriadoPorId,
                        ResponsavelId = chamado.ResponsavelId
                    };
                }
            }
        }

        public IActionResult OnPostAdicionar()
        {
            ValidarUsuariosSelecionados();

            if (!ModelState.IsValid)
            {
                Chamados = _chamadoService.Listar(ConstruirFiltro());
                Usuarios = _usuarioService.Listar();
                NomesPorId = Usuarios.ToDictionary(u => u.Id, u => u.Nome);
                return Page();
            }

            _chamadoService.Criar(Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, Form.CriadoPorId!.Value, Form.ResponsavelId!.Value);
            return RedirectToPage();
        }

        public IActionResult OnPostAtualizar()
        {
            ValidarUsuariosSelecionados();

            if (!ModelState.IsValid)
            {
                Chamados = _chamadoService.Listar(ConstruirFiltro());
                Usuarios = _usuarioService.Listar();
                NomesPorId = Usuarios.ToDictionary(u => u.Id, u => u.Nome);
                return Page();
            }

            if (Form.Id.HasValue)
            {
                _chamadoService.Atualizar(Form.Id.Value, Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, Form.CriadoPorId!.Value, Form.ResponsavelId!.Value);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            _chamadoService.Excluir(id);
            return RedirectToPage();
        }

        private void ValidarUsuariosSelecionados()
        {
            if (!Form.CriadoPorId.HasValue)
            {
                ModelState.AddModelError(nameof(Form.CriadoPorId), "Selecione o usuário Criado por.");
            }

            if (!Form.ResponsavelId.HasValue)
            {
                ModelState.AddModelError(nameof(Form.ResponsavelId), "Selecione o usuário Responsável.");
            }
        }

        private FiltroChamados ConstruirFiltro()
        {
            return new FiltroChamados
            {
                Status = Filtro.Status,
                CriadoDe = Filtro.CriadoDe,
                CriadoAte = Filtro.CriadoAte,
                ModificadoDe = Filtro.ModificadoDe,
                ModificadoAte = Filtro.ModificadoAte
            };
        }
    }

    public class ChamadoFormModel
    {
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; } = string.Empty;

        public StatusChamado Status { get; set; } = StatusChamado.Aberto;

        public int? CriadoPorId { get; set; }

        public int? ResponsavelId { get; set; }
    }

    public class FiltroFormModel
    {
        public StatusChamado? Status { get; set; }

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
