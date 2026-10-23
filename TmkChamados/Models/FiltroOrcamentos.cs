namespace TmkChamados.Models
{
    public class FiltroOrcamentos
    {
        public StatusOrcamento? Status { get; set; }

        // Expirado = Cotado com validade vencida; só tem sentido junto com Status = Cotado.
        public bool Expirado { get; set; }

        // Valor único do select de status da tela: nome do status ou "Expirado".
        public string? Situacao
        {
            get => Expirado ? "Expirado" : Status?.ToString();
            set
            {
                Expirado = value == "Expirado";
                if (Expirado)
                {
                    Status = StatusOrcamento.Cotado;
                }
                else
                {
                    Status = Enum.TryParse<StatusOrcamento>(value, out var status) && Enum.IsDefined(status)
                        ? status
                        : null;
                }
            }
        }

        public DateTime? CriadoDe { get; set; }

        public DateTime? CriadoAte { get; set; }

        public string? Titulo { get; set; }

        public int? EmpresaId { get; set; }
    }
}
