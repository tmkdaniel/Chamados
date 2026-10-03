using System.ComponentModel.DataAnnotations;

namespace TmkChamados.Models
{
    public enum StatusChamado
    {
        Aberto,
        EmAndamento,
        Concluido,
        Cancelado
    }

    public enum PrioridadeChamado
    {
        Normal,
        Alta,
        Urgente
    }

    public class Chamado
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public StatusChamado Status { get; set; } = StatusChamado.Aberto;

        public PrioridadeChamado Prioridade { get; set; } = PrioridadeChamado.Normal;

        public DateTime DataCriacao { get; set; }

        public DateTime DataUltimaModificacao { get; set; }

        public DateTime? DataPrazo { get; set; }

        public DateTime? DataConclusao { get; set; }

        public int CriadoPorId { get; set; }

        public int? ResponsavelId { get; set; }
    }
}
