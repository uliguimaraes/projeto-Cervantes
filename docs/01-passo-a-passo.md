# Roteiro de desenvolvimento

## Etapa 1 — entender o enunciado

Separei requisitos funcionais (cadastros e listagens), regras de integridade (unicidade, obrigatoriedade, faixa do ano e vínculos), requisitos técnicos (Windows Forms, C#, PostgreSQL e orientação a objetos) e requisitos de entrega (SQL, testes, README e projeto compilável).

## Etapa 2 — dividir em camadas

O domínio não conhece PostgreSQL nem Windows Forms. A aplicação (regras de negócio) conhece apenas contratos. A infraestrutura implementa os contratos. A interface monta os objetos e conversa com a aplicação. Trocar a tela ou o driver do banco não exige reescrever as entidades.

## Etapa 3 — modelar a herança

`Veiculo` é abstrata e concentra os atributos comuns. `Carro` (número de portas) e `Motocicleta` (cilindradas) acrescentam o que é só delas e descrevem a si mesmas por membros abstratos (`Tipo`, `Detalhe`, `DetalheDescricao`). Uma fábrica (`VeiculoFactory`) cria a subclasse certa a partir do tipo escolhido na tela.

## Etapa 4 — colocar a integridade no PostgreSQL

As regras usam `NOT NULL`, `UNIQUE`, `CHECK`, `FOREIGN KEY ... ON DELETE RESTRICT` e triggers. O ano máximo usa o ano atual do servidor do banco, evitando uma regra congelada no código. A placa e o nome da marca são normalizados por trigger, e o nome da marca é único ignorando maiúsculas e espaços.

## Etapa 5 — registrar transações

A trigger `TR_Veiculo_Log` reage a `INSERT`, `UPDATE` e `DELETE` e grava tipo da operação, código do veículo e data/hora. É mais seguro do que depender de cada botão da tela lembrar de escrever o log.

## Etapa 6 — testar sem infraestrutura

Os testes instanciam os serviços com repositórios falsos. Assim, as regras de entrada são testadas sem banco, usuário, senha ou estado prévio. O relógio também é injetado para o teste do ano não depender da data atual.

## Etapa 7 — tratar falhas

Erros de uso e regras violadas viram mensagens específicas. Falhas inesperadas (conexão, bugs) são registradas em arquivo e a interface mostra uma mensagem curta, sem stack trace. Há uma rede de segurança global em `Program.cs` para exceções fora dos botões.

## Próximas melhorias naturais

- Criar testes de integração para confirmar triggers e constraints contra um PostgreSQL de teste.
- Adicionar migrações versionadas para alterações posteriores do banco.
- Adicionar mais tipos de veículo (ex.: caminhão) com atributos próprios.
