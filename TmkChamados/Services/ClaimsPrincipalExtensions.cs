using System.Security.Claims;
using TmkChamados.Models;

namespace TmkChamados.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public const string TipoClaimType = "TipoUsuario";

        public const string EmpresaClaimType = "EmpresaId";

        public static int? ObterUsuarioId(this ClaimsPrincipal principal)
        {
            var valor = principal.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(valor, out var id) ? id : null;
        }

        public static TipoUsuario? ObterTipoUsuario(this ClaimsPrincipal principal)
        {
            var valor = principal.FindFirstValue(TipoClaimType);
            return Enum.TryParse<TipoUsuario>(valor, out var tipo) ? tipo : null;
        }

        public static int? ObterEmpresaId(this ClaimsPrincipal principal)
        {
            var valor = principal.FindFirstValue(EmpresaClaimType);
            return int.TryParse(valor, out var id) ? id : null;
        }

        public static bool EhMaster(this ClaimsPrincipal principal)
        {
            return principal.ObterTipoUsuario() == TipoUsuario.Master;
        }
    }
}
