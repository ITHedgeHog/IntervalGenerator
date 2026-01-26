using FluentAssertions;
using IntervalGenerator.Core.Utilities;

namespace IntervalGenerator.Core.Tests.Utilities;

public class MpanGeneratorTests
{
    #region GenerateMpan Tests

    [Fact]
    public void GenerateMpan_ValidGuid_Returns13DigitString()
    {
        var guid = Guid.NewGuid();

        var mpan = MpanGenerator.GenerateMpan(guid);

        mpan.Should().HaveLength(13);
        mpan.Should().MatchRegex(@"^\d{13}$");
    }

    [Fact]
    public void GenerateMpan_SameGuid_ReturnsSameMpan()
    {
        var guid = Guid.Parse("12345678-1234-1234-1234-123456789abc");

        var mpan1 = MpanGenerator.GenerateMpan(guid);
        var mpan2 = MpanGenerator.GenerateMpan(guid);

        mpan1.Should().Be(mpan2);
    }

    [Fact]
    public void GenerateMpan_DifferentGuids_ReturnDifferentMpans()
    {
        var guid1 = Guid.NewGuid();
        var guid2 = Guid.NewGuid();

        var mpan1 = MpanGenerator.GenerateMpan(guid1);
        var mpan2 = MpanGenerator.GenerateMpan(guid2);

        mpan1.Should().NotBe(mpan2);
    }

    [Fact]
    public void GenerateMpan_EmptyGuid_ReturnsValidMpan()
    {
        var emptyGuid = Guid.Empty;

        var mpan = MpanGenerator.GenerateMpan(emptyGuid);

        mpan.Should().HaveLength(13);
        mpan.Should().MatchRegex(@"^\d{13}$");
    }

    #endregion

    #region GenerateMpans Tests

    [Fact]
    public void GenerateMpans_MultipleGuids_ReturnsUniqueMpans()
    {
        var guids = Enumerable.Range(0, 100)
            .Select(_ => Guid.NewGuid())
            .ToList();

        var mpanMap = MpanGenerator.GenerateMpans(guids);

        mpanMap.Should().HaveCount(100);
        mpanMap.Values.Distinct().Should().HaveCount(100, "all MPANs should be unique");
    }

    [Fact]
    public void GenerateMpans_SameGuidsInDifferentOrder_ReturnsSameMpans()
    {
        var guids = new List<Guid>
        {
            Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
            Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc")
        };

        var mpanMap1 = MpanGenerator.GenerateMpans(guids);
        var mpanMap2 = MpanGenerator.GenerateMpans(guids.AsEnumerable().Reverse());

        foreach (var guid in guids)
        {
            mpanMap1[guid].Should().Be(mpanMap2[guid]);
        }
    }

    [Fact]
    public void GenerateMpans_EmptyList_ReturnsEmptyDictionary()
    {
        var emptyList = new List<Guid>();

        var mpanMap = MpanGenerator.GenerateMpans(emptyList);

        mpanMap.Should().BeEmpty();
    }

    [Fact]
    public void GenerateMpans_AllMpansAreValid()
    {
        var guids = Enumerable.Range(0, 50)
            .Select(_ => Guid.NewGuid())
            .ToList();

        var mpanMap = MpanGenerator.GenerateMpans(guids);

        foreach (var mpan in mpanMap.Values)
        {
            MpanGenerator.IsValidMpan(mpan).Should().BeTrue();
        }
    }

    #endregion

    #region IsValidMpan Tests

    [Theory]
    [InlineData("1234567890123", true)]
    [InlineData("0000000000000", true)]
    [InlineData("9999999999999", true)]
    [InlineData("123456789012", false)]   // 12 digits
    [InlineData("12345678901234", false)] // 14 digits
    [InlineData("123456789012A", false)]  // Contains letter
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("   ", false)]
    [InlineData("123-456-7890", false)]   // Contains hyphens
    public void IsValidMpan_ReturnsExpectedResult(string? mpan, bool expected)
    {
        var result = MpanGenerator.IsValidMpan(mpan!);

        result.Should().Be(expected);
    }

    #endregion
}
