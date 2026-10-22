namespace Veiculos.Domain;

/// <summary>Identifica a subclasse concreta de <see cref="Veiculo"/>.</summary>
public enum TipoVeiculo
{
    Carro = 1,
    Motocicleta = 2
}

/// <summary>
/// Classe abstrata com os campos comuns a todo veículo.
/// Cada subclasse acrescenta o seu atributo específico e descreve a si mesma (polimorfismo).
/// </summary>
public abstract class Veiculo
{
    public int Codigo { get; private set; }
    public string Placa { get; private set; }
    public string Modelo { get; private set; }
    public int Ano { get; private set; }
    public Marca Marca { get; private set; }

    /// <summary>Tipo concreto do veículo (define a subclasse).</summary>
    public abstract TipoVeiculo Tipo { get; }

    /// <summary>Valor do atributo específico: portas (Carro) ou cilindradas (Motocicleta).</summary>
    public abstract int Detalhe { get; }

    /// <summary>Atributo específico em texto, para exibição na tela.</summary>
    public abstract string DetalheDescricao { get; }

    protected Veiculo(int codigo, string placa, string modelo, int ano, Marca marca)
    {
        Codigo = codigo;
        Placa = placa ?? string.Empty;
        Modelo = modelo ?? string.Empty;
        Ano = ano;
        Marca = marca;
    }
}

public sealed class Carro : Veiculo
{
    public int NumeroPortas { get; private set; }

    public Carro(int codigo, string placa, string modelo, int ano, Marca marca, int numeroPortas)
        : base(codigo, placa, modelo, ano, marca) => NumeroPortas = numeroPortas;

    public override TipoVeiculo Tipo => TipoVeiculo.Carro;
    public override int Detalhe => NumeroPortas;
    public override string DetalheDescricao => $"{NumeroPortas} portas";
}

public sealed class Motocicleta : Veiculo
{
    public int Cilindradas { get; private set; }

    public Motocicleta(int codigo, string placa, string modelo, int ano, Marca marca, int cilindradas)
        : base(codigo, placa, modelo, ano, marca) => Cilindradas = cilindradas;

    public override TipoVeiculo Tipo => TipoVeiculo.Motocicleta;
    public override int Detalhe => Cilindradas;
    public override string DetalheDescricao => $"{Cilindradas} cc";
}

/// <summary>Cria a subclasse correta a partir do tipo.</summary>
public static class VeiculoFactory
{
    public static Veiculo Criar(TipoVeiculo tipo, int codigo, string placa, string modelo, int ano, Marca marca, int detalhe) => tipo switch
    {
        TipoVeiculo.Carro => new Carro(codigo, placa, modelo, ano, marca, detalhe),
        TipoVeiculo.Motocicleta => new Motocicleta(codigo, placa, modelo, ano, marca, detalhe),
        _ => throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de veículo desconhecido.")
    };
}
