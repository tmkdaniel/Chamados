namespace TmkChamados.Models
{
    public class ImportacaoEmailSettings
    {
        public string Host { get; set; } = string.Empty;

        public int Port { get; set; } = 993;

        public bool EnableSsl { get; set; } = true;

        public string? Username { get; set; }

        public string? Password { get; set; }

        public int IntervaloMinutos { get; set; } = 3;
    }
}
