# Gerenciador de Veículos

Aplicação desktop para Windows (C# / Windows Forms) com PostgreSQL para cadastro de **marcas** e **veículos**. Aplica Orientação a Objetos (classe abstrata e herança), separação em camadas, regras de negócio no banco, log de transações, tratamento de erros e testes unitários.

## Tecnologias

- C# / .NET 8 e Windows Forms
- PostgreSQL 14 ou superior
- Npgsql (ADO.NET, sem ORM)
- xUnit (testes unitários)

## Arquitetura

| Projeto | Responsabilidade |
|---|---|
| `Veiculos.WinForms` | **Interface**: telas, mensagens ao usuário e composição das dependências (`Program.cs`). |
| `Veiculos.Application` | **Regras de negócio**: serviços, validações de entrada, exceção `RegraNegocioException`, logger e interfaces dos repositórios. |
| `Veiculos.Infrastructure` | **Acesso a dados**: repositórios Npgsql e tradução dos erros do PostgreSQL em mensagens amigáveis. |
| `Veiculos.Domain` | Entidades: `Marca`, `Veiculo` (abstrata), `Carro` e `Motocicleta`. |
| `Veiculos.Tests` | Testes unitários (usam repositórios falsos, sem banco). |
| `database` | Script SQL de criação do banco. |

Fluxo de dependências: `WinForms → Application → Domain` e `Infrastructure → Application → Domain`. A interface nunca contém SQL; as regras de negócio só conhecem interfaces de repositório.

## Como executar

### 1. Pré-requisitos (Windows)

- .NET 8 SDK (ou Visual Studio 2022 com a carga **Desenvolvimento para desktop com .NET**)
- PostgreSQL 14+ em execução

### 2. Criar o banco

```bash
psql -U postgres -c "CREATE DATABASE veiculos;"
psql -U postgres -d veiculos -f database/01-criacao.sql
```

Também dá para criar o banco `veiculos` e executar `database/01-criacao.sql` pelo pgAdmin. O script pode ser executado mais de uma vez. Se o banco foi criado com uma versão antiga do script, apague-o e crie de novo.

### 3. Configurar a conexão

Sem configuração, a aplicação usa:

```text
Host=localhost;Port=5432;Database=veiculos;Username=postgres;Password=postgres
```

Para usar outra conexão (e não deixar senha no código), defina a variável de ambiente `VEICULOS_CONNECTION`. Exemplo no PowerShell:

```powershell
$env:VEICULOS_CONNECTION = "Host=localhost;Port=5432;Database=veiculos;Username=postgres;Password=SUA_SENHA"
```

### 4. Compilar, testar e executar

```bash
dotnet build
dotnet test
dotnet run --project src/Veiculos.WinForms
```

Ou abra `Veiculos.sln` no Visual Studio, defina `Veiculos.WinForms` como projeto de inicialização e execute (F5).

## Regras de negócio e onde são garantidas

O enunciado pede validação **no banco de dados**. O banco é a autoridade final; a camada de negócio antecipa as mesmas checagens apenas para responder rápido e com mensagem amigável.

| Regra | No banco (`database/01-criacao.sql`) |
|---|---|
| Marca: nome obrigatório | `NOT NULL` + `CHECK` (não aceita vazio nem só espaços) |
| Marca: nome único | índice único em `lower(btrim("Nome"))` (`Fiat` = ` FIAT `) |
| Marca vinculada a veículo não pode ser excluída | chave estrangeira com `ON DELETE RESTRICT` |
| Veículo: placa obrigatória | `NOT NULL` + `CHECK` |
| Veículo: placa única | `UNIQUE`; a trigger normaliza para maiúsculas (`abc1d23` = `ABC1D23`) |
| Veículo: modelo obrigatório | `NOT NULL` + `CHECK` |
| Veículo: ano entre 1950 e o ano atual | `CHECK ("Ano" >= 1950)` + trigger `TR_Veiculo_Validar` (o limite superior muda com o tempo) |
| Veículo deve possuir marca | `"MarcaCodigo" NOT NULL` + chave estrangeira |

## Herança

`Veiculo` é uma **classe abstrata** com Código, Placa, Modelo, Ano e Marca, mais membros abstratos que cada subclasse implementa:

```
Veiculo (abstract)
├── Carro        → NumeroPortas
└── Motocicleta  → Cilindradas
```

Na tela, o campo "Tipo" define a subclasse e o campo específico muda (Portas ou Cilindradas). No banco, a herança é mapeada em tabela única: a coluna `Tipo` identifica a subclasse e uma constraint garante que só a coluna do tipo correspondente esteja preenchida.

## Log de transações

A trigger `TR_Veiculo_Log` registra em `LogTransacao` todo `INSERT`, `UPDATE` e `DELETE` feito na tabela `Veiculo` (pela aplicação ou direto no banco), com **tipo da operação** e **data e hora**. Também guarda o código do veículo afetado.

```sql
SELECT "Codigo", "TipoOperacao", "DataHora", "VeiculoCodigo"
  FROM "LogTransacao"
 ORDER BY "DataHora" DESC;
```

## Tratamento de erros

| Situação | Comportamento |
|---|---|
| Dado inválido ou nada selecionado (`RegraNegocioException`) | Aviso com a mensagem específica (ex.: "A placa é obrigatória."). Não vai para o log de erros. |
| Regra violada no banco (placa duplicada, ano inválido, marca vinculada...) | O `DatabaseExceptionMapper` traduz o código do PostgreSQL em mensagem amigável. Não vai para o log de erros. |
| Banco fora do ar, senha errada, banco inexistente | Mensagem orientando a verificar o PostgreSQL e a conexão; **registrado no log**. |
| Qualquer outra exceção, inclusive fora dos botões | Capturada em `Program.cs` (`ThreadException` e `UnhandledException`): mensagem amigável e **registro no log**. A tela nunca mostra stack trace. |

Os erros inesperados são gravados em `logs/erros.log`, ao lado do executável, com data/hora, contexto e stack trace. O logger nunca lança exceção.

## Testes unitários

Em `tests/Veiculos.Tests`, cada teste indica no comentário a regra que valida:

| Teste | Regra validada |
|---|---|
| `Deve_rejeitar_placa_vazia` | Veículo: placa obrigatória |
| `Deve_rejeitar_veiculo_sem_marca` | Veículo deve possuir marca |
| `Deve_rejeitar_ano_fora_do_intervalo` (1949 e ano atual + 1) | Veículo: ano entre 1950 e o ano atual |
| `Deve_aceitar_anos_nos_limites_do_intervalo` (1950 e ano atual) | Veículo: ano entre 1950 e o ano atual |
| `Deve_criar_a_subclasse_conforme_o_tipo` | Herança: `Carro` e `Motocicleta` |
| `Deve_rejeitar_marca_sem_nome` | Marca: nome obrigatório |

Os testes de ano usam um relógio fixo, então não quebram quando o ano muda.

## Decisões de projeto

- **Sem ORM**: ADO.NET com Npgsql e SQL parametrizado, deixando explícito o que roda no banco.
- **Validação em duas camadas**: o banco garante as regras; a camada de negócio as antecipa para dar melhor experiência e ser testável sem banco.
- **Log por trigger**: o registro acontece na mesma transação da operação, mesmo que a alteração seja feita fora da tela.
- **Nomes das tabelas** em `PascalCase` entre aspas (`"Marca"`, `"Veiculo"`, `"LogTransacao"`), iguais aos do enunciado.
