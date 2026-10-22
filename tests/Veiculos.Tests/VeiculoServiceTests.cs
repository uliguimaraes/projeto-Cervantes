using Veiculos.Application;
using Veiculos.Domain;
using Xunit;

namespace Veiculos.Tests;

/// <summary>
/// Testes das regras de negócio do cadastro de veículos.
/// Usam um repositório falso (em memória), então não dependem do PostgreSQL.
/// </summary>
public sealed class VeiculoServiceTests
{
    private const int AnoDoRelogio = 2026;

    private static VeiculoService CriarServico(FakeVeiculoRepository repositorio) =>
        new(repositorio, new RelogioFixo(AnoDoRelogio));

    /// <summary>TESTE 1 — REGRA VALIDADA: "Placa obrigatória". Placa vazia ou só com espaços é rejeitada e nada é gravado.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Deve_rejeitar_placa_vazia(string placa)
    {
        var repositorio = new FakeVeiculoRepository();
        var servico = CriarServico(repositorio);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => servico.InserirAsync(TipoVeiculo.Carro, placa, "Civic", 2020, 1, 4));

        Assert.Equal("A placa é obrigatória.", erro.Message);
        Assert.Equal(0, repositorio.Insercoes);
    }

    /// <summary>TESTE 2 — REGRA VALIDADA: "Veículo deve possuir marca". Sem marca (código 0) é rejeitado e nada é gravado.</summary>
    [Fact]
    public async Task Deve_rejeitar_veiculo_sem_marca()
    {
        var repositorio = new FakeVeiculoRepository();
        var servico = CriarServico(repositorio);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => servico.InserirAsync(TipoVeiculo.Carro, "ABC1D23", "Civic", 2020, 0, 4));

        Assert.Equal("O veículo deve possuir uma marca.", erro.Message);
        Assert.Equal(0, repositorio.Insercoes);
    }

    /// <summary>TESTE 3 — REGRA VALIDADA: "Ano entre 1950 e o ano atual" (fora do intervalo é rejeitado).</summary>
    [Theory]
    [InlineData(1949)]
    [InlineData(AnoDoRelogio + 1)]
    public async Task Deve_rejeitar_ano_fora_do_intervalo(int ano)
    {
        var repositorio = new FakeVeiculoRepository();
        var servico = CriarServico(repositorio);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(
            () => servico.InserirAsync(TipoVeiculo.Motocicleta, "ABC1D23", "CG 160", ano, 1, 160));

        Assert.Equal($"O ano do veículo deve estar entre 1950 e {AnoDoRelogio}.", erro.Message);
        Assert.Equal(0, repositorio.Insercoes);
    }

    /// <summary>TESTE 4 — REGRA VALIDADA: "Ano entre 1950 e o ano atual" (os limites 1950 e ano atual são aceitos).</summary>
    [Theory]
    [InlineData(1950)]
    [InlineData(AnoDoRelogio)]
    public async Task Deve_aceitar_anos_nos_limites_do_intervalo(int ano)
    {
        var repositorio = new FakeVeiculoRepository();
        var servico = CriarServico(repositorio);

        await servico.InserirAsync(TipoVeiculo.Carro, "abc1d23", "Civic", ano, 1, 4);

        Assert.Equal(1, repositorio.Insercoes);
        Assert.Equal("ABC1D23", repositorio.Ultimo!.Placa); // placa é normalizada
    }

    /// <summary>TESTE 5 — HERANÇA: o tipo escolhido gera a subclasse correta de Veiculo.</summary>
    [Fact]
    public async Task Deve_criar_a_subclasse_conforme_o_tipo()
    {
        var repositorio = new FakeVeiculoRepository();
        var servico = CriarServico(repositorio);

        await servico.InserirAsync(TipoVeiculo.Carro, "AAA1A11", "Argo", 2022, 1, 4);
        Assert.IsType<Carro>(repositorio.Ultimo);

        await servico.InserirAsync(TipoVeiculo.Motocicleta, "BBB2B22", "CG 160", 2022, 1, 160);
        Assert.IsType<Motocicleta>(repositorio.Ultimo);
    }

    private sealed class RelogioFixo : TimeProvider
    {
        private readonly DateTimeOffset agora;
        public RelogioFixo(int ano) => agora = new DateTimeOffset(ano, 6, 15, 12, 0, 0, TimeSpan.Zero);
        public override DateTimeOffset GetUtcNow() => agora;
        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.Utc;
    }

    private sealed class FakeVeiculoRepository : IVeiculoRepository
    {
        public int Insercoes { get; private set; }
        public Veiculo? Ultimo { get; private set; }

        public Task<IReadOnlyList<Veiculo>> ListarAsync(CancellationToken token = default) =>
            Task.FromResult<IReadOnlyList<Veiculo>>(Array.Empty<Veiculo>());

        public Task<int> InserirAsync(Veiculo veiculo, CancellationToken token = default)
        {
            Insercoes++;
            Ultimo = veiculo;
            return Task.FromResult(Insercoes);
        }

        public Task AtualizarAsync(Veiculo veiculo, CancellationToken token = default) => Task.CompletedTask;
        public Task ExcluirAsync(int codigo, CancellationToken token = default) => Task.CompletedTask;
    }
}
