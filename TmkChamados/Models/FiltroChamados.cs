namespace TmkChamados.Models
{
    public class FiltroChamados
    {
        public StatusChamado? Status { get; set; }

        public DateTime? CriadoDe { get; set; }

        public DateTime? CriadoAte { get; set; }

        public DateTime? ModificadoDe { get; set; }

        public DateTime? ModificadoAte { get; set; }
    }
}
