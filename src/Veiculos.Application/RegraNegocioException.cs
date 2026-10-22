namespace Veiculos.Application;

/// <summary>
/// Violação de uma regra de negócio ou uso incorreto da tela (campo vazio, nada selecionado...).
/// A mensagem já é amigável e pode ser mostrada direto ao usuário.
/// </summary>
public sealed class RegraNegocioException : Exception
{
    public RegraNegocioException(string mensagem) : base(mensagem) { }
}
