# Desafio Dev (.NET 8)

Aplicação de console com três opções:

1. Calcula a comissão de cada venda e exibe o total por vendedor.
2. Registra entradas e saídas de produtos, impede saldo negativo e mostra o saldo atualizado.
3. Calcula juros simples de 2,5% do valor original por dia corrido de atraso.

Para executar, use .NET 8 SDK na pasta do projeto:

```powershell
dotnet run
```

Os dados iniciais ficam em `Dados/vendas.json` e `Dados/estoque.json`. O histórico numérico de movimentações fica em `Dados/movimentacoes.json`. Durante a execução, a aplicação lê e grava os arquivos na pasta `Dados` junto ao executável (por exemplo, `bin/Debug/net8.0/Dados`).

Para calcular os juros, informe a data no formato `dd/MM/aaaa`. Os juros são calculados somente quando a data de vencimento já passou; vencimentos hoje ou futuros não geram juros.