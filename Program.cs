using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DesafioDev;

internal static class Program
{
    private static readonly string DataDirectory = Path.Combine(AppContext.BaseDirectory, "Dados");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static void Main()
    {
        Directory.CreateDirectory(DataDirectory);
        EnsureDataFile("vendas.json", "{\"vendas\":[]}");
        EnsureDataFile("estoque.json", "{\"estoque\":[]}");
        EnsureDataFile("movimentacoes.json", "[]");

        while (true)
        {
            Console.WriteLine("\n=== Desafio Dev ===");
            Console.WriteLine("1. Calcular comissões");
            Console.WriteLine("2. Registrar movimentação de estoque");
            Console.WriteLine("3. Calcular juros por atraso");
            Console.WriteLine("0. Sair");
            Console.Write("Escolha uma opção: ");

            switch (Console.ReadLine()?.Trim())
            {
                case "1": CalcularComissoes(); break;
                case "2": RegistrarMovimentacao(); break;
                case "3": CalcularJuros(); break;
                case "0": return;
                default: Console.WriteLine("Opção inválida."); break;
            }
        }
    }

    private static void CalcularComissoes()
    {
        var dados = ReadJson<VendasData>("vendas.json");
        if (dados.Vendas.Count == 0)
        {
            Console.WriteLine("Nenhuma venda encontrada em Dados/vendas.json.");
            return;
        }

        var totais = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var venda in dados.Vendas)
        {
            var percentual = venda.Valor < 100m ? 0m : venda.Valor < 500m ? 0.01m : 0.05m;
            var comissao = decimal.Round(venda.Valor * percentual, 2, MidpointRounding.AwayFromZero);
            totais[venda.Vendedor] = totais.GetValueOrDefault(venda.Vendedor) + comissao;
        }

        Console.WriteLine("\nComissão total por vendedor:");
        foreach (var (vendedor, total) in totais.OrderBy(item => item.Key))
            Console.WriteLine($"{vendedor}: {FormatarMoeda(total)}");
    }

    private static void RegistrarMovimentacao()
    {
        var dados = ReadJson<EstoqueData>("estoque.json");
        if (dados.Estoque.Count == 0)
        {
            Console.WriteLine("Nenhum produto encontrado em Dados/estoque.json.");
            return;
        }

        Console.WriteLine("\nProdutos:");
        foreach (var produto in dados.Estoque)
            Console.WriteLine($"{produto.CodigoProduto} - {produto.DescricaoProduto} (saldo: {produto.Estoque})");

        var codigo = LerInteiro("Código do produto: ");
        var item = dados.Estoque.SingleOrDefault(produto => produto.CodigoProduto == codigo);
        if (item is null)
        {
            Console.WriteLine("Produto não encontrado.");
            return;
        }

        Console.Write("Tipo (E = entrada, S = saída): ");
        var tipo = Console.ReadLine()?.Trim().ToUpperInvariant();
        if (tipo is not ("E" or "S"))
        {
            Console.WriteLine("Tipo inválido.");
            return;
        }

        var quantidade = LerInteiro("Quantidade: ");
        if (quantidade <= 0)
        {
            Console.WriteLine("A quantidade deve ser maior que zero.");
            return;
        }

        if (tipo == "S" && quantidade > item.Estoque)
        {
            Console.WriteLine("Estoque insuficiente para essa saída.");
            return;
        }

        Console.Write("Descrição da movimentação: ");
        var descricao = Console.ReadLine()?.Trim();
        if (string.IsNullOrWhiteSpace(descricao))
        {
            Console.WriteLine("A descrição é obrigatória.");
            return;
        }

        var movimentacoes = ReadJson<List<MovimentacaoEstoque>>("movimentacoes.json");
        var proximoId = movimentacoes.Count == 0 ? 1 : movimentacoes.Max(movimento => movimento.Id) + 1;
        item.Estoque += tipo == "E" ? quantidade : -quantidade;
        movimentacoes.Add(new MovimentacaoEstoque
        {
            Id = proximoId,
            CodigoProduto = item.CodigoProduto,
            Tipo = tipo == "E" ? "Entrada" : "Saída",
            Descricao = descricao,
            Quantidade = quantidade,
            Data = DateTime.Now
        });

        WriteJson("estoque.json", dados);
        WriteJson("movimentacoes.json", movimentacoes);
        Console.WriteLine($"Movimentação #{proximoId} registrada. Saldo final de {item.DescricaoProduto}: {item.Estoque} unidade(s).");
    }

    private static void CalcularJuros()
    {
        var valor = LerDecimal("Valor original (R$): ");
        if (valor <= 0m)
        {
            Console.WriteLine("O valor deve ser maior que zero.");
            return;
        }

        Console.Write("Data de vencimento (dd/MM/aaaa): ");
        if (!DateOnly.TryParseExact(Console.ReadLine()?.Trim(), "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out var vencimento))
        {
            Console.WriteLine("Data inválida. Use o formato dd/MM/aaaa.");
            return;
        }

        var diasAtraso = Math.Max(0, DateOnly.FromDateTime(DateTime.Today).DayNumber - vencimento.DayNumber);
        var juros = decimal.Round(valor * 0.025m * diasAtraso, 2, MidpointRounding.AwayFromZero);
        Console.WriteLine($"Dias de atraso: {diasAtraso}");
        Console.WriteLine($"Juros: {FormatarMoeda(juros)}");
        Console.WriteLine($"Total atualizado: {FormatarMoeda(valor + juros)}");
    }

    private static T ReadJson<T>(string fileName)
    {
        var path = Path.Combine(DataDirectory, fileName);
        return JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException($"O arquivo {fileName} está vazio ou inválido.");
    }

    private static void WriteJson<T>(string fileName, T data)
    {
        var path = Path.Combine(DataDirectory, fileName);
        File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
    }

    private static void EnsureDataFile(string fileName, string emptyContent)
    {
        var path = Path.Combine(DataDirectory, fileName);
        if (!File.Exists(path))
            File.WriteAllText(path, emptyContent);
    }

    private static int LerInteiro(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            if (int.TryParse(Console.ReadLine(), out var valor))
                return valor;
            Console.WriteLine("Digite um número inteiro válido.");
        }
    }

    private static decimal LerDecimal(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);
            var texto = Console.ReadLine()?.Trim().Replace(',', '.');
            if (decimal.TryParse(texto, NumberStyles.Number, CultureInfo.InvariantCulture, out var valor))
                return valor;
            Console.WriteLine("Digite um valor numérico válido.");
        }
    }

    private static string FormatarMoeda(decimal valor) =>
        valor.ToString("C2", CultureInfo.GetCultureInfo("pt-BR"));
}