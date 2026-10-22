using Veiculos.Domain;

namespace Veiculos.Application;

/// <summary>
/// Regras de negócio do cadastro de veículos. As regras finais (placa única, ano, marca...)
/// são garantidas pelo banco; aqui elas são antecipadas para dar resposta imediata e mensagem amigável.
/// </summary>
public sealed class VeiculoService
{
    public const int AnoMinimo = 1950;
    public const int TamanhoMaximoPlaca = 10;
    public const int TamanhoMaximoModelo = 120;

    private readonly IVeiculoRepository repository;
    private readonly TimeProvider relogio;

    /// <param name="relogio">Fonte de data/hora. Opcional; nos testes permite fixar o "ano atual".</param>
    public VeiculoService(IVeiculoRepository repository, TimeProvider? relogio = null)
    {
        this.repository = repository;
        this.relogio = relogio ?? TimeProvider.System;
    }

    public Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken token = default) => repository.ListarAsync(token);

    public Task<int> InserirAsync(TipoVeiculo tipo, string placa, string modelo, int ano, int marcaCodigo, int detalhe, CancellationToken token = default)
    {
        var veiculo = Montar(0, tipo, placa, modelo, ano, marcaCodigo, detalhe);
        return repository.InserirAsync(veiculo, token);
    }

    public Task AtualizarAsync(int codigo, TipoVeiculo tipo, string placa, string modelo, int ano, int marcaCodigo, int detalhe, CancellationToken token = default)
    {
        if (codigo <= 0) throw new RegraNegocioException("Selecione um veículo para alterar.");
        var veiculo = Montar(codigo, tipo, placa, modelo, ano, marcaCodigo, detalhe);
        return repository.AtualizarAsync(veiculo, token);
    }

    public Task ExcluirAsync(int codigo, CancellationToken token = default)
    {
        if (codigo <= 0) throw new RegraNegocioException("Selecione um veículo para excluir.");
        return repository.ExcluirAsync(codigo, token);
    }

    private Veiculo Montar(int codigo, TipoVeiculo tipo, string? placa, string? modelo, int ano, int marcaCodigo, int detalhe)
    {
        var placaLimpa = (placa ?? string.Empty).Trim().ToUpperInvariant();
        var modeloLimpo = (modelo ?? string.Empty).Trim();

        if (placaLimpa.Length == 0)
            throw new RegraNegocioException("A placa é obrigatória.");
        if (placaLimpa.Length > TamanhoMaximoPlaca)
            throw new RegraNegocioException($"A placa deve ter no máximo {TamanhoMaximoPlaca} caracteres.");
        if (modeloLimpo.Length == 0)
            throw new RegraNegocioException("O modelo é obrigatório.");
        if (modeloLimpo.Length > TamanhoMaximoModelo)
            throw new RegraNegocioException($"O modelo deve ter no máximo {TamanhoMaximoModelo} caracteres.");

        var anoAtual = relogio.GetLocalNow().Year;
        if (ano < AnoMinimo || ano > anoAtual)
            throw new RegraNegocioException($"O ano do veículo deve estar entre {AnoMinimo} e {anoAtual}.");

        if (marcaCodigo <= 0)
            throw new RegraNegocioException("O veículo deve possuir uma marca.");

        switch (tipo)
        {
            case TipoVeiculo.Carro when detalhe is < 1 or > 6:
                throw new RegraNegocioException("O número de portas do carro deve estar entre 1 e 6.");
            case TipoVeiculo.Motocicleta when detalhe <= 0:
                throw new RegraNegocioException("A cilindrada da motocicleta deve ser maior que zero.");
        }

        // A marca viaja só com o código; o nome é preenchido pelo banco na leitura.
        return VeiculoFactory.Criar(tipo, codigo, placaLimpa, modeloLimpo, ano, new Marca(marcaCodigo, string.Empty), detalhe);
    }
}
