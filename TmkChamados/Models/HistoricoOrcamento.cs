namespace TmkChamados.Models
{
    public class HistoricoOrcamento
    {
        public int Id { get; set; }

        public int OrcamentoId { get; set; }

        public TipoEventoOrcamento Tipo { get; set; }

        public int UsuarioId { get; set; }

        public DateTime Data { get; set; }

        public int? VersaoNumero { get; set; }

        public string? Texto { get; set; }
    }
}
