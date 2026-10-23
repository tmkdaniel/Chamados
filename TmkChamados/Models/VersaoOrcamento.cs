namespace TmkChamados.Models
{
    // Cotação do Master. Imutável depois de criada.
    public class VersaoOrcamento
    {
        public int Id { get; set; }

        public int OrcamentoId { get; set; }

        public int Numero { get; set; }

        public decimal Valor { get; set; }

        public int PrazoDias { get; set; }

        public DateTime Validade { get; set; }

        public string? Observacao { get; set; }

        public int CriadoPorId { get; set; }

        public DateTime DataCriacao { get; set; }
    }
}
