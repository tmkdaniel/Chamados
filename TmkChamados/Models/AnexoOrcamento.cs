namespace TmkChamados.Models
{
    public class AnexoOrcamento
    {
        public int Id { get; set; }

        public int OrcamentoId { get; set; }

        public string NomeArquivoOriginal { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public long TamanhoBytes { get; set; }

        public byte[] Conteudo { get; set; } = Array.Empty<byte>();
    }
}
