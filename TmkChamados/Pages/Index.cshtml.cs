using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class IndexModel : PageModel
    {
        private readonly IChamadoService _chamadoService;

        public IndexModel(IChamadoService chamadoService)
        {
            _chamadoService = chamadoService;
        }

        [BindProperty]
        public ChamadoFormModel Form { get; set; } = new();

        public IReadOnlyList<Chamado> Chamados { get; private set; } = Array.Empty<Chamado>();

        public bool EmEdicao => Form.Id.HasValue;

        public void OnGet(int? editId)
        {
            Chamados = _chamadoService.Listar();

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
                        Status = chamado.Status
                    };
                }
            }
        }

        public IActionResult OnPostAdicionar()
        {
            if (!ModelState.IsValid)
            {
                Chamados = _chamadoService.Listar();
                return Page();
            }

            _chamadoService.Criar(Form.Titulo, Form.Descricao ?? string.Empty, Form.Status);
            return RedirectToPage();
        }

        public IActionResult OnPostAtualizar()
        {
            if (!ModelState.IsValid)
            {
                Chamados = _chamadoService.Listar();
                return Page();
            }

            if (Form.Id.HasValue)
            {
                _chamadoService.Atualizar(Form.Id.Value, Form.Titulo, Form.Descricao ?? string.Empty, Form.Status);
            }

            return RedirectToPage();
        }

        public IActionResult OnPostExcluir(int id)
        {
            _chamadoService.Excluir(id);
            return RedirectToPage();
        }
    }

    public class ChamadoFormModel
    {
        public int? Id { get; set; }

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string? Descricao { get; set; } = string.Empty;

        public StatusChamado Status { get; set; } = StatusChamado.Aberto;
    }
}
