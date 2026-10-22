using Veiculos.Application;
using Veiculos.Infrastructure;

namespace Veiculos.WinForms;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var logger = new FileLogger(Path.Combine(AppContext.BaseDirectory, "logs", "erros.log"));

        // Rede de segurança: qualquer exceção que escape dos tratamentos da tela cai aqui,
        // é registrada no log e o usuário recebe uma mensagem amigável (sem stack trace).
        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) => TratarExcecaoNaoTratada(e.Exception, logger);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            if (e.ExceptionObject is Exception excecao) logger.Erro(excecao, "exceção não tratada (AppDomain)");
        };

        try
        {
            var connection = Environment.GetEnvironmentVariable("VEICULOS_CONNECTION")
                ?? "Host=localhost;Port=5432;Database=veiculos;Username=postgres;Password=postgres";
            var factory = new PostgresConnectionFactory(connection);

            var marcas = new MarcaService(new PostgresMarcaRepository(factory));
            var veiculos = new VeiculoService(new PostgresVeiculoRepository(factory));

            System.Windows.Forms.Application.Run(new MainForm(marcas, veiculos, logger));
        }
        catch (Exception ex)
        {
            TratarExcecaoNaoTratada(ex, logger);
        }
    }

    private static void TratarExcecaoNaoTratada(Exception excecao, IAppLogger logger)
    {
        logger.Erro(excecao, "exceção não tratada na interface");
        MessageBox.Show(
            DatabaseExceptionMapper.ParaUsuario(excecao),
            "Erro inesperado",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }
}
