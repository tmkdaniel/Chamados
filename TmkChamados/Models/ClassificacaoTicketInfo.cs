namespace TmkChamados.Models
{
    public record ClassificacaoTicketInfo(
        ClassificacaoTicket Valor,
        string Label,
        string OQueE,
        string Foco,
        string ExemploTitulo)
    {
        public static readonly IReadOnlyList<ClassificacaoTicketInfo> Todas = new[]
        {
            new ClassificacaoTicketInfo(
                ClassificacaoTicket.Incidente,
                "Incidente",
                "Qualquer interrupção não planejada ou redução na qualidade de um serviço de TI. Algo que funcionava e parou de funcionar.",
                "Restabelecer a operação normal o mais rápido possível (apagar o fogo).",
                "Erro ao acessar o sistema TMK"),
            new ClassificacaoTicketInfo(
                ClassificacaoTicket.Requisicao,
                "Requisição",
                "Uma solicitação formal de um usuário para que algo seja fornecido. Não há uma falha ou quebra de serviço, apenas um pedido de rotina.",
                "Atender à necessidade do usuário seguindo procedimentos padrão predefinidos.",
                "Criação de novo usuário no Active Directory"),
            new ClassificacaoTicketInfo(
                ClassificacaoTicket.Problema,
                "Problema",
                "A causa raiz de um ou mais incidentes recorrentes ou graves.",
                "Investigar, identificar a causa e encontrar uma solução definitiva ou um contorno (workaround).",
                "Instabilidade intermitente no servidor de banco de dados"),
            new ClassificacaoTicketInfo(
                ClassificacaoTicket.Mudanca,
                "Mudança",
                "Qualquer adição, modificação ou remoção de algo que possa afetar os serviços de TI.",
                "Controlar os riscos para que a alteração não cause novos incidentes.",
                "Migração do servidor de arquivos para a nuvem"),
        };

        private static readonly Dictionary<ClassificacaoTicket, ClassificacaoTicketInfo> PorValor =
            Todas.ToDictionary(c => c.Valor);

        public static ClassificacaoTicketInfo Obter(ClassificacaoTicket valor) => PorValor[valor];

        public static string ObterLabel(ClassificacaoTicket valor) => PorValor[valor].Label;
    }
}
