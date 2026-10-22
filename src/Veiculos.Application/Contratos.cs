using Veiculos.Domain;

namespace Veiculos.Application;

public interface IMarcaRepository
{
    Task<IReadOnlyList<Marca>> ListarAsync(CancellationToken cancellationToken = default);
    Task<int> InserirAsync(string nome, CancellationToken cancellationToken = default);
    Task AtualizarAsync(int codigo, string nome, CancellationToken cancellationToken = default);
    Task ExcluirAsync(int codigo, CancellationToken cancellationToken = default);
}

public interface IVeiculoRepository
{
    Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken cancellationToken = default);
    Task<int> InserirAsync(Veiculo veiculo, CancellationToken cancellationToken = default);
    Task AtualizarAsync(Veiculo veiculo, CancellationToken cancellationToken = default);
    Task ExcluirAsync(int codigo, CancellationToken cancellationToken = default);
}

public interface IAppLogger
{
    void Erro(Exception exception, string contexto);
}
