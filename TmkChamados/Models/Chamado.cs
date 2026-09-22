using System.ComponentModel.DataAnnotations;

namespace TmkChamados.Models
{
    public enum StatusChamado
    {
        Aberto,
        EmAndamento,
        Concluido
    }

    public class Chamado
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O título é obrigatório.")]
        public string Titulo { get; set; } = string.Empty;

        public string Descricao { get; set; } = string.Empty;

        public StatusChamado Status { get; set; } = StatusChamado.Aberto;
    }
}
