using Veiculos.Domain;

namespace Veiculos.Application;

/// <summary>
/// Regras de negócio do cadastro de marcas. As regras finais (nome único, marca vinculada)
/// são garantidas pelo banco; aqui só se antecipa o que dá para checar sem ir ao banco.
/// </summary>
public sealed class MarcaService
{
    public const int TamanhoMaximoNome = 120;

    private readonly IMarcaRepository repository;

    public MarcaService(IMarcaRepository repository) => this.repository = repository;

    public Task<IReadOnlyList<Marca>> ListarAsync(CancellationToken token = default) => repository.ListarAsync(token);

    public Task<int> InserirAsync(string nome, CancellationToken token = default)
    {
        var nomeValido = Validar(nome);
        return repository.InserirAsync(nomeValido, token);
    }

    public Task AtualizarAsync(int codigo, string nome, CancellationToken token = default)
    {
        if (codigo <= 0) throw new RegraNegocioException("Selecione uma marca para alterar.");
        var nomeValido = Validar(nome);
        return repository.AtualizarAsync(codigo, nomeValido, token);
    }

    public Task ExcluirAsync(int codigo, CancellationToken token = default)
    {
        if (codigo <= 0) throw new RegraNegocioException("Selecione uma marca para excluir.");
        return repository.ExcluirAsync(codigo, token);
    }

    private static string Validar(string? nome)
    {
        var limpo = (nome ?? string.Empty).Trim();
        if (limpo.Length == 0)
            throw new RegraNegocioException("O nome da marca é obrigatório.");
        if (limpo.Length > TamanhoMaximoNome)
            throw new RegraNegocioException($"O nome da marca deve ter no máximo {TamanhoMaximoNome} caracteres.");
        return limpo;
    }
}
