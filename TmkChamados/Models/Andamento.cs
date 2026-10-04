using System.ComponentModel.DataAnnotations;

namespace TmkChamados.Models
{
    public class Andamento
    {
        public int Id { get; set; }

        public int ChamadoId { get; set; }

        [Required(ErrorMessage = "O texto do andamento é obrigatório.")]
        public string Texto { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; }

        public int CriadoPorId { get; set; }
    }
}
