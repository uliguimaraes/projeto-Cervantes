namespace Veiculos.Domain;

public sealed class Marca
{
    public int Codigo { get; private set; }
    public string Nome { get; private set; }

    public Marca(int codigo, string nome)
    {
        Codigo = codigo;
        Nome = nome ?? string.Empty;
    }

    public override string ToString() => Nome;
}
