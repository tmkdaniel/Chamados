using System.ComponentModel.DataAnnotations;

namespace TmkChamados.Models
{
    public class Empresa
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;
    }
}
