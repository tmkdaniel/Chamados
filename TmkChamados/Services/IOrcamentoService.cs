using TmkChamados.Models;

namespace TmkChamados.Services
{
    public record AnexoOrcamentoResumo(int Id, string NomeArquivoOriginal, long TamanhoBytes);

    // Erro != null: operação rejeitada, nada foi alterado. Repetido: o efeito já havia sido aplicado
    // (duplo clique/concorrência) e não deve gerar mensagem nem erro.
    public record ResultadoOrcamento(bool Sucesso, string? Erro = null, bool Repetido = false)
    {
        public static ResultadoOrcamento Ok() => new(true);

        public static ResultadoOrcamento Falha(string erro) => new(false, erro);
    }

    public record MensagemConversa(int Id, int AutorId, TipoUsuario AutorTipo, string Texto, DateTime Data);

    // Uma pergunta e suas respostas. Pendente até alguém do lado oposto ao do autor responder
    // (lado = tipo do usuário: Master de um lado, Gerentes do outro).
    public record ConversaPergunta(MensagemConversa Pergunta, IReadOnlyList<MensagemConversa> Respostas)
    {
        public bool Pendente => !Respostas.Any(r => r.AutorTipo != Pergunta.AutorTipo);
    }

    // Contagens do card do dashboard. Cotados = validade em vigor; Expirados = Cotado com validade vencida.
    public record ResumoOrcamentosDashboard(int AguardandoCotacao, int Cotados, int AjusteSolicitado, int Expirados, int PerguntasPendentes)
    {
        public static ResumoOrcamentosDashboard Vazio() => new(0, 0, 0, 0, 0);
    }

    public interface IOrcamentoService
    {
        IReadOnlyList<Orcamento> Listar(FiltroOrcamentos filtro, TipoUsuario tipoUsuario, int empresaId);

        Orcamento? Obter(int id);

        // Visibilidade: Gerente vê a própria empresa em qualquer status; Master vê todas, exceto Rascunho;
        // Usuario não vê nada.
        bool PodeAcessar(Orcamento orcamento, TipoUsuario tipoUsuario, int empresaId);

        // Ações de escrita são exclusivas de Gerentes da empresa do orçamento (o tipo é checado pelas páginas).
        bool PodeGerenciar(Orcamento orcamento, TipoUsuario tipoUsuario, int empresaId);

        Orcamento Criar(string titulo, string descricao, int empresaId, int criadoPorId);

        // As operações abaixo retornam false quando o orçamento não existe, é de outra empresa
        // ou não está no status que a operação exige (nada é alterado nesse caso).
        bool Atualizar(int id, string titulo, string descricao, int empresaId);

        bool Excluir(int id, int empresaId);

        bool Enviar(int id, int empresaId, int usuarioId);

        bool Cancelar(int id, int empresaId, int usuarioId);

        // Negociação. Cotar é do Master; as demais são de Gerentes da empresa do orçamento.
        // versaoVista: versão vigente quando o Master abriu a tela (null se ainda não havia cotação); protege
        // contra duplo clique, já que duas cotações idênticas seguidas seriam duas versões legítimas.
        ResultadoOrcamento Cotar(int id, int? versaoVista, decimal valor, int prazoDias, DateTime validade, string? observacao, int masterId);

        ResultadoOrcamento SolicitarAjuste(int id, int empresaId, int gerenteId, string? motivo);

        // versaoVista: a versão que o Gerente estava vendo; a aprovação só vale para a versão vigente.
        ResultadoOrcamento Aprovar(int id, int empresaId, int gerenteId, int versaoVista, string? comentario);

        ResultadoOrcamento Recusar(int id, int empresaId, int gerenteId, string? motivo);

        // Orçamento que gerou o ticket (ou null se o ticket não veio de um orçamento).
        Orcamento? ObterOrigemDoTicket(int ticketId);

        IReadOnlyList<VersaoOrcamento> ListarVersoes(int orcamentoId);

        IReadOnlyList<HistoricoOrcamento> ListarHistorico(int orcamentoId);

        // Retorna null em caso de sucesso, ou a mensagem de erro para o usuário.
        string? AdicionarAnexo(int orcamentoId, int empresaId, string nomeArquivo, string contentType, byte[] conteudo);

        bool RemoverAnexo(int orcamentoId, int anexoId, int empresaId);

        // Projeção explícita: nunca traz a coluna Conteudo só para listar os anexos.
        IReadOnlyList<AnexoOrcamentoResumo> ListarAnexos(int orcamentoId);

        AnexoOrcamento? ObterAnexo(int anexoId);

        // Perguntas e respostas. Master (qualquer empresa) e Gerentes (da própria empresa) escrevem enquanto o
        // orçamento está em negociação (Aguardando cotação, Ajuste solicitado ou Cotado); Usuario nunca.
        ResultadoOrcamento Perguntar(int orcamentoId, TipoUsuario tipoUsuario, int empresaId, int autorId, string? texto);

        ResultadoOrcamento Responder(int orcamentoId, int perguntaId, TipoUsuario tipoUsuario, int empresaId, int autorId, string? texto);

        IReadOnlyList<ConversaPergunta> ListarConversa(int orcamentoId);

        int ContarPendentes(int orcamentoId);

        // OrcamentoId -> perguntas pendentes, só para orçamentos em negociação visíveis ao usuário (uma consulta).
        IReadOnlyDictionary<int, int> ContarPendentesPorOrcamento(TipoUsuario tipoUsuario, int empresaId);

        // Master: todas as empresas, sem Rascunho; Gerente: a própria empresa; Usuario: vazio.
        ResumoOrcamentosDashboard ObterResumoDashboard(TipoUsuario tipoUsuario, int empresaId);

        bool ExisteOrcamentoDe(int usuarioId);

        bool ExisteOrcamentoDaEmpresa(int empresaId);
    }
}
