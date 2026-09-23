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

        public IReadOnlyList<Usuario> Usuarios { get; private set; } = Array.Empty<Usuario>();

        public bool EmEdicao => Form.Id.HasValue;

        public void OnGet(int? editId)
        {
            Usuarios = _usuarioService.Listar();

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
                Usuarios = _usuarioService.Listar();
                return Page();
            }

            _chamadoService.Criar(Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, Form.CriadoPorId!.Value, Form.ResponsavelId!.Value);
            return RedirectToPage("/Index");
        }

        public IActionResult OnPostAtualizar()
        {
            ValidarUsuariosSelecionados();

            if (!ModelState.IsValid)
            {
                Usuarios = _usuarioService.Listar();
                return Page();
            }

            if (Form.Id.HasValue)
            {
                _chamadoService.Atualizar(Form.Id.Value, Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, Form.CriadoPorId!.Value, Form.ResponsavelId!.Value);
            }

            return RedirectToPage("/Index");
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
}
