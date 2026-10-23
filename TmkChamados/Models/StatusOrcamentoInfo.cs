namespace TmkChamados.Models
{
    public static class StatusOrcamentoInfo
    {
        public static string Label(StatusOrcamento status) => status switch
        {
            StatusOrcamento.Rascunho => "Rascunho",
            StatusOrcamento.AguardandoCotacao => "Aguardando cotação",
            StatusOrcamento.Cancelado => "Cancelado",
            StatusOrcamento.Cotado => "Cotado",
            StatusOrcamento.AjusteSolicitado => "Ajuste solicitado",
            StatusOrcamento.Aprovado => "Aprovado",
            StatusOrcamento.Recusado => "Recusado",
            _ => status.ToString()
        };

        public static string ClasseCor(StatusOrcamento status) => status switch
        {
            StatusOrcamento.Rascunho => "bg-secondary",
            StatusOrcamento.AguardandoCotacao => "bg-warning text-dark",
            StatusOrcamento.Cancelado => "bg-dark",
            StatusOrcamento.Cotado => "bg-info text-dark",
            StatusOrcamento.AjusteSolicitado => "bg-warning text-dark",
            StatusOrcamento.Aprovado => "bg-success",
            StatusOrcamento.Recusado => "bg-danger",
            _ => "bg-light text-dark"
        };

        // Rótulo e cor de exibição: um Cotado com validade vencida aparece como Expirado.
        public static string Label(Orcamento orcamento) => orcamento.Expirado ? "Expirado" : Label(orcamento.Status);

        public static string ClasseCor(Orcamento orcamento) => orcamento.Expirado ? "bg-danger" : ClasseCor(orcamento.Status);

        public static string Label(TipoEventoOrcamento tipo) => tipo switch
        {
            TipoEventoOrcamento.Enviado => "Enviado ao Master",
            TipoEventoOrcamento.Cotado => "Cotação emitida",
            TipoEventoOrcamento.AjusteSolicitado => "Ajuste solicitado",
            TipoEventoOrcamento.Aprovado => "Aprovado",
            TipoEventoOrcamento.Recusado => "Recusado",
            TipoEventoOrcamento.Cancelado => "Cancelado",
            _ => tipo.ToString()
        };
    }
}
