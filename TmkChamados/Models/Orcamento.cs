using System.ComponentModel.DataAnnotations;

namespace TmkChamados.Models
{
    // Persistido como inteiro: as próximas etapas só acrescentam valores novos, nunca renumeram.
    public enum StatusOrcamento
    {
        Rascunho = 0,
        AguardandoCotacao = 1,
        Cancelado = 2,
        Cotado = 3,
        AjusteSolicitado = 4,
        Aprovado = 5,
        Recusado = 6
    }

    // Persistido como inteiro: só acrescentar valores novos.
    public enum TipoEventoOrcamento
    {
        Enviado = 0,
        Cotado = 1,
        AjusteSolicitado = 2,
        Aprovado = 3,
        Recusado = 4,
        Cancelado = 5
    }

    public class Orcamento
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        public string Descricao { get; set; } = string.Empty;

        public StatusOrcamento Status { get; set; } = StatusOrcamento.Rascunho;

        public int EmpresaId { get; set; }

        public int CriadoPorId { get; set; }

        public DateTime DataCriacao { get; set; }

        public DateTime? DataEnvio { get; set; }

        // Resumo da cotação vigente (última versão), gravado junto com a versão. A fonte da verdade
        // é a tabela de versões; estas colunas existem para listar e filtrar sem subconsulta.
        public int? VersaoAtual { get; set; }

        public decimal? ValorAtual { get; set; }

        public int? PrazoAtualDias { get; set; }

        public DateTime? ValidadeAtual { get; set; }

        // Ticket de Mudança gerado na aprovação; nulo antes da aprovação ou depois que o ticket é excluído.
        public int? TicketId { get; set; }

        // "Expirado" não é gravado: é Cotado com a validade anterior ao dia atual.
        public bool Expirado => Status == StatusOrcamento.Cotado
            && ValidadeAtual.HasValue
            && ValidadeAtual.Value.Date < DateTime.Today;
    }
}
