namespace TmkChamados.Models
{
    // Mensagem seguinte de uma conversa. Imutável depois de criada.
    public class RespostaPergunta
    {
        public int Id { get; set; }

        public int PerguntaId { get; set; }

        public int AutorId { get; set; }

        public Usuario? Autor { get; set; }

        public string Texto { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; }
    }
}
