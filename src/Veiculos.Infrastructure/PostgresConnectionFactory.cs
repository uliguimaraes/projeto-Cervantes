using Npgsql;

namespace Veiculos.Infrastructure;

public sealed class PostgresConnectionFactory
{
    private readonly string connectionString;
    public PostgresConnectionFactory(string connectionString) => this.connectionString = connectionString;
    public NpgsqlConnection Create() => new(connectionString);
}
