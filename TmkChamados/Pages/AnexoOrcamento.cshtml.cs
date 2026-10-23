using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TmkChamados.Services;

namespace TmkChamados.Pages
{
    public class AnexoOrcamentoModel : PageModel
    {
        private readonly IOrcamentoService _orcamentoService;

        public AnexoOrcamentoModel(IOrcamentoService orcamentoService)
        {
            _orcamentoService = orcamentoService;
        }

        public IActionResult OnGet(int anexoId)
        {
            var tipoUsuario = User.ObterTipoUsuario();
            var empresaId = User.ObterEmpresaId();

            if (!User.EhMasterOuGerente() || tipoUsuario is null || empresaId is null)
            {
                return Forbid();
            }

            var anexo = _orcamentoService.ObterAnexo(anexoId);
            if (anexo is null)
            {
                return NotFound();
            }

            var orcamento = _orcamentoService.Obter(anexo.OrcamentoId);
            if (orcamento is null || !_orcamentoService.PodeAcessar(orcamento, tipoUsuario.Value, empresaId.Value))
            {
                return Forbid();
            }

            return File(anexo.Conteudo, anexo.ContentType, anexo.NomeArquivoOriginal);
        }
    }
}
