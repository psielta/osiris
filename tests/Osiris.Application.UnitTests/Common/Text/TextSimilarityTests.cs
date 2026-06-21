using Osiris.Application.Common.Text;

namespace Osiris.Application.UnitTests.Common.Text;

public sealed class TextSimilarityTests
{
    [Fact]
    public void Jaccard_IdenticalStrings_ReturnsOne()
    {
        Assert.Equal(1.0, TextSimilarity.Jaccard("Mercado Livre", "Mercado Livre"));
    }

    [Fact]
    public void Jaccard_DisjointTokens_ReturnsZero()
    {
        Assert.Equal(0.0, TextSimilarity.Jaccard("aluguel", "salario"));
    }

    [Fact]
    public void Jaccard_PartialOverlap_ReturnsRatio()
    {
        // A = {mercado, livre, pagamento}, B = {mercado, pagamento}; intersection 2, union 3.
        Assert.Equal(2.0 / 3.0, TextSimilarity.Jaccard("Mercado Livre Pagamento", "Mercado Pagamento"), 3);
    }

    [Fact]
    public void Jaccard_IsAccentInsensitive()
    {
        Assert.Equal(1.0, TextSimilarity.Jaccard("São José", "sao jose"));
    }

    [Fact]
    public void Jaccard_IsTokenOrderInsensitive()
    {
        Assert.Equal(1.0, TextSimilarity.Jaccard("alpha beta", "beta alpha"));
    }

    [Fact]
    public void Jaccard_IgnoresStopwordsAndSingleCharTokens()
    {
        // "de" is a stopword and "x" is a single char; both drop out, leaving identical token sets.
        Assert.Equal(1.0, TextSimilarity.Jaccard("pagamento de salario x", "salario pagamento"));
    }

    [Fact]
    public void Jaccard_BothEmpty_ReturnsOne()
    {
        Assert.Equal(1.0, TextSimilarity.Jaccard("", ""));
        Assert.Equal(1.0, TextSimilarity.Jaccard(null, null));
    }

    [Fact]
    public void Jaccard_OneEmpty_ReturnsZero()
    {
        Assert.Equal(0.0, TextSimilarity.Jaccard("salario", ""));
    }
}
