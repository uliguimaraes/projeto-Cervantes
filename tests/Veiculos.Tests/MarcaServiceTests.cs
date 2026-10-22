using Veiculos.Application;
using Veiculos.Domain;
using Xunit;

namespace Veiculos.Tests;

public sealed class MarcaServiceTests
{
    /// <summary>TESTE 6 — REGRA VALIDADA: "Marca: nome obrigatório". Nome vazio ou só com espaços é rejeitado e nada é gravado.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("    ")]
    public async Task Deve_rejeitar_marca_sem_nome(string nome)
    {
        var repositorio = new FakeMarcaRepository();
        var servico = new MarcaService(repositorio);

        var erro = await Assert.ThrowsAsync<RegraNegocioException>(() => servico.InserirAsync(nome));

        Assert.Equal("O nome da marca é obrigatório.", erro.Message);
        Assert.Equal(0, repositorio.Insercoes);
    }

    private sealed class FakeMarcaRepository : IMarcaRepository
    {
        public int Insercoes { get; private set; }

        public Task<IReadOnlyList<Marca>> ListarAsync(CancellationToken token = default) =>
            Task.FromResult<IReadOnlyList<Marca>>(Array.Empty<Marca>());

        public Task<int> InserirAsync(string nome, CancellationToken token = default)
        {
            Insercoes++;
            return Task.FromResult(Insercoes);
        }

        public Task AtualizarAsync(int codigo, string nome, CancellationToken token = default) => Task.CompletedTask;
        public Task ExcluirAsync(int codigo, CancellationToken token = default) => Task.CompletedTask;
    }
}
