using Npgsql;
using NpgsqlTypes;
using Veiculos.Application;
using Veiculos.Domain;

namespace Veiculos.Infrastructure;

public sealed class PostgresMarcaRepository : IMarcaRepository
{
    private readonly PostgresConnectionFactory factory;
    public PostgresMarcaRepository(PostgresConnectionFactory factory) => this.factory = factory;

    public async Task<IReadOnlyList<Marca>> ListarAsync(CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand("SELECT \"Codigo\", \"Nome\" FROM \"Marca\" ORDER BY \"Nome\"", connection);
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<Marca>();
        while (await reader.ReadAsync(token)) result.Add(new Marca(reader.GetInt32(0), reader.GetString(1)));
        return result;
    }

    public async Task<int> InserirAsync(string nome, CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand("INSERT INTO \"Marca\" (\"Nome\") VALUES (@nome) RETURNING \"Codigo\"", connection);
        command.Parameters.AddWithValue("nome", nome);
        return (int)(await command.ExecuteScalarAsync(token) ?? 0);
    }

    public async Task AtualizarAsync(int codigo, string nome, CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand("UPDATE \"Marca\" SET \"Nome\" = @nome WHERE \"Codigo\" = @codigo", connection);
        command.Parameters.AddWithValue("nome", nome);
        command.Parameters.AddWithValue("codigo", codigo);
        if (await command.ExecuteNonQueryAsync(token) == 0)
            throw new RegraNegocioException("Marca não encontrada. Atualize a lista e tente novamente.");
    }

    public async Task ExcluirAsync(int codigo, CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand("DELETE FROM \"Marca\" WHERE \"Codigo\" = @codigo", connection);
        command.Parameters.AddWithValue("codigo", codigo);

        int afetadas;
        try
        {
            afetadas = await command.ExecuteNonQueryAsync(token);
        }
        catch (PostgresException pg) when (pg.SqlState == "23503")
        {
            // Regra do banco (FK com ON DELETE RESTRICT): marca com veículos não pode ser excluída.
            // Traduzido aqui porque só o repositório sabe que a operação era uma exclusão de marca.
            throw new RegraNegocioException("Não é possível excluir esta marca porque ela está vinculada a um ou mais veículos.");
        }

        if (afetadas == 0)
            throw new RegraNegocioException("Marca não encontrada. Atualize a lista e tente novamente.");
    }
}

public sealed class PostgresVeiculoRepository : IVeiculoRepository
{
    private const string SqlSelect =
        "SELECT v.\"Codigo\", v.\"Placa\", v.\"Modelo\", v.\"Ano\", m.\"Codigo\", m.\"Nome\", v.\"Tipo\", v.\"NumeroPortas\", v.\"Cilindradas\" " +
        "FROM \"Veiculo\" v JOIN \"Marca\" m ON m.\"Codigo\" = v.\"MarcaCodigo\" ORDER BY v.\"Codigo\"";

    private readonly PostgresConnectionFactory factory;
    public PostgresVeiculoRepository(PostgresConnectionFactory factory) => this.factory = factory;

    public async Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(SqlSelect, connection);
        await using var reader = await command.ExecuteReaderAsync(token);
        var result = new List<Veiculo>();
        while (await reader.ReadAsync(token))
        {
            var tipo = Enum.Parse<TipoVeiculo>(reader.GetString(6), ignoreCase: true);
            var marca = new Marca(reader.GetInt32(4), reader.GetString(5));
            var detalhe = tipo == TipoVeiculo.Carro ? reader.GetInt32(7) : reader.GetInt32(8);
            result.Add(VeiculoFactory.Criar(tipo, reader.GetInt32(0), reader.GetString(1), reader.GetString(2), reader.GetInt32(3), marca, detalhe));
        }
        return result;
    }

    public async Task<int> InserirAsync(Veiculo veiculo, CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            "INSERT INTO \"Veiculo\" (\"Placa\", \"Modelo\", \"Ano\", \"MarcaCodigo\", \"Tipo\", \"NumeroPortas\", \"Cilindradas\") " +
            "VALUES (@placa, @modelo, @ano, @marca, @tipo, @portas, @cilindradas) RETURNING \"Codigo\"", connection);
        PreencherParametros(command, veiculo);
        return (int)(await command.ExecuteScalarAsync(token) ?? 0);
    }

    public async Task AtualizarAsync(Veiculo veiculo, CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand(
            "UPDATE \"Veiculo\" SET \"Placa\"=@placa, \"Modelo\"=@modelo, \"Ano\"=@ano, \"MarcaCodigo\"=@marca, " +
            "\"Tipo\"=@tipo, \"NumeroPortas\"=@portas, \"Cilindradas\"=@cilindradas WHERE \"Codigo\"=@codigo", connection);
        PreencherParametros(command, veiculo);
        command.Parameters.AddWithValue("codigo", veiculo.Codigo);
        if (await command.ExecuteNonQueryAsync(token) == 0)
            throw new RegraNegocioException("Veículo não encontrado. Atualize a lista e tente novamente.");
    }

    public async Task ExcluirAsync(int codigo, CancellationToken token = default)
    {
        await using var connection = factory.Create(); await connection.OpenAsync(token);
        await using var command = new NpgsqlCommand("DELETE FROM \"Veiculo\" WHERE \"Codigo\"=@codigo", connection);
        command.Parameters.AddWithValue("codigo", codigo);
        if (await command.ExecuteNonQueryAsync(token) == 0)
            throw new RegraNegocioException("Veículo não encontrado. Atualize a lista e tente novamente.");
    }

    private static void PreencherParametros(NpgsqlCommand command, Veiculo veiculo)
    {
        int? portas = (veiculo as Carro)?.NumeroPortas;
        int? cilindradas = (veiculo as Motocicleta)?.Cilindradas;

        command.Parameters.AddWithValue("placa", veiculo.Placa);
        command.Parameters.AddWithValue("modelo", veiculo.Modelo);
        command.Parameters.AddWithValue("ano", veiculo.Ano);
        command.Parameters.AddWithValue("marca", veiculo.Marca.Codigo);
        command.Parameters.AddWithValue("tipo", veiculo.Tipo.ToString().ToUpperInvariant());
        command.Parameters.Add(new NpgsqlParameter("portas", NpgsqlDbType.Integer) { Value = (object?)portas ?? DBNull.Value });
        command.Parameters.Add(new NpgsqlParameter("cilindradas", NpgsqlDbType.Integer) { Value = (object?)cilindradas ?? DBNull.Value });
    }
}
