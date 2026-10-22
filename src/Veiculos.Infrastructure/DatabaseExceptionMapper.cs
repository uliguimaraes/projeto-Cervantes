using Npgsql;
using Veiculos.Application;

namespace Veiculos.Infrastructure;

/// <summary>
/// Converte qualquer exceção em mensagem amigável para o usuário.
/// Como as regras de negócio vivem no banco, é aqui que a violação de uma
/// constraint/trigger vira um texto que o usuário entende.
/// </summary>
public static class DatabaseExceptionMapper
{
    private const string FalhaConexao =
        "Não foi possível conectar ao banco de dados. Verifique se o PostgreSQL está em execução e se a conexão (VEICULOS_CONNECTION) está correta.";

    /// <summary>
    /// Erro "esperado" é aquele causado pelo uso (dado inválido, regra violada): o usuário corrige e segue.
    /// Esses não vão para o log de erros; os inesperados (falha de conexão, bug) vão.
    /// </summary>
    public static bool EhErroEsperado(Exception exception) => exception switch
    {
        RegraNegocioException => true,
        PostgresException pg => pg.SqlState is "23505" or "23503" or "23502" or "23514" or "22001" or "P0001",
        _ => false
    };

    public static string ParaUsuario(Exception exception) => exception switch
    {
        RegraNegocioException regra => regra.Message,
        PostgresException pg => MensagemPostgres(pg),
        NpgsqlException or TimeoutException or System.Net.Sockets.SocketException => FalhaConexao,
        _ => "Ocorreu um erro inesperado. O problema foi registrado no arquivo de log."
    };

    private static string MensagemPostgres(PostgresException pg) => pg.SqlState switch
    {
        "23505" => pg.ConstraintName switch
        {
            "UQ_Marca_Nome" => "Já existe uma marca com esse nome.",
            "UQ_Veiculo_Placa" => "Já existe um veículo cadastrado com essa placa.",
            _ => "Já existe um registro com esses dados."
        },

        // A exclusão de marca vinculada é tratada no PostgresMarcaRepository.ExcluirAsync.
        // Aqui sobra o caso de gravar um veículo apontando para uma marca que não existe.
        "23503" => "A marca informada não existe.",

        "23502" => pg.ColumnName switch
        {
            "Nome" => "O nome da marca é obrigatório.",
            "Placa" => "A placa é obrigatória.",
            "Modelo" => "O modelo é obrigatório.",
            "Ano" => "O ano é obrigatório.",
            "MarcaCodigo" => "O veículo deve possuir uma marca.",
            _ => "Existe um campo obrigatório sem preenchimento."
        },

        "23514" => pg.ConstraintName switch
        {
            "CK_Marca_Nome_NaoVazio" => "O nome da marca é obrigatório.",
            "CK_Veiculo_Placa_NaoVazia" => "A placa é obrigatória.",
            "CK_Veiculo_Modelo_NaoVazio" => "O modelo é obrigatório.",
            "CK_Veiculo_Ano_Minimo" => "O ano do veículo deve ser 1950 ou posterior.",
            "CK_Veiculo_Tipo" or "CK_Veiculo_Detalhe" => "Os dados específicos do tipo de veículo (portas ou cilindradas) são inválidos.",
            _ => "Os dados informados violam uma regra de validação do banco de dados."
        },

        "22001" => "Um dos textos informados é maior que o tamanho permitido (placa: até 10 caracteres).",

        // RAISE EXCEPTION das triggers (ex.: ano acima do ano atual) já vem com texto em português.
        "P0001" => pg.MessageText,

        // Falha de acesso: senha errada, banco inexistente, servidor indisponível.
        "28P01" or "28000" or "3D000" or "57P03" => FalhaConexao,
        var estado when estado.StartsWith("08", StringComparison.Ordinal) => FalhaConexao,

        _ => "O banco de dados recusou a operação. Confira os dados e tente novamente."
    };
}
