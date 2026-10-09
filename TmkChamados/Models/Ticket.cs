using System.ComponentModel.DataAnnotations;

namespace TmkChamados.Models
{
    public enum StatusTicket
    {
        Aberto,
        EmAndamento,
        Concluido,
        Cancelado
    }

    public enum PrioridadeTicket
    {
        Normal,
        Alta,
        Urgente
    }

    public enum ClassificacaoTicket
    {
        Incidente,
        Requisicao,
        Problema,
        Mudanca
    }

    public class Ticket
    {
        public int Id { get; set; }

        public ClassificacaoTicket Classificacao { get; set; } = ClassificacaoTicket.Incidente;

        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public StatusTicket Status { get; set; } = StatusTicket.Aberto;

        public PrioridadeTicket Prioridade { get; set; } = PrioridadeTicket.Normal;

        public DateTime DataCriacao { get; set; }

        public DateTime DataUltimaModificacao { get; set; }

        public DateTime? DataPrazo { get; set; }

        public DateTime? DataConclusao { get; set; }

        public int CriadoPorId { get; set; }

        public int? ResponsavelId { get; set; }
    }
}
