using System.Globalization;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using TmkChamados.Data;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public class OrcamentoService : IOrcamentoService
    {
        private readonly TmkChamadosDbContext _dbContext;
        private readonly ITicketService _ticketService;
        private readonly ILogger<OrcamentoService> _logger;
        private readonly INotificacaoOrcamentoEmailService _notificacao;

        public OrcamentoService(TmkChamadosDbContext dbContext, ITicketService ticketService, ILogger<OrcamentoService> logger, INotificacaoOrcamentoEmailService notificacao)
        {
            _notificacao = notificacao;
            _dbContext = dbContext;
            _ticketService = ticketService;
            _logger = logger;
        }

        // Falha ao gerar o ticket da aprovação: a transação não é confirmada e a aprovação não acontece.
        private sealed class FalhaAoGerarTicketException : Exception
        {
            public FalhaAoGerarTicketException(Exception causa) : base("Falha ao gerar o ticket do orçamento aprovado.", causa)
            {
            }
        }

        public IReadOnlyList<Orcamento> Listar(FiltroOrcamentos filtro, TipoUsuario tipoUsuario, int empresaId)
        {
            IQueryable<Orcamento> query = _dbContext.Orcamentos;

            switch (tipoUsuario)
            {
                case TipoUsuario.Gerente:
                    query = query.Where(o => o.EmpresaId == empresaId);
                    break;
                case TipoUsuario.Master:
                    query = query.Where(o => o.Status != StatusOrcamento.Rascunho);
                    break;
                default:
                    return Array.Empty<Orcamento>();
            }

            if (filtro.Status.HasValue)
            {
                var status = filtro.Status.Value;
                query = query.Where(o => o.Status == status);

                // Expirado é Cotado com validade vencida; Cotado traz só os de validade em vigor.
                if (status == StatusOrcamento.Cotado)
                {
                    var hoje = DateTime.Today;
                    query = filtro.Expirado
                        ? query.Where(o => o.ValidadeAtual < hoje)
                        : query.Where(o => o.ValidadeAtual >= hoje);
                }
            }

            if (filtro.CriadoDe.HasValue)
            {
                var de = filtro.CriadoDe.Value.Date;
                query = query.Where(o => o.DataCriacao >= de);
            }

            if (filtro.CriadoAte.HasValue)
            {
                var ateExclusivo = filtro.CriadoAte.Value.Date.AddDays(1);
                query = query.Where(o => o.DataCriacao < ateExclusivo);
            }

            if (!string.IsNullOrWhiteSpace(filtro.Titulo))
            {
                var trecho = filtro.Titulo.Trim();
                query = query.Where(o => o.Titulo.Contains(trecho));
            }

            // Somente Master filtra por empresa; para Gerente a empresa já está fixada acima.
            if (tipoUsuario == TipoUsuario.Master && filtro.EmpresaId.HasValue)
            {
                query = query.Where(o => o.EmpresaId == filtro.EmpresaId.Value);
            }

            return query
                .OrderByDescending(o => o.DataCriacao)
                .ThenByDescending(o => o.Id)
                .ToList();
        }

        public Orcamento? Obter(int id)
        {
            // Sem rastreamento: as transições de status usam UPDATE direto no banco, então uma
            // entidade rastreada ficaria desatualizada em leituras seguintes da mesma requisição.
            return _dbContext.Orcamentos.AsNoTracking().FirstOrDefault(o => o.Id == id);
        }

        public bool PodeAcessar(Orcamento orcamento, TipoUsuario tipoUsuario, int empresaId)
        {
            return tipoUsuario switch
            {
                TipoUsuario.Gerente => orcamento.EmpresaId == empresaId,
                TipoUsuario.Master => orcamento.Status != StatusOrcamento.Rascunho,
                _ => false
            };
        }

        public bool PodeGerenciar(Orcamento orcamento, TipoUsuario tipoUsuario, int empresaId)
        {
            return tipoUsuario == TipoUsuario.Gerente && orcamento.EmpresaId == empresaId;
        }

        public Orcamento Criar(string titulo, string descricao, int empresaId, int criadoPorId)
        {
            var orcamento = new Orcamento
            {
                Titulo = titulo.Trim(),
                Descricao = descricao.Trim(),
                Status = StatusOrcamento.Rascunho,
                EmpresaId = empresaId,
                CriadoPorId = criadoPorId,
                DataCriacao = DateTime.Now
            };

            _dbContext.Orcamentos.Add(orcamento);
            _dbContext.SaveChanges();
            return orcamento;
        }

        public bool Atualizar(int id, string titulo, string descricao, int empresaId)
        {
            var tituloLimpo = titulo.Trim();
            var descricaoLimpa = descricao.Trim();

            return _dbContext.Orcamentos
                .Where(o => o.Id == id && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Rascunho)
                .ExecuteUpdate(s => s
                    .SetProperty(o => o.Titulo, tituloLimpo)
                    .SetProperty(o => o.Descricao, descricaoLimpa)) > 0;
        }

        public bool Excluir(int id, int empresaId)
        {
            // Os anexos saem junto por cascata de FK no banco.
            return _dbContext.Orcamentos
                .Where(o => o.Id == id && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Rascunho)
                .ExecuteDelete() > 0;
        }

        public bool Enviar(int id, int empresaId, int usuarioId)
        {
            var agora = DateTime.Now;

            // UPDATE condicional pelo status esperado: duplo clique ou envio concorrente
            // produzem exatamente um efeito (ver design.md - Decisão 5).
            var feito = ComEvento(
                () => _dbContext.Orcamentos
                    .Where(o => o.Id == id && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Rascunho)
                    .ExecuteUpdate(s => s
                        .SetProperty(o => o.Status, StatusOrcamento.AguardandoCotacao)
                        .SetProperty(o => o.DataEnvio, agora)),
                NovoEvento(id, TipoEventoOrcamento.Enviado, usuarioId, agora));
            if (feito)
            {
                _notificacao.Notificar(id, EventoEmailOrcamento.Enviado, usuarioId);
            }

            return feito;
        }

        public bool Cancelar(int id, int empresaId, int usuarioId)
        {

            var feito = ComEvento(
                () => _dbContext.Orcamentos
                    .Where(o => o.Id == id && o.EmpresaId == empresaId && (o.Status == StatusOrcamento.AguardandoCotacao || o.Status == StatusOrcamento.Cotado || o.Status == StatusOrcamento.AjusteSolicitado))
                    .ExecuteUpdate(s => s.SetProperty(o => o.Status, StatusOrcamento.Cancelado)),
                NovoEvento(id, TipoEventoOrcamento.Cancelado, usuarioId, DateTime.Now));
            if (feito)
            {
                _notificacao.Notificar(id, EventoEmailOrcamento.Cancelado, usuarioId);
            }

            return feito;
        }

        public ResultadoOrcamento Cotar(int id, int? versaoVista, decimal valor, int prazoDias, DateTime validade, string? observacao, int masterId)
        {
            if (valor <= 0 || decimal.Round(valor, 2) != valor || valor >= 10_000_000_000_000m)
            {
                return ResultadoOrcamento.Falha("Informe um valor maior que zero, com no máximo duas casas decimais.");
            }

            if (prazoDias < 1)
            {
                return ResultadoOrcamento.Falha("Informe um prazo de pelo menos 1 dia.");
            }

            var dataValidade = validade.Date;
            if (dataValidade < DateTime.Today)
            {
                return ResultadoOrcamento.Falha("A validade não pode ser anterior à data de hoje.");
            }

            var obs = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
            var cotaveis = new[] { StatusOrcamento.AguardandoCotacao, StatusOrcamento.AjusteSolicitado, StatusOrcamento.Cotado };

            using var transacao = _dbContext.Database.BeginTransaction();

            var atual = _dbContext.Orcamentos.AsNoTracking()
                .Where(o => o.Id == id)
                .Select(o => new { o.Status, o.VersaoAtual })
                .FirstOrDefault();
            if (atual is null || !cotaveis.Contains(atual.Status))
            {
                return ResultadoOrcamento.Falha("Não é possível cotar este orçamento no status atual.");
            }

            // A cotação só vale sobre a versão que o Master viu.
            if (atual.VersaoAtual != versaoVista)
            {
                return DivergenciaDeVersao(id, versaoVista, masterId);
            }

            var versaoLida = atual.VersaoAtual;
            var numero = (versaoLida ?? 0) + 1;
            var agora = DateTime.Now;

            // O WHERE inclui a versão lida: duas cotações simultâneas não geram a mesma versão.
            var afetadas = _dbContext.Orcamentos
                .Where(o => o.Id == id && (o.Status == StatusOrcamento.AguardandoCotacao || o.Status == StatusOrcamento.AjusteSolicitado || o.Status == StatusOrcamento.Cotado) && o.VersaoAtual == versaoLida)
                .ExecuteUpdate(s => s
                    .SetProperty(o => o.Status, StatusOrcamento.Cotado)
                    .SetProperty(o => o.VersaoAtual, numero)
                    .SetProperty(o => o.ValorAtual, valor)
                    .SetProperty(o => o.PrazoAtualDias, prazoDias)
                    .SetProperty(o => o.ValidadeAtual, dataValidade));
            if (afetadas == 0)
            {
                // Perdeu a corrida para outra cotação simultânea (ex.: duplo clique).
                return DivergenciaDeVersao(id, versaoVista, masterId);
            }

            _dbContext.VersoesOrcamento.Add(new VersaoOrcamento
            {
                OrcamentoId = id,
                Numero = numero,
                Valor = valor,
                PrazoDias = prazoDias,
                Validade = dataValidade,
                Observacao = obs,
                CriadoPorId = masterId,
                DataCriacao = agora
            });
            var evento = NovoEvento(id, TipoEventoOrcamento.Cotado, masterId, agora, obs);
            evento.VersaoNumero = numero;
            _dbContext.HistoricoOrcamento.Add(evento);
            _dbContext.SaveChanges();

            transacao.Commit();
            _notificacao.Notificar(id, EventoEmailOrcamento.Cotado, masterId, new DetalheNotificacaoOrcamento(VersaoNumero: numero));
            return ResultadoOrcamento.Ok();
        }

        // A versão vigente não é a que o Master viu. Se a vigente é a seguinte e foi emitida por ele mesmo,
        // é a repetição do mesmo envio (duplo clique): sucesso silencioso. Caso contrário, pede atualização.
        private ResultadoOrcamento DivergenciaDeVersao(int id, int? versaoVista, int masterId)
        {
            var vigente = _dbContext.Orcamentos.AsNoTracking().Where(o => o.Id == id).Select(o => o.VersaoAtual).FirstOrDefault();
            var repeticao = vigente == (versaoVista ?? 0) + 1
                && _dbContext.VersoesOrcamento.Any(v => v.OrcamentoId == id && v.Numero == vigente && v.CriadoPorId == masterId);

            return repeticao
                ? new ResultadoOrcamento(true, Repetido: true)
                : ResultadoOrcamento.Falha("A cotação deste orçamento foi alterada por outra operação. Atualize a página e tente novamente.");
        }

        public ResultadoOrcamento SolicitarAjuste(int id, int empresaId, int gerenteId, string? motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ResultadoOrcamento.Falha("O motivo do ajuste é obrigatório.");
            }

            var feito = ComEvento(
                () => _dbContext.Orcamentos
                    .Where(o => o.Id == id && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Cotado)
                    .ExecuteUpdate(s => s.SetProperty(o => o.Status, StatusOrcamento.AjusteSolicitado)),
                NovoEvento(id, TipoEventoOrcamento.AjusteSolicitado, gerenteId, DateTime.Now, motivo.Trim()));

            if (feito)
            {
                _notificacao.Notificar(id, EventoEmailOrcamento.AjusteSolicitado, gerenteId);
                return ResultadoOrcamento.Ok();
            }

            return ResultadoOrcamento.Falha("Não foi possível solicitar ajuste: o orçamento não está em Cotado.");
        }

        public ResultadoOrcamento Recusar(int id, int empresaId, int gerenteId, string? motivo)
        {
            if (string.IsNullOrWhiteSpace(motivo))
            {
                return ResultadoOrcamento.Falha("O motivo da recusa é obrigatório.");
            }

            var feito = ComEvento(
                () => _dbContext.Orcamentos
                    .Where(o => o.Id == id && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Cotado)
                    .ExecuteUpdate(s => s.SetProperty(o => o.Status, StatusOrcamento.Recusado)),
                NovoEvento(id, TipoEventoOrcamento.Recusado, gerenteId, DateTime.Now, motivo.Trim()));
            if (feito)
            {
                _notificacao.Notificar(id, EventoEmailOrcamento.Recusado, gerenteId);
                return ResultadoOrcamento.Ok();
            }

            // Duplo clique: já recusado pela ação anterior.
            return _dbContext.Orcamentos.AsNoTracking().Any(o => o.Id == id && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Recusado)
                ? new ResultadoOrcamento(true, Repetido: true)
                : ResultadoOrcamento.Falha("Não foi possível recusar: o orçamento não está em Cotado.");
        }

        public ResultadoOrcamento Aprovar(int id, int empresaId, int gerenteId, int versaoVista, string? comentario)
        {
            var hoje = DateTime.Today;
            var texto = string.IsNullOrWhiteSpace(comentario) ? null : comentario.Trim();
            var evento = NovoEvento(id, TipoEventoOrcamento.Aprovado, gerenteId, DateTime.Now, texto);
            evento.VersaoNumero = versaoVista;

            // O ticket é gerado dentro da mesma transação: só quem vence o UPDATE condicional chega aqui,
            // então aprovações simultâneas geram exatamente um ticket.
            bool feito;
            int? ticketGeradoId = null;
            try
            {
                feito = ComEvento(
                    () => _dbContext.Orcamentos
                        .Where(o => o.Id == id && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Cotado
                            && o.VersaoAtual == versaoVista && o.ValidadeAtual >= hoje)
                        .ExecuteUpdate(s => s.SetProperty(o => o.Status, StatusOrcamento.Aprovado)),
                    evento,
                    () => ticketGeradoId = GerarTicketDaAprovacao(id, gerenteId, hoje));
            }
            catch (FalhaAoGerarTicketException ex)
            {
                _dbContext.ChangeTracker.Clear();
                _logger.LogError(ex.InnerException ?? ex, "Não foi possível gerar o ticket do orçamento {OrcamentoId}; a aprovação foi desfeita.", id);
                return ResultadoOrcamento.Falha("Não foi possível aprovar este orçamento agora. Tente novamente.");
            }

            if (feito)
            {
                _notificacao.Notificar(id, EventoEmailOrcamento.Aprovado, gerenteId, new DetalheNotificacaoOrcamento(TicketId: ticketGeradoId));
                return ResultadoOrcamento.Ok();
            }

            // Descobre a causa para a mensagem; aprovação duplicada é sucesso silencioso.
            var atual = _dbContext.Orcamentos.AsNoTracking().FirstOrDefault(x => x.Id == id && x.EmpresaId == empresaId);
            if (atual is null)
            {
                return ResultadoOrcamento.Falha("Orçamento não encontrado.");
            }

            if (atual.Status == StatusOrcamento.Aprovado)
            {
                return new ResultadoOrcamento(true, Repetido: true);
            }

            if (atual.Status != StatusOrcamento.Cotado)
            {
                return ResultadoOrcamento.Falha("Não foi possível aprovar: o orçamento não está em Cotado.");
            }

            if (atual.VersaoAtual != versaoVista)
            {
                return ResultadoOrcamento.Falha("A cotação foi atualizada pelo Master. Revise a cotação atual antes de decidir.");
            }

            return ResultadoOrcamento.Falha("Não foi possível aprovar: a validade da cotação venceu.");
        }

        // Cria o ticket de Mudança do orçamento aprovado e grava o vínculo. Usa o mesmo DbContext (e portanto
        // a transação aberta pela aprovação); qualquer exceção vira FalhaAoGerarTicketException.
        private int GerarTicketDaAprovacao(int orcamentoId, int gerenteId, DateTime hoje)
        {
            try
            {
                var orcamento = _dbContext.Orcamentos.AsNoTracking().First(o => o.Id == orcamentoId);
                var empresa = _dbContext.Empresas.Where(e => e.Id == orcamento.EmpresaId).Select(e => e.Nome).FirstOrDefault() ?? "-";
                var pt = CultureInfo.GetCultureInfo("pt-BR");
                var prazoDias = orcamento.PrazoAtualDias ?? 0;

                var descricao = string.Join(Environment.NewLine, new[]
                {
                    $"Gerado a partir do orçamento #{orcamento.Id}.",
                    $"Empresa: {empresa}",
                    $"Valor: {(orcamento.ValorAtual ?? 0m).ToString("C", pt)}",
                    $"Prazo: {prazoDias} dia(s)",
                    $"Validade da cotação: {orcamento.ValidadeAtual?.ToString("dd/MM/yyyy") ?? "-"}",
                    string.Empty,
                    "Descrição do orçamento:",
                    orcamento.Descricao
                });

                var ticket = _ticketService.Criar(
                    ClassificacaoTicket.Mudanca,
                    $"Orçamento #{orcamento.Id} - {orcamento.Titulo}",
                    descricao,
                    PrioridadeTicket.Normal,
                    hoje.AddDays(prazoDias),
                    gerenteId);

                _dbContext.Orcamentos.Where(o => o.Id == orcamentoId)
                    .ExecuteUpdate(s => s.SetProperty(o => o.TicketId, (int?)ticket.Id));
                return ticket.Id;
            }
            catch (Exception ex)
            {
                throw new FalhaAoGerarTicketException(ex);
            }
        }

        public Orcamento? ObterOrigemDoTicket(int ticketId)
        {
            return _dbContext.Orcamentos.AsNoTracking().FirstOrDefault(o => o.TicketId == ticketId);
        }

        public IReadOnlyList<VersaoOrcamento> ListarVersoes(int orcamentoId)
        {
            return _dbContext.VersoesOrcamento.AsNoTracking()
                .Where(v => v.OrcamentoId == orcamentoId)
                .OrderBy(v => v.Numero)
                .ToList();
        }

        public IReadOnlyList<HistoricoOrcamento> ListarHistorico(int orcamentoId)
        {
            return _dbContext.HistoricoOrcamento.AsNoTracking()
                .Where(h => h.OrcamentoId == orcamentoId)
                .OrderBy(h => h.Data)
                .ThenBy(h => h.Id)
                .ToList();
        }

        private static HistoricoOrcamento NovoEvento(int orcamentoId, TipoEventoOrcamento tipo, int usuarioId, DateTime data, string? texto = null)
        {
            return new HistoricoOrcamento
            {
                OrcamentoId = orcamentoId,
                Tipo = tipo,
                UsuarioId = usuarioId,
                Data = data,
                Texto = texto
            };
        }

        // Muda o status e grava o evento na mesma transação; sem efeito (0 linhas), nada é gravado.
        // aposAtualizar roda dentro da mesma transação, só se o UPDATE teve efeito.
        private bool ComEvento(Func<int> atualizar, HistoricoOrcamento evento, Action? aposAtualizar = null)
        {
            using var transacao = _dbContext.Database.BeginTransaction();
            if (atualizar() == 0)
            {
                return false;
            }

            aposAtualizar?.Invoke();

            _dbContext.HistoricoOrcamento.Add(evento);
            _dbContext.SaveChanges();
            transacao.Commit();
            return true;
        }

        public string? AdicionarAnexo(int orcamentoId, int empresaId, string nomeArquivo, string contentType, byte[] conteudo)
        {
            var editavel = _dbContext.Orcamentos.Any(o =>
                o.Id == orcamentoId && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Rascunho);
            if (!editavel)
            {
                return "Não é possível alterar os anexos deste orçamento.";
            }

            var extensao = Path.GetExtension(nomeArquivo);
            if (string.IsNullOrEmpty(extensao) || !AnexoAndamentoRegras.ExtensoesAceitas.Contains(extensao))
            {
                return "Tipo de arquivo não permitido. Tipos aceitos: PDF, JPEG, JPG, PNG, BMP.";
            }

            if (conteudo.Length == 0)
            {
                return "O arquivo está vazio.";
            }

            if (conteudo.LongLength > AnexoAndamentoRegras.TamanhoMaximoBytes)
            {
                return "O arquivo excede o tamanho máximo de 50MB.";
            }

            _dbContext.AnexosOrcamento.Add(new AnexoOrcamento
            {
                OrcamentoId = orcamentoId,
                NomeArquivoOriginal = Path.GetFileName(nomeArquivo),
                ContentType = contentType,
                TamanhoBytes = conteudo.LongLength,
                Conteudo = conteudo
            });
            _dbContext.SaveChanges();
            return null;
        }

        public bool RemoverAnexo(int orcamentoId, int anexoId, int empresaId)
        {
            var editavel = _dbContext.Orcamentos.Any(o =>
                o.Id == orcamentoId && o.EmpresaId == empresaId && o.Status == StatusOrcamento.Rascunho);
            if (!editavel)
            {
                return false;
            }

            return _dbContext.AnexosOrcamento
                .Where(a => a.Id == anexoId && a.OrcamentoId == orcamentoId)
                .ExecuteDelete() > 0;
        }

        public IReadOnlyList<AnexoOrcamentoResumo> ListarAnexos(int orcamentoId)
        {
            return _dbContext.AnexosOrcamento
                .Where(a => a.OrcamentoId == orcamentoId)
                .OrderBy(a => a.Id)
                .Select(a => new AnexoOrcamentoResumo(a.Id, a.NomeArquivoOriginal, a.TamanhoBytes))
                .ToList();
        }

        public AnexoOrcamento? ObterAnexo(int anexoId)
        {
            return _dbContext.AnexosOrcamento.FirstOrDefault(a => a.Id == anexoId);
        }

        // Status em que há conversa ativa. Escrito com || (não com array.Contains): o banco de produção está no
        // nível de compatibilidade 100, onde o EF traduziria Contains para OPENJSON, que não existe.
        private static readonly Expression<Func<Orcamento, bool>> EmNegociacao = o =>
            o.Status == StatusOrcamento.AguardandoCotacao
            || o.Status == StatusOrcamento.AjusteSolicitado
            || o.Status == StatusOrcamento.Cotado;

        public ResultadoOrcamento Perguntar(int orcamentoId, TipoUsuario tipoUsuario, int empresaId, int autorId, string? texto)
        {
            return RegistrarMensagem(orcamentoId, null, tipoUsuario, empresaId, autorId, texto);
        }

        public ResultadoOrcamento Responder(int orcamentoId, int perguntaId, TipoUsuario tipoUsuario, int empresaId, int autorId, string? texto)
        {
            return RegistrarMensagem(orcamentoId, perguntaId, tipoUsuario, empresaId, autorId, texto);
        }

        private ResultadoOrcamento RegistrarMensagem(int orcamentoId, int? perguntaId, TipoUsuario tipoUsuario, int empresaId, int autorId, string? texto)
        {
            if (tipoUsuario != TipoUsuario.Master && tipoUsuario != TipoUsuario.Gerente)
            {
                return ResultadoOrcamento.Falha("Você não tem permissão para escrever neste orçamento.");
            }

            var conteudo = texto?.Trim();
            if (string.IsNullOrEmpty(conteudo))
            {
                return ResultadoOrcamento.Falha("Escreva o texto da mensagem.");
            }

            if (conteudo.Length > PerguntaOrcamento.TamanhoMaximoTexto)
            {
                return ResultadoOrcamento.Falha($"A mensagem pode ter no máximo {PerguntaOrcamento.TamanhoMaximoTexto} caracteres.");
            }

            var ehMaster = tipoUsuario == TipoUsuario.Master;

            using var transacao = _dbContext.Database.BeginTransaction();

            // UPDATE sem efeito real: confirma o status de negociação e segura o bloqueio da linha até o commit,
            // de modo que uma aprovação/recusa/cancelamento concorrente espera e nunca fica com mensagem nova depois.
            var emNegociacao = _dbContext.Orcamentos
                .Where(EmNegociacao)
                .Where(o => o.Id == orcamentoId && (ehMaster || o.EmpresaId == empresaId))
                .ExecuteUpdate(s => s.SetProperty(o => o.Status, o => o.Status));
            if (emNegociacao == 0)
            {
                return ResultadoOrcamento.Falha("Não é possível registrar mensagens neste orçamento no status atual.");
            }

            var agora = DateTime.Now;
            if (perguntaId is null)
            {
                _dbContext.PerguntasOrcamento.Add(new PerguntaOrcamento
                {
                    OrcamentoId = orcamentoId,
                    AutorId = autorId,
                    Texto = conteudo,
                    DataCriacao = agora
                });
            }
            else
            {
                var pertence = _dbContext.PerguntasOrcamento.Any(p => p.Id == perguntaId && p.OrcamentoId == orcamentoId);
                if (!pertence)
                {
                    return ResultadoOrcamento.Falha("A pergunta não foi encontrada neste orçamento.");
                }

                _dbContext.RespostasPergunta.Add(new RespostaPergunta
                {
                    PerguntaId = perguntaId.Value,
                    AutorId = autorId,
                    Texto = conteudo,
                    DataCriacao = agora
                });
            }

            _dbContext.SaveChanges();
            transacao.Commit();
            _notificacao.Notificar(orcamentoId, perguntaId is null ? EventoEmailOrcamento.Pergunta : EventoEmailOrcamento.Resposta, autorId);
            return ResultadoOrcamento.Ok();
        }

        public IReadOnlyList<ConversaPergunta> ListarConversa(int orcamentoId)
        {
            var perguntas = _dbContext.PerguntasOrcamento.AsNoTracking()
                .Where(p => p.OrcamentoId == orcamentoId)
                .OrderBy(p => p.DataCriacao)
                .ThenBy(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.AutorId,
                    Tipo = p.Autor!.Tipo,
                    p.Texto,
                    p.DataCriacao,
                    Respostas = p.Respostas
                        .OrderBy(r => r.DataCriacao)
                        .ThenBy(r => r.Id)
                        .Select(r => new { r.Id, r.AutorId, Tipo = r.Autor!.Tipo, r.Texto, r.DataCriacao })
                        .ToList()
                })
                .ToList();

            return perguntas
                .Select(p => new ConversaPergunta(
                    new MensagemConversa(p.Id, p.AutorId, p.Tipo, p.Texto, p.DataCriacao),
                    p.Respostas.Select(r => new MensagemConversa(r.Id, r.AutorId, r.Tipo, r.Texto, r.DataCriacao)).ToList()))
                .ToList();
        }

        public int ContarPendentes(int orcamentoId)
        {
            return _dbContext.PerguntasOrcamento
                .Count(p => p.OrcamentoId == orcamentoId
                    && !p.Respostas.Any(r => r.Autor!.Tipo != p.Autor!.Tipo));
        }

        public IReadOnlyDictionary<int, int> ContarPendentesPorOrcamento(TipoUsuario tipoUsuario, int empresaId)
        {
            if (tipoUsuario != TipoUsuario.Master && tipoUsuario != TipoUsuario.Gerente)
            {
                return new Dictionary<int, int>();
            }

            var ehMaster = tipoUsuario == TipoUsuario.Master;

            return (from o in _dbContext.Orcamentos.Where(EmNegociacao).Where(o => ehMaster || o.EmpresaId == empresaId)
                    join p in _dbContext.PerguntasOrcamento on o.Id equals p.OrcamentoId
                    where !p.Respostas.Any(r => r.Autor!.Tipo != p.Autor!.Tipo)
                    group p by p.OrcamentoId into g
                    select new { OrcamentoId = g.Key, Quantidade = g.Count() })
                .ToDictionary(x => x.OrcamentoId, x => x.Quantidade);
        }

        public ResumoOrcamentosDashboard ObterResumoDashboard(TipoUsuario tipoUsuario, int empresaId)
        {
            if (tipoUsuario != TipoUsuario.Master && tipoUsuario != TipoUsuario.Gerente)
            {
                return ResumoOrcamentosDashboard.Vazio();
            }

            var hoje = DateTime.Today;
            IQueryable<Orcamento> visiveis = _dbContext.Orcamentos.AsNoTracking();
            visiveis = tipoUsuario == TipoUsuario.Gerente
                ? visiveis.Where(o => o.EmpresaId == empresaId)
                : visiveis.Where(o => o.Status != StatusOrcamento.Rascunho);

            var porStatus = visiveis
                .GroupBy(o => o.Status)
                .Select(g => new { Status = g.Key, Quantidade = g.Count() })
                .ToDictionary(x => x.Status, x => x.Quantidade);
            var expirados = visiveis.Count(o => o.Status == StatusOrcamento.Cotado && o.ValidadeAtual < hoje);

            int Quantidade(StatusOrcamento status) => porStatus.TryGetValue(status, out var q) ? q : 0;

            return new ResumoOrcamentosDashboard(
                Quantidade(StatusOrcamento.AguardandoCotacao),
                Quantidade(StatusOrcamento.Cotado) - expirados,
                Quantidade(StatusOrcamento.AjusteSolicitado),
                expirados,
                ContarPendentesPorOrcamento(tipoUsuario, empresaId).Values.Sum());
        }

        public bool ExisteOrcamentoDe(int usuarioId)
        {
            return _dbContext.Orcamentos.Any(o => o.CriadoPorId == usuarioId)
                || _dbContext.VersoesOrcamento.Any(v => v.CriadoPorId == usuarioId)
                || _dbContext.HistoricoOrcamento.Any(h => h.UsuarioId == usuarioId)
                || _dbContext.PerguntasOrcamento.Any(p => p.AutorId == usuarioId)
                || _dbContext.RespostasPergunta.Any(r => r.AutorId == usuarioId);
        }

        public bool ExisteOrcamentoDaEmpresa(int empresaId)
        {
            return _dbContext.Orcamentos.Any(o => o.EmpresaId == empresaId);
        }
    }
}
