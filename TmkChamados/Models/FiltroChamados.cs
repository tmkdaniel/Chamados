namespace TmkChamados.Models
{
    public class FiltroChamados
    {
        public StatusChamado? Status { get; set; }

        public int? CriadoPorId { get; set; }

        public int? ResponsavelId { get; set; }

        public DateTime? CriadoDe { get; set; }

        public DateTime? CriadoAte { get; set; }

        public DateTime? ModificadoDe { get; set; }

        public DateTime? ModificadoAte { get; set; }
    }
}
