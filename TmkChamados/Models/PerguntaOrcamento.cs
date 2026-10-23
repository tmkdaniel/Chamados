namespace TmkChamados.Models
{
    // Primeira mensagem de uma conversa do orçamento. Imutável depois de criada.
    public class PerguntaOrcamento
    {
        public const int TamanhoMaximoTexto = 2000;

        public int Id { get; set; }

        public int OrcamentoId { get; set; }

        public int AutorId { get; set; }

        public Usuario? Autor { get; set; }

        public string Texto { get; set; } = string.Empty;

        public DateTime DataCriacao { get; set; }

        public List<RespostaPergunta> Respostas { get; set; } = new();
    }
}
