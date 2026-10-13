using System.Text.Json.Nodes;
using Microsoft.AspNetCore.DataProtection;

namespace TmkChamados.Services
{
    public class SegredoConfiguracaoResolver
    {
        private readonly IDataProtector _protetor;

        public SegredoConfiguracaoResolver(IDataProtector protetor)
        {
            _protetor = protetor;
        }

        public string? Resolver(string? valorEfetivo, JsonNode appSettingsRaiz, string[] caminhoValor, string[] caminhoCripto, out bool arquivoAlterado)
        {
            arquivoAlterado = false;

            var valorBruto = ObterTexto(appSettingsRaiz, caminhoValor);

            // Alguma camada de prioridade maior (appsettings.{Ambiente}.json, variável de
            // ambiente) está sobrescrevendo; usa como está, sem mexer em criptografia
            // nem no arquivo (ver design.md - Decisão 2).
            if (valorEfetivo != valorBruto)
            {
                return valorEfetivo;
            }

            if (string.IsNullOrEmpty(valorBruto))
            {
                return valorEfetivo;
            }

            if (ObterFlagCripto(appSettingsRaiz, caminhoCripto))
            {
                return Descriptografar(valorBruto, caminhoValor);
            }

            var valorCriptografado = Criptografar(valorBruto);
            DefinirTexto(appSettingsRaiz, caminhoValor, valorCriptografado);
            DefinirFlagCripto(appSettingsRaiz, caminhoCripto, 1);
            arquivoAlterado = true;
            return valorBruto;
        }

        private string Criptografar(string textoPuro) => _protetor.Protect(textoPuro);

        private string Descriptografar(string valorCriptografado, string[] caminhoValor)
        {
            try
            {
                return _protetor.Unprotect(valorCriptografado);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Falha ao descriptografar a configuração em '{string.Join(':', caminhoValor)}'. " +
                    "A chave de proteção pode estar indisponível (ex.: appsettings.json copiado sem a pasta Keys correspondente).", ex);
            }
        }

        private static string? ObterTexto(JsonNode raiz, string[] caminho)
        {
            var no = NavegarAte(raiz, caminho);
            return no?.GetValue<string>();
        }

        private static bool ObterFlagCripto(JsonNode raiz, string[] caminho)
        {
            var no = NavegarAte(raiz, caminho);
            return no is not null && no.AsValue().TryGetValue<int>(out var valor) && valor == 1;
        }

        private static void DefinirTexto(JsonNode raiz, string[] caminho, string valor)
            => DefinirNo(raiz, caminho, JsonValue.Create(valor));

        private static void DefinirFlagCripto(JsonNode raiz, string[] caminho, int valor)
            => DefinirNo(raiz, caminho, JsonValue.Create(valor));

        private static JsonNode? NavegarAte(JsonNode raiz, string[] caminho)
        {
            JsonNode? atual = raiz;
            foreach (var segmento in caminho)
            {
                if (atual is not JsonObject objeto || !objeto.TryGetPropertyValue(segmento, out atual))
                {
                    return null;
                }
            }
            return atual;
        }

        private static void DefinirNo(JsonNode raiz, string[] caminho, JsonNode? valor)
        {
            var atual = (JsonObject)raiz;
            for (var i = 0; i < caminho.Length - 1; i++)
            {
                if (atual[caminho[i]] is not JsonObject proximo)
                {
                    proximo = new JsonObject();
                    atual[caminho[i]] = proximo;
                }
                atual = proximo;
            }
            atual[caminho[^1]] = valor;
        }
    }
}
