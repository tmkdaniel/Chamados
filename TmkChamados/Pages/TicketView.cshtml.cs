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
        private readonly IProtecaoEnvioDuplicadoService _protecaoEnvioDuplicado;

        public TicketViewModel(ITicketService ticketService, IAndamentoService andamentoService, IUsuarioService usuarioService, IProtecaoEnvioDuplicadoService protecaoEnvioDuplicado)
        {
            _ticketService = ticketService;
            _andamentoService = andamentoService;
            _usuarioService = usuarioService;
            _protecaoEnvioDuplicado = protecaoEnvioDuplicado;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public string? TokenEnvioAndamento { get; set; }

        [BindProperty]
        public string? NovoAndamentoTexto { get; set; }

        [BindProperty]
        public IFormFile? NovoAndamentoAnexo { get; set; }

        [BindProperty]
        public TicketEdicaoFormModel Form { get; set; } = new();

        public Ticket? Ticket { get; private set; }

        public IReadOnlyList<Andamento> Andamentos { get; private set; } = Array.Empty<Andamento>();

        public IReadOnlyDictionary<int, string> NomesAnexosPorAndamentoId { get; private set; } = new Dictionary<int, string>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        public IReadOnlyList<Usuario> UsuariosMaster { get; private set; } = Array.Empty<Usuario>();

        public IReadOnlyList<Usuario> UsuariosTodos { get; private set; } = Array.Empty<Usuario>();

        public bool IsMaster { get; private set; }

        public async Task<IActionResult> OnGetAsync()
        {
            var acesso = await VerificarAcessoAsync();
            if (acesso is not null)
            {
                return acesso;
            }

            TokenEnvioAndamento = _protecaoEnvioDuplicado.GerarToken();
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
            }

            ValidarAnexo();

            if (!ModelState.IsValid)
            {
                CarregarDados();
                PreencherFormDeEdicao();
                return Page();
            }

            // Protege contra duplo clique: a segunda submissão com o mesmo token é ignorada
            // silenciosamente (ver design.md - Decisão 3), em vez de criar um segundo andamento.
            if (!_protecaoEnvioDuplicado.TentarRegistrar(TokenEnvioAndamento))
            {
                return RedirectToPage(new { Id });
            }

            byte[]? anexoConteudo = null;
            if (NovoAndamentoAnexo is not null)
            {
                using var stream = new MemoryStream();
                await NovoAndamentoAnexo.CopyToAsync(stream);
                anexoConteudo = stream.ToArray();
            }

            _andamentoService.Criar(Id, NovoAndamentoTexto!, usuarioId, anexoConteudo, NovoAndamentoAnexo?.FileName, NovoAndamentoAnexo?.ContentType);
            return RedirectToPage(new { Id });
        }

        private void ValidarAnexo()
        {
            if (NovoAndamentoAnexo is null)
            {
                return;
            }

            var extensao = Path.GetExtension(NovoAndamentoAnexo.FileName);
            if (string.IsNullOrEmpty(extensao) || !AnexoAndamentoRegras.ExtensoesAceitas.Contains(extensao))
            {
                ModelState.AddModelError(nameof(NovoAndamentoAnexo), "Tipo de arquivo não permitido. Tipos aceitos: PDF, JPEG, JPG, PNG, BMP.");
                return;
            }

            if (NovoAndamentoAnexo.Length > AnexoAndamentoRegras.TamanhoMaximoBytes)
            {
                ModelState.AddModelError(nameof(NovoAndamentoAnexo), "O anexo excede o tamanho máximo permitido de 50MB.");
            }
        }

        public async Task<IActionResult> OnPostAtualizarAsync()
        {
            var acesso = await VerificarAcessoAsync();
            if (acesso is not null)
            {
                return acesso;
            }

            var ticketExistente = _ticketService.Obter(Id)!;

            // Status, Responsável, Prioridade e Data de Prazo só podem ser alterados por Master
            // (ver design.md - Decisão 3 de renomear-chamado-para-ticket e Decisão 1 de
            // restringir-status-responsavel-master): para quem não é Master, o servidor ignora o
            // que veio no post e preserva os valores atuais, em vez de confiar no campo estar
            // oculto/desabilitado na UI.
            if (!IsMaster)
            {
                Form.Status = ticketExistente.Status;
                Form.ResponsavelId = ticketExistente.ResponsavelId;
                Form.Prioridade = ticketExistente.Prioridade;
                Form.DataPrazo = ticketExistente.DataPrazo;
                Form.CriadoPorId = ticketExistente.CriadoPorId;

                // Prioridade e Criado por não são enviados no POST quando exibidos como somente
                // leitura (fora do Form vinculado), então o model binding os marca como ausentes
                // e o [Required] acima falha antes deste bloco rodar; removidos aqui porque os
                // valores já foram restaurados logo acima.
                ModelState.Remove($"{nameof(Form)}.{nameof(Form.Prioridade)}");
                ModelState.Remove($"{nameof(Form)}.{nameof(Form.CriadoPorId)}");
            }

            ValidarResponsavelMaster();
            ValidarCriadoPor();

            if (!ModelState.IsValid)
            {
                CarregarDados();
                return Page();
            }

            var usuarioQueAlterouId = User.ObterUsuarioId()!.Value;
            _ticketService.Atualizar(Id, Form.Classificacao!.Value, Form.Titulo, Form.Descricao ?? string.Empty, Form.Status, Form.Prioridade!.Value, Form.DataPrazo, Form.CriadoPorId!.Value, Form.ResponsavelId, usuarioQueAlterouId);

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
            NomesAnexosPorAndamentoId = _andamentoService.ObterNomesAnexosPorTicket(Id);

            var usuarios = _usuarioService.Listar();
            NomesPorId = usuarios.ToDictionary(u => u.Id, u => u.Nome);
            UsuariosMaster = usuarios.Where(u => u.Tipo == TipoUsuario.Master).ToList();
            UsuariosTodos = usuarios;
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
                ResponsavelId = Ticket.ResponsavelId,
                CriadoPorId = Ticket.CriadoPorId
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

        private void ValidarCriadoPor()
        {
            if (!Form.CriadoPorId.HasValue || _usuarioService.Obter(Form.CriadoPorId.Value) is null)
            {
                ModelState.AddModelError(nameof(Form.CriadoPorId), "O Criado por deve ser um usuário cadastrado.");
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

        [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "O Criado por é obrigatório.")]
        public int? CriadoPorId { get; set; }
    }
}
