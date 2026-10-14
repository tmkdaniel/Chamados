using System.Collections.Concurrent;

namespace TmkChamados.Services
{
    public class ProtecaoEnvioDuplicadoService : IProtecaoEnvioDuplicadoService
    {
        private static readonly TimeSpan TempoRetencao = TimeSpan.FromMinutes(10);

        private readonly ConcurrentDictionary<string, DateTime> _tokens = new();

        public string GerarToken()
        {
            return Guid.NewGuid().ToString();
        }

        public bool TentarRegistrar(string? token)
        {
            if (string.IsNullOrEmpty(token))
            {
                return true;
            }

            LimparTokensExpirados();

            return _tokens.TryAdd(token, DateTime.UtcNow);
        }

        private void LimparTokensExpirados()
        {
            var limite = DateTime.UtcNow - TempoRetencao;
            foreach (var par in _tokens)
            {
                if (par.Value < limite)
                {
                    _tokens.TryRemove(par.Key, out _);
                }
            }
        }
    }
}
