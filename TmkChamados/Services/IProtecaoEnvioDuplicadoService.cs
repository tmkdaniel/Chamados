namespace TmkChamados.Services
{
    public interface IProtecaoEnvioDuplicadoService
    {
        string GerarToken();

        bool TentarRegistrar(string? token);
    }
}
