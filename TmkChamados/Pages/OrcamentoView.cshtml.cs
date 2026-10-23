using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Models;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class OrcamentoViewModel : PageModel
    {
        private readonly IOrcamentoService _orcamentoService;
        private readonly IUsuarioService _usuarioService;
        private readonly IEmpresaService _empresaService;
        private readonly IProtecaoEnvioDuplicadoService _protecaoEnvioDuplicado;

        public OrcamentoViewModel(IOrcamentoService orcamentoService, IUsuarioService usuarioService, IEmpresaService empresaService, IProtecaoEnvioDuplicadoService protecaoEnvioDuplicado)
        {
            _orcamentoService = orcamentoService;
            _usuarioService = usuarioService;
            _empresaService = empresaService;
            _protecaoEnvioDuplicado = protecaoEnvioDuplicado;
        }

        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [TempData]
        public string? Erro { get; set; }

        [TempData]
        public string? Mensagem { get; set; }

        public Orcamento? Orcamento { get; private set; }

        public IReadOnlyList<AnexoOrcamentoResumo> Anexos { get; private set; } = Array.Empty<AnexoOrcamentoResumo>();

        public string NomeAutor { get; private set; } = "-";

        public string NomeEmpresa { get; private set; } = "-";

        // Campos de edição do Rascunho; preenchidos a partir do orçamento ou do que o usuário digitou.
        public string? TituloEdicao { get; private set; }

        public string? DescricaoEdicao { get; private set; }

        public string? ErroEdicao { get; private set; }

        public IReadOnlyList<VersaoOrcamento> Versoes { get; private set; } = Array.Empty<VersaoOrcamento>();

        public IReadOnlyList<HistoricoOrcamento> Historico { get; private set; } = Array.Empty<HistoricoOrcamento>();

        public Dictionary<int, string> NomesPorId { get; private set; } = new();

        // Campos do modal de cotação; reabertos com o que foi digitado quando a validação falha.
        public string? ValorCotacao { get; private set; }

        public string? PrazoCotacao { get; private set; }

        public string? ValidadeCotacao { get; private set; }

        public string? ObservacaoCotacao { get; private set; }

        public string? ErroCotacao { get; private set; }

        public IReadOnlyList<ConversaPergunta> Conversa { get; private set; } = Array.Empty<ConversaPergunta>();

        public int PerguntasPendentes { get; private set; }

        // Um token por formulário de mensagem (nova pergunta e uma resposta por conversa), gerado a cada
        // carregamento: protege contra duplo clique.
        public string TokenPergunta { get; private set; } = string.Empty;

        public Dictionary<int, string> TokensResposta { get; private set; } = new();

        // Texto e erro reexibidos quando a validação rejeita a mensagem; RespostaPerguntaId indica a conversa.
        public string? TextoPergunta { get; private set; }

        public string? ErroPergunta { get; private set; }

        public int? RespostaPerguntaId { get; private set; }

        public string? TextoResposta { get; private set; }

        public string? ErroResposta { get; private set; }

        // Master e Gerentes da empresa conversam enquanto o orçamento está em negociação; depois, só consulta.
        public bool PodeConversar => (EhMaster || Gerencia) && Orcamento?.Status is StatusOrcamento.AguardandoCotacao
            or StatusOrcamento.AjusteSolicitado or StatusOrcamento.Cotado;

        public bool PodeEditar => Orcamento?.Status == StatusOrcamento.Rascunho && Gerencia;

        public bool PodeCancelar => Gerencia && Orcamento?.Status is StatusOrcamento.AguardandoCotacao
            or StatusOrcamento.Cotado or StatusOrcamento.AjusteSolicitado;

        // Ajuste e recusa valem também com a cotação expirada; aprovar não.
        public bool PodeDecidir => Gerencia && Orcamento?.Status == StatusOrcamento.Cotado;

        public bool PodeAprovar => PodeDecidir && Orcamento is { Expirado: false };

        public bool PodeCotar => EhMaster && Orcamento?.Status is StatusOrcamento.AguardandoCotacao
            or StatusOrcamento.AjusteSolicitado or StatusOrcamento.Cotado;

        public bool EhMaster { get; private set; }

        private bool Gerencia { get; set; }

        private int UsuarioIdLogado => User.ObterUsuarioId()!.Value;

        private int EmpresaIdUsuario => User.ObterEmpresaId()!.Value;

        public IActionResult OnGet()
        {
            var acesso = Preparar(exigeGerente: false);
            if (acesso is not null)
            {
                return acesso;
            }

            if (Orcamento is not null)
            {
                TituloEdicao = Orcamento.Titulo;
                DescricaoEdicao = Orcamento.Descricao;
            }

            return Page();
        }

        public IActionResult OnPostAtualizar(string? titulo, string? descricao)
        {
            var acesso = Preparar(exigeGerente: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            if (string.IsNullOrWhiteSpace(titulo))
            {
                return ReexibirEdicao(titulo, descricao, "O título é obrigatório.");
            }

            if (titulo.Trim().Length > 200)
            {
                return ReexibirEdicao(titulo, descricao, "O título deve ter no máximo 200 caracteres.");
            }

            if (string.IsNullOrWhiteSpace(descricao))
            {
                return ReexibirEdicao(titulo, descricao, "A descrição é obrigatória.");
            }

            if (!_orcamentoService.Atualizar(Id, titulo, descricao, EmpresaIdUsuario))
            {
                Erro = "Não foi possível alterar este orçamento: ele não está mais em Rascunho.";
            }
            else
            {
                Mensagem = "Orçamento atualizado.";
            }

            return RedirectToPage(new { Id });
        }

        public async Task<IActionResult> OnPostAdicionarAnexoAsync(IFormFile? arquivo)
        {
            var acesso = Preparar(exigeGerente: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            if (arquivo is null || arquivo.Length == 0)
            {
                Erro = "Selecione um arquivo para anexar.";
                return RedirectToPage(new { Id });
            }

            if (arquivo.Length > AnexoAndamentoRegras.TamanhoMaximoBytes)
            {
                Erro = "O arquivo excede o tamanho máximo de 50MB.";
                return RedirectToPage(new { Id });
            }

            using var stream = new MemoryStream();
            await arquivo.CopyToAsync(stream);

            var erroAnexo = _orcamentoService.AdicionarAnexo(Id, EmpresaIdUsuario, arquivo.FileName, arquivo.ContentType, stream.ToArray());
            if (erroAnexo is not null)
            {
                Erro = erroAnexo;
            }
            else
            {
                Mensagem = "Anexo adicionado.";
            }

            return RedirectToPage(new { Id });
        }

        public IActionResult OnPostRemoverAnexo(int anexoId)
        {
            var acesso = Preparar(exigeGerente: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            if (!_orcamentoService.RemoverAnexo(Id, anexoId, EmpresaIdUsuario))
            {
                Erro = "Não foi possível remover o anexo.";
            }

            return RedirectToPage(new { Id });
        }

        public IActionResult OnPostEnviar()
        {
            var acesso = Preparar(exigeGerente: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            // Duplo clique: o segundo envio não encontra mais Rascunho; se o orçamento já está
            // em Aguardando cotação, trata como sucesso silencioso.
            if (_orcamentoService.Enviar(Id, EmpresaIdUsuario, UsuarioIdLogado))
            {
                Mensagem = "Orçamento enviado ao Master.";
            }
            else if (_orcamentoService.Obter(Id)?.Status != StatusOrcamento.AguardandoCotacao)
            {
                Erro = "Não foi possível enviar este orçamento: ele não está em Rascunho.";
            }

            return RedirectToPage(new { Id });
        }

        public IActionResult OnPostExcluir()
        {
            var acesso = Preparar(exigeGerente: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            if (!_orcamentoService.Excluir(Id, EmpresaIdUsuario))
            {
                Erro = "Não foi possível excluir este orçamento: ele não está em Rascunho.";
                return RedirectToPage(new { Id });
            }

            return RedirectToPage("/Orcamentos");
        }

        public IActionResult OnPostCancelar()
        {
            var acesso = Preparar(exigeGerente: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            if (!_orcamentoService.Cancelar(Id, EmpresaIdUsuario, UsuarioIdLogado))
            {
                Erro = "Não foi possível cancelar este orçamento: ele já foi decidido ou cancelado, ou ainda é um rascunho.";
            }
            else
            {
                Mensagem = "Orçamento cancelado.";
            }

            return RedirectToPage(new { Id });
        }

        public IActionResult OnPostCotar(int? versaoVista, string? valor, string? prazo, string? validade, string? observacao)
        {
            var acesso = Preparar(exigeGerente: false, exigeMaster: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            if (!PodeCotar)
            {
                Erro = "Não é possível cotar este orçamento no status atual.";
                return RedirectToPage(new { Id });
            }

            var pt = CultureInfo.GetCultureInfo("pt-BR");
            if (!decimal.TryParse(valor?.Trim(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, pt, out var valorNumero))
            {
                return ReexibirCotacao(valor, prazo, validade, observacao, "Informe um valor válido, por exemplo 1.500,00.");
            }

            if (!int.TryParse(prazo?.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out var prazoDias))
            {
                return ReexibirCotacao(valor, prazo, validade, observacao, "Informe o prazo em dias (número inteiro maior que zero).");
            }

            if (!DateTime.TryParseExact(validade?.Trim(), "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var validadeData))
            {
                return ReexibirCotacao(valor, prazo, validade, observacao, "Informe a validade da cotação.");
            }

            var resultado = _orcamentoService.Cotar(Id, versaoVista, valorNumero, prazoDias, validadeData, observacao, UsuarioIdLogado);
            if (!resultado.Sucesso)
            {
                return ReexibirCotacao(valor, prazo, validade, observacao, resultado.Erro ?? "Não foi possível cotar.");
            }

            if (!resultado.Repetido)
            {
                Mensagem = "Cotação registrada.";
            }

            return RedirectToPage(new { Id });
        }

        public IActionResult OnPostPerguntar(string? texto, string? tokenEnvio)
        {
            return Conversar(null, texto, tokenEnvio, "Pergunta enviada.");
        }

        public IActionResult OnPostResponder(int perguntaId, string? texto, string? tokenEnvio)
        {
            return Conversar(perguntaId, texto, tokenEnvio, "Resposta enviada.");
        }

        private IActionResult Conversar(int? perguntaId, string? texto, string? tokenEnvio, string mensagemSucesso)
        {
            var acesso = Preparar(exigeGerente: false);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            if (!PodeConversar)
            {
                Erro = "Não é possível registrar mensagens neste orçamento no status atual.";
                return RedirectToPage(new { Id });
            }

            var conteudo = texto?.Trim();
            if (string.IsNullOrEmpty(conteudo) || conteudo.Length > PerguntaOrcamento.TamanhoMaximoTexto)
            {
                var erro = string.IsNullOrEmpty(conteudo)
                    ? "Escreva o texto da mensagem."
                    : $"A mensagem pode ter no máximo {PerguntaOrcamento.TamanhoMaximoTexto} caracteres.";
                if (perguntaId is null)
                {
                    TextoPergunta = texto;
                    ErroPergunta = erro;
                }
                else
                {
                    RespostaPerguntaId = perguntaId;
                    TextoResposta = texto;
                    ErroResposta = erro;
                }

                return Page();
            }

            // Duplo clique: o token já foi usado pelo primeiro envio; ignora sem erro.
            if (!_protecaoEnvioDuplicado.TentarRegistrar(tokenEnvio))
            {
                return RedirectToPage(new { Id });
            }

            var tipoUsuario = User.ObterTipoUsuario()!.Value;
            var resultado = perguntaId is null
                ? _orcamentoService.Perguntar(Id, tipoUsuario, EmpresaIdUsuario, UsuarioIdLogado, conteudo)
                : _orcamentoService.Responder(Id, perguntaId.Value, tipoUsuario, EmpresaIdUsuario, UsuarioIdLogado, conteudo);
            if (!resultado.Sucesso)
            {
                Erro = resultado.Erro;
            }
            else
            {
                Mensagem = mensagemSucesso;
            }

            return RedirectToPage(new { Id });
        }

        public IActionResult OnPostSolicitarAjuste(string? motivo)
        {
            return Decidir(() => _orcamentoService.SolicitarAjuste(Id, EmpresaIdUsuario, UsuarioIdLogado, motivo), "Ajuste solicitado ao Master.");
        }

        public IActionResult OnPostRecusar(string? motivo)
        {
            return Decidir(() => _orcamentoService.Recusar(Id, EmpresaIdUsuario, UsuarioIdLogado, motivo), "Orçamento recusado.");
        }

        public IActionResult OnPostAprovar(int versaoVista, string? comentario)
        {
            return Decidir(() => _orcamentoService.Aprovar(Id, EmpresaIdUsuario, UsuarioIdLogado, versaoVista, comentario), "Orçamento aprovado.");
        }

        private IActionResult Decidir(Func<ResultadoOrcamento> operacao, string mensagemSucesso)
        {
            var acesso = Preparar(exigeGerente: true);
            if (acesso is not null || Orcamento is null)
            {
                return acesso ?? Page();
            }

            var resultado = operacao();
            if (!resultado.Sucesso)
            {
                Erro = resultado.Erro;
            }
            else if (!resultado.Repetido)
            {
                Mensagem = mensagemSucesso;
            }

            return RedirectToPage(new { Id });
        }

        public static string FormatarValor(decimal valor)
        {
            return valor.ToString("C", CultureInfo.GetCultureInfo("pt-BR"));
        }

        public string NomeUsuario(int usuarioId)
        {
            return NomesPorId.TryGetValue(usuarioId, out var nome) ? nome : "-";
        }

        public static string FormatarTamanho(long bytes)
        {
            return bytes >= 1024 * 1024
                ? $"{bytes / (1024.0 * 1024.0):0.0} MB"
                : $"{Math.Max(1, bytes / 1024)} KB";
        }

        private IActionResult ReexibirCotacao(string? valor, string? prazo, string? validade, string? observacao, string erro)
        {
            ValorCotacao = valor;
            PrazoCotacao = prazo;
            ValidadeCotacao = validade;
            ObservacaoCotacao = observacao;
            ErroCotacao = erro;
            return Page();
        }

        private IActionResult ReexibirEdicao(string? titulo, string? descricao, string erro)
        {
            TituloEdicao = titulo;
            DescricaoEdicao = descricao;
            ErroEdicao = erro;
            return Page();
        }

        // Valida tipo de usuário, existência e acesso ao orçamento e carrega os dados da tela.
        // Retorna null quando a requisição pode prosseguir. Orçamento inexistente devolve a própria
        // página (que exibe "não encontrado"); falta de acesso devolve Forbid.
        private IActionResult? Preparar(bool exigeGerente, bool exigeMaster = false)
        {
            var tipoUsuario = User.ObterTipoUsuario();
            var empresaId = User.ObterEmpresaId();
            if (!User.EhMasterOuGerente() || tipoUsuario is null || empresaId is null)
            {
                return Forbid();
            }

            Orcamento = _orcamentoService.Obter(Id);
            if (Orcamento is null)
            {
                if (exigeMaster && tipoUsuario.Value != TipoUsuario.Master)
                {
                    return Forbid();
                }

                return exigeGerente && tipoUsuario.Value != TipoUsuario.Gerente ? Forbid() : Page();
            }

            if (!_orcamentoService.PodeAcessar(Orcamento, tipoUsuario.Value, empresaId.Value))
            {
                return Forbid();
            }

            EhMaster = tipoUsuario.Value == TipoUsuario.Master;
            if (exigeMaster && !EhMaster)
            {
                return Forbid();
            }

            Gerencia = _orcamentoService.PodeGerenciar(Orcamento, tipoUsuario.Value, empresaId.Value);
            if (exigeGerente && !Gerencia)
            {
                return Forbid();
            }

            CarregarDados();
            return null;
        }

        private void CarregarDados()
        {
            if (Orcamento is null)
            {
                return;
            }

            Anexos = _orcamentoService.ListarAnexos(Orcamento.Id);
            Versoes = _orcamentoService.ListarVersoes(Orcamento.Id);
            Historico = _orcamentoService.ListarHistorico(Orcamento.Id);
            Conversa = _orcamentoService.ListarConversa(Orcamento.Id);
            PerguntasPendentes = Conversa.Count(c => c.Pendente);
            TokenPergunta = _protecaoEnvioDuplicado.GerarToken();
            TokensResposta = Conversa.ToDictionary(c => c.Pergunta.Id, _ => _protecaoEnvioDuplicado.GerarToken());
            NomesPorId = _usuarioService.Listar().ToDictionary(u => u.Id, u => u.Nome);
            NomeAutor = _usuarioService.Obter(Orcamento.CriadoPorId)?.Nome ?? "-";
            NomeEmpresa = _empresaService.Obter(Orcamento.EmpresaId)?.Nome ?? "-";
        }
    }
}
