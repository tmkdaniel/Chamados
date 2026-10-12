namespace TmkChamados.Services
{
    public static class AnexoAndamentoRegras
    {
        public static readonly IReadOnlySet<string> ExtensoesAceitas = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf",
            ".jpeg",
            ".jpg",
            ".png",
            ".bmp"
        };

        public const long TamanhoMaximoBytes = 50L * 1024 * 1024;
    }
}
