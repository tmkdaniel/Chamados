using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class AnexoAndamentoModel : PageModel
    {
        private readonly IAndamentoService _andamentoService;
        private readonly ITicketService _ticketService;

        public AnexoAndamentoModel(IAndamentoService andamentoService, ITicketService ticketService)
        {
            _andamentoService = andamentoService;
            _ticketService = ticketService;
        }

        public IActionResult OnGet(int andamentoId)
        {
            var tipoUsuario = User.ObterTipoUsuario();
            var usuarioId = User.ObterUsuarioId();
            var empresaId = User.ObterEmpresaId();

            if (tipoUsuario is null || usuarioId is null || empresaId is null)
            {
                return Forbid();
            }

            var andamento = _andamentoService.Obter(andamentoId);
            if (andamento is null)
            {
                return NotFound();
            }

            var ticket = _ticketService.Obter(andamento.TicketId);
            if (ticket is null || !_ticketService.PodeAcessar(ticket, tipoUsuario.Value, usuarioId.Value, empresaId.Value))
            {
                return Forbid();
            }

            var anexo = _andamentoService.ObterAnexo(andamentoId);
            if (anexo is null)
            {
                return NotFound();
            }

            return File(anexo.Conteudo, anexo.ContentType, anexo.NomeArquivoOriginal);
        }
    }
}
