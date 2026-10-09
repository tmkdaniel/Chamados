using System.ComponentModel.DataAnnotations;

namespace TmkChamados.Models
{
    public enum TipoUsuario
    {
        Master,
        Gerente,
        Usuario
    }

    public class Usuario
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "O nome é obrigatório.")]
        public string Nome { get; set; } = string.Empty;

        public string SenhaHash { get; set; } = string.Empty;

        public TipoUsuario Tipo { get; set; } = TipoUsuario.Usuario;

        public string? Email { get; set; }

        public int EmpresaId { get; set; }

        public Empresa? Empresa { get; set; }
    }
}
