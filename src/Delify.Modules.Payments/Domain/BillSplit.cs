namespace Delify.Modules.Payments.Domain;

public static class BillSplit
{
    /// <summary>Máximo de partes aceito numa divisão de comanda.</summary>
    public const int MaxShares = 30;

    /// <summary>
    /// Reparte um total em N partes iguais SEM perder nem inventar centavo:
    /// a soma das partes é sempre exatamente o total. Divisões que não fecham
    /// (R$ 100,00 ÷ 3) distribuem os centavos restantes um a um nas primeiras
    /// partes — R$ 33,34 / R$ 33,33 / R$ 33,33 — em vez de arredondar cada
    /// parte isoladamente, o que sobraria ou faltaria dinheiro.
    /// </summary>
    public static decimal[] Even(decimal total, int shares)
    {
        if (shares < 2) throw new ArgumentOutOfRangeException(nameof(shares), "Divisão exige ao menos 2 partes.");
        if (shares > MaxShares) throw new ArgumentOutOfRangeException(nameof(shares), $"Máximo de {MaxShares} partes.");
        if (total <= 0) throw new ArgumentOutOfRangeException(nameof(total), "Total deve ser positivo.");

        var totalCents = (long)Math.Round(total * 100m, MidpointRounding.AwayFromZero);
        var baseCents = totalCents / shares;
        var leftover = (int)(totalCents % shares);

        var result = new decimal[shares];
        for (var i = 0; i < shares; i++)
            result[i] = (baseCents + (i < leftover ? 1 : 0)) / 100m;

        return result;
    }
}
