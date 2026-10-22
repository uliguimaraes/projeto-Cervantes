using Veiculos.Application;
using Veiculos.Domain;
using Veiculos.Infrastructure;

namespace Veiculos.WinForms;

public sealed class MainForm : Form
{
    private readonly MarcaService marcas;
    private readonly VeiculoService veiculos;
    private readonly IAppLogger logger;

    private readonly DataGridView tabelaMarcas = CriarTabela();
    private readonly DataGridView tabelaVeiculos = CriarTabela();

    private readonly TextBox nome = new() { MaxLength = MarcaService.TamanhoMaximoNome };

    private readonly ComboBox tipo = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 110 };
    private readonly TextBox placa = new() { MaxLength = VeiculoService.TamanhoMaximoPlaca, CharacterCasing = CharacterCasing.Upper };
    private readonly TextBox modelo = new() { MaxLength = VeiculoService.TamanhoMaximoModelo };
    private readonly NumericUpDown ano = new() { Minimum = 1900, Maximum = 2100, Value = DateTime.Now.Year };
    private readonly ComboBox marca = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly Label rotuloDetalhe = new() { AutoSize = true, Margin = new Padding(3, 8, 3, 3) };
    private readonly NumericUpDown detalhe = new() { Minimum = 1, Maximum = 6, Value = 4 };

    private List<Marca> marcasCarregadas = new();
    private int? marcaSelecionada;
    private int? veiculoSelecionado;
    private bool carregando;

    public MainForm(MarcaService marcas, VeiculoService veiculos, IAppLogger logger)
    {
        this.marcas = marcas;
        this.veiculos = veiculos;
        this.logger = logger;
        Text = "Gerenciador de Veículos";
        Width = 1000;
        Height = 650;
        StartPosition = FormStartPosition.CenterScreen;

        tipo.DataSource = Enum.GetValues<TipoVeiculo>();
        tipo.SelectedIndexChanged += (_, _) => AtualizarCampoDetalhe();
        AtualizarCampoDetalhe();

        var abas = new TabControl { Dock = DockStyle.Fill };
        abas.TabPages.Add(CriarAbaMarcas());
        abas.TabPages.Add(CriarAbaVeiculos());
        Controls.Add(abas);
        Shown += async (_, _) => await ExecutarAsync(AtualizarTudoAsync);
    }

    private TabPage CriarAbaMarcas()
    {
        var aba = new TabPage("Marcas");
        var formulario = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 70, Padding = new Padding(8), WrapContents = false };
        formulario.Controls.Add(new Label { Text = "Nome:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        nome.Width = 220;
        formulario.Controls.Add(nome);
        formulario.Controls.Add(CriarBotao("Inserir", async () => await ExecutarAsync(InserirMarcaAsync)));
        formulario.Controls.Add(CriarBotao("Alterar", async () => await ExecutarAsync(AlterarMarcaAsync)));
        formulario.Controls.Add(CriarBotao("Excluir", async () => await ExecutarAsync(ExcluirMarcaAsync)));
        formulario.Controls.Add(CriarBotao("Limpar", LimparMarca));
        tabelaMarcas.SelectionChanged += (_, _) => SelecionarMarca();
        aba.Controls.Add(tabelaMarcas);
        aba.Controls.Add(formulario);
        return aba;
    }

    private TabPage CriarAbaVeiculos()
    {
        var aba = new TabPage("Veículos");
        var formulario = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 100, Padding = new Padding(8), WrapContents = true };
        formulario.Controls.Add(new Label { Text = "Tipo:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        formulario.Controls.Add(tipo);
        formulario.Controls.Add(new Label { Text = "Placa:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        placa.Width = 90; formulario.Controls.Add(placa);
        formulario.Controls.Add(new Label { Text = "Modelo:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        modelo.Width = 160; formulario.Controls.Add(modelo);
        formulario.Controls.Add(new Label { Text = "Ano:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        ano.Width = 75; formulario.Controls.Add(ano);
        formulario.Controls.Add(new Label { Text = "Marca:", AutoSize = true, Margin = new Padding(3, 8, 3, 3) });
        marca.Width = 140; formulario.Controls.Add(marca);
        formulario.Controls.Add(rotuloDetalhe);
        detalhe.Width = 75; formulario.Controls.Add(detalhe);
        formulario.Controls.Add(CriarBotao("Inserir", async () => await ExecutarAsync(InserirVeiculoAsync)));
        formulario.Controls.Add(CriarBotao("Alterar", async () => await ExecutarAsync(AlterarVeiculoAsync)));
        formulario.Controls.Add(CriarBotao("Excluir", async () => await ExecutarAsync(ExcluirVeiculoAsync)));
        formulario.Controls.Add(CriarBotao("Limpar", LimparVeiculo));
        tabelaVeiculos.SelectionChanged += (_, _) => SelecionarVeiculo();
        aba.Controls.Add(tabelaVeiculos);
        aba.Controls.Add(formulario);
        return aba;
    }

    /// <summary>Ajusta rótulo, limites e valor padrão do campo específico conforme o tipo escolhido.</summary>
    private void AtualizarCampoDetalhe()
    {
        if (tipo.SelectedItem is not TipoVeiculo selecionado) return;

        if (selecionado == TipoVeiculo.Carro)
        {
            rotuloDetalhe.Text = "Portas:";
            detalhe.Minimum = 1;
            detalhe.Maximum = 6;
            detalhe.Value = 4;
        }
        else
        {
            rotuloDetalhe.Text = "Cilindradas (cc):";
            detalhe.Minimum = 1;
            detalhe.Maximum = 20000;
            detalhe.Value = 150;
        }
    }

    private async Task AtualizarTudoAsync()
    {
        var listaMarcas = (await marcas.ListarAsync()).ToList();
        var listaVeiculos = (await veiculos.ListarAsync()).ToList();

        // Durante o carregamento o grid seleciona a 1ª linha sozinho; o flag evita preencher o formulário por causa disso.
        carregando = true;
        try
        {
            marcasCarregadas = listaMarcas;
            marca.DataSource = listaMarcas;
            marca.DisplayMember = nameof(Marca.Nome);
            marca.SelectedIndex = -1;

            tabelaMarcas.DataSource = listaMarcas;
            ConfigurarColunasMarca();
            tabelaMarcas.ClearSelection();

            tabelaVeiculos.DataSource = listaVeiculos;
            ConfigurarColunasVeiculo();
            tabelaVeiculos.ClearSelection();
        }
        finally
        {
            carregando = false;
        }
    }

    private async Task InserirMarcaAsync()
    {
        await marcas.InserirAsync(nome.Text);
        LimparMarca();
        await AtualizarTudoAsync();
    }

    private async Task AlterarMarcaAsync()
    {
        if (marcaSelecionada is null) throw new RegraNegocioException("Selecione uma marca na lista para alterar.");
        await marcas.AtualizarAsync(marcaSelecionada.Value, nome.Text);
        LimparMarca();
        await AtualizarTudoAsync();
    }

    private async Task ExcluirMarcaAsync()
    {
        if (marcaSelecionada is null) throw new RegraNegocioException("Selecione uma marca na lista para excluir.");
        if (!Confirmar("Deseja realmente excluir a marca selecionada?")) return;
        await marcas.ExcluirAsync(marcaSelecionada.Value);
        LimparMarca();
        await AtualizarTudoAsync();
    }

    private async Task InserirVeiculoAsync()
    {
        await veiculos.InserirAsync(TipoSelecionado(), placa.Text, modelo.Text, (int)ano.Value, CodigoMarcaSelecionada(), (int)detalhe.Value);
        LimparVeiculo();
        await AtualizarTudoAsync();
    }

    private async Task AlterarVeiculoAsync()
    {
        if (veiculoSelecionado is null) throw new RegraNegocioException("Selecione um veículo na lista para alterar.");
        await veiculos.AtualizarAsync(veiculoSelecionado.Value, TipoSelecionado(), placa.Text, modelo.Text, (int)ano.Value, CodigoMarcaSelecionada(), (int)detalhe.Value);
        LimparVeiculo();
        await AtualizarTudoAsync();
    }

    private async Task ExcluirVeiculoAsync()
    {
        if (veiculoSelecionado is null) throw new RegraNegocioException("Selecione um veículo na lista para excluir.");
        if (!Confirmar("Deseja realmente excluir o veículo selecionado?")) return;
        await veiculos.ExcluirAsync(veiculoSelecionado.Value);
        LimparVeiculo();
        await AtualizarTudoAsync();
    }

    private TipoVeiculo TipoSelecionado() =>
        tipo.SelectedItem is TipoVeiculo selecionado ? selecionado : throw new RegraNegocioException("Selecione o tipo do veículo.");

    // Sem marca escolhida devolve 0; a validação "veículo deve possuir marca" acontece no serviço e no banco.
    private int CodigoMarcaSelecionada() => (marca.SelectedItem as Marca)?.Codigo ?? 0;

    private bool Confirmar(string pergunta) =>
        MessageBox.Show(this, pergunta, "Confirmar exclusão", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;

    private void SelecionarMarca()
    {
        if (carregando || tabelaMarcas.SelectedRows.Count == 0) return;
        if (tabelaMarcas.SelectedRows[0].DataBoundItem is not Marca item) return;
        marcaSelecionada = item.Codigo;
        nome.Text = item.Nome;
    }

    private void SelecionarVeiculo()
    {
        if (carregando || tabelaVeiculos.SelectedRows.Count == 0) return;
        if (tabelaVeiculos.SelectedRows[0].DataBoundItem is not Veiculo item) return;

        veiculoSelecionado = item.Codigo;
        tipo.SelectedItem = item.Tipo;          // dispara AtualizarCampoDetalhe (limites e valor padrão)
        placa.Text = item.Placa;
        modelo.Text = item.Modelo;
        ano.Value = Math.Clamp(item.Ano, (int)ano.Minimum, (int)ano.Maximum);
        marca.SelectedItem = marcasCarregadas.FirstOrDefault(m => m.Codigo == item.Marca.Codigo);
        detalhe.Value = Math.Clamp(item.Detalhe, (int)detalhe.Minimum, (int)detalhe.Maximum);
    }

    private void LimparMarca()
    {
        marcaSelecionada = null;
        nome.Clear();
        tabelaMarcas.ClearSelection();
    }

    private void LimparVeiculo()
    {
        veiculoSelecionado = null;
        tipo.SelectedIndex = 0;
        placa.Clear();
        modelo.Clear();
        ano.Value = DateTime.Now.Year;
        marca.SelectedIndex = -1;
        tabelaVeiculos.ClearSelection();
    }

    /// <summary>
    /// Executa uma ação da tela com tratamento de erros: mensagem amigável ao usuário e,
    /// só para erros inesperados (conexão, bugs), registro no arquivo de log.
    /// </summary>
    private async Task ExecutarAsync(Func<Task> acao)
    {
        try
        {
            await acao();
        }
        catch (Exception ex)
        {
            var esperado = DatabaseExceptionMapper.EhErroEsperado(ex);
            if (!esperado) logger.Erro(ex, "operação na tela");

            MessageBox.Show(
                this,
                DatabaseExceptionMapper.ParaUsuario(ex),
                esperado ? "Atenção" : "Não foi possível concluir",
                MessageBoxButtons.OK,
                esperado ? MessageBoxIcon.Warning : MessageBoxIcon.Error);
        }
    }

    private static Button CriarBotao(string texto, Func<Task> acao)
    {
        var botao = new Button { Text = texto, AutoSize = true };
        botao.Click += async (_, _) => await acao();
        return botao;
    }

    private static Button CriarBotao(string texto, Action acao)
    {
        var botao = new Button { Text = texto, AutoSize = true };
        botao.Click += (_, _) => acao();
        return botao;
    }

    private static DataGridView CriarTabela() => new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        AutoGenerateColumns = true,
        AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
        SelectionMode = DataGridViewSelectionMode.FullRowSelect,
        MultiSelect = false,
        AllowUserToAddRows = false
    };

    private static void Titulo(DataGridView tabela, string coluna, string texto) =>
        tabela.Columns[coluna]!.HeaderText = texto;

    private void ConfigurarColunasMarca()
    {
        if (tabelaMarcas.Columns.Count == 0) return;
        Titulo(tabelaMarcas, nameof(Marca.Codigo), "Código");
        Titulo(tabelaMarcas, nameof(Marca.Nome), "Nome");
    }

    private void ConfigurarColunasVeiculo()
    {
        if (tabelaVeiculos.Columns.Count == 0) return;
        Titulo(tabelaVeiculos, nameof(Veiculo.Codigo), "Código");
        Titulo(tabelaVeiculos, nameof(Veiculo.Tipo), "Tipo");
        Titulo(tabelaVeiculos, nameof(Veiculo.Placa), "Placa");
        Titulo(tabelaVeiculos, nameof(Veiculo.Modelo), "Modelo");
        Titulo(tabelaVeiculos, nameof(Veiculo.Ano), "Ano");
        Titulo(tabelaVeiculos, nameof(Veiculo.Marca), "Marca");
        Titulo(tabelaVeiculos, nameof(Veiculo.DetalheDescricao), "Detalhe");
        tabelaVeiculos.Columns[nameof(Veiculo.Detalhe)]!.Visible = false;
    }
}
