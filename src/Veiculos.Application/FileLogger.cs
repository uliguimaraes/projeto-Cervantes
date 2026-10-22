namespace Veiculos.Application;

/// <summary>
/// Grava os erros da aplicação em arquivo. Nunca lança exceção:
/// uma falha ao gravar o log não pode derrubar a aplicação.
/// </summary>
public sealed class FileLogger : IAppLogger
{
    private readonly string caminho;
    private readonly object trava = new();

    public FileLogger(string caminho) => this.caminho = caminho;

    public void Erro(Exception exception, string contexto)
    {
        try
        {
            lock (trava)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(caminho) ?? ".");
                File.AppendAllText(caminho, $"{DateTime.Now:O} | {contexto} | {exception}{Environment.NewLine}");
            }
        }
        catch
        {
            // Intencional: o log é "melhor esforço".
        }
    }
}
