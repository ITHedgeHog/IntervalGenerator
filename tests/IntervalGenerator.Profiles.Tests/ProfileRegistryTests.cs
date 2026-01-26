using FluentAssertions;
using IntervalGenerator.Core.Profiles;
using NSubstitute;

namespace IntervalGenerator.Profiles.Tests;

public class ProfileRegistryTests
{
    private readonly ProfileRegistry _registry;

    public ProfileRegistryTests()
    {
        _registry = new ProfileRegistry();
    }

    #region GetProfile Tests

    [Theory]
    [InlineData("Office")]
    [InlineData("Manufacturing")]
    [InlineData("Retail")]
    [InlineData("DataCenter")]
    [InlineData("Educational")]
    public void GetProfile_ValidBusinessType_ReturnsProfile(string businessType)
    {
        var profile = _registry.GetProfile(businessType);

        profile.Should().NotBeNull();
        profile.BusinessType.Should().Be(businessType);
    }

    [Theory]
    [InlineData("office")]
    [InlineData("OFFICE")]
    [InlineData("Office")]
    [InlineData("oFfIcE")]
    public void GetProfile_CaseInsensitive_ReturnsProfile(string businessType)
    {
        var profile = _registry.GetProfile(businessType);

        profile.Should().NotBeNull();
        profile.BusinessType.Should().Be("Office");
    }

    [Fact]
    public void GetProfile_UnknownBusinessType_ThrowsArgumentException()
    {
        var act = () => _registry.GetProfile("Unknown");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Unknown business type*")
            .WithParameterName("businessType");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetProfile_NullOrEmptyBusinessType_ThrowsArgumentException(string? businessType)
    {
        var act = () => _registry.GetProfile(businessType!);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("businessType");
    }

    [Fact]
    public void GetProfile_ReturnsCorrectProfileTypes()
    {
        _registry.GetProfile("Office").Should().BeOfType<OfficeProfile>();
        _registry.GetProfile("Manufacturing").Should().BeOfType<ManufacturingProfile>();
        _registry.GetProfile("Retail").Should().BeOfType<RetailProfile>();
        _registry.GetProfile("DataCenter").Should().BeOfType<DataCenterProfile>();
        _registry.GetProfile("Educational").Should().BeOfType<EducationalInstitutionProfile>();
    }

    #endregion

    #region GetAvailableBusinessTypes Tests

    [Fact]
    public void GetAvailableBusinessTypes_ReturnsAllFiveTypes()
    {
        var types = _registry.GetAvailableBusinessTypes();

        types.Should().HaveCount(5);
        types.Should().Contain(new[] { "Office", "Manufacturing", "Retail", "DataCenter", "Educational" });
    }

    [Fact]
    public void GetAvailableBusinessTypes_ReturnsReadOnlyCollection()
    {
        var types = _registry.GetAvailableBusinessTypes();

        types.Should().BeAssignableTo<IReadOnlyCollection<string>>();
    }

    #endregion

    #region IsRegistered Tests

    [Theory]
    [InlineData("Office", true)]
    [InlineData("Manufacturing", true)]
    [InlineData("Retail", true)]
    [InlineData("DataCenter", true)]
    [InlineData("Educational", true)]
    [InlineData("Unknown", false)]
    [InlineData("Hospital", false)]
    public void IsRegistered_ReturnsExpectedResult(string businessType, bool expected)
    {
        var result = _registry.IsRegistered(businessType);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData("office")]
    [InlineData("MANUFACTURING")]
    [InlineData("ReTaIl")]
    public void IsRegistered_CaseInsensitive_ReturnsTrue(string businessType)
    {
        var result = _registry.IsRegistered(businessType);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void IsRegistered_NullOrEmpty_ReturnsFalse(string? businessType)
    {
        var result = _registry.IsRegistered(businessType!);

        result.Should().BeFalse();
    }

    #endregion

    #region RegisterProfile Tests

    [Fact]
    public void RegisterProfile_NewProfile_AddsToRegistry()
    {
        var customProfile = Substitute.For<IConsumptionProfile>();
        customProfile.BusinessType.Returns("Hospital");

        _registry.RegisterProfile(customProfile);

        _registry.IsRegistered("Hospital").Should().BeTrue();
        _registry.GetProfile("Hospital").Should().Be(customProfile);
    }

    [Fact]
    public void RegisterProfile_ExistingBusinessType_ReplacesProfile()
    {
        var customOffice = Substitute.For<IConsumptionProfile>();
        customOffice.BusinessType.Returns("Office");

        _registry.RegisterProfile(customOffice);

        _registry.GetProfile("Office").Should().Be(customOffice);
    }

    [Fact]
    public void RegisterProfile_NullProfile_ThrowsArgumentNullException()
    {
        var act = () => _registry.RegisterProfile(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("profile");
    }

    [Fact]
    public void RegisterProfile_UpdatesAvailableTypes()
    {
        var customProfile = Substitute.For<IConsumptionProfile>();
        customProfile.BusinessType.Returns("Warehouse");

        _registry.RegisterProfile(customProfile);

        _registry.GetAvailableBusinessTypes().Should().Contain("Warehouse");
    }

    #endregion

    #region Profile Independence Tests

    [Fact]
    public void GetProfile_MultipleCallsSameType_ReturnsSameInstance()
    {
        var profile1 = _registry.GetProfile("Office");
        var profile2 = _registry.GetProfile("Office");

        profile1.Should().BeSameAs(profile2);
    }

    [Fact]
    public void GetProfile_DifferentTypes_ReturnDifferentInstances()
    {
        var office = _registry.GetProfile("Office");
        var retail = _registry.GetProfile("Retail");

        office.Should().NotBeSameAs(retail);
    }

    #endregion
}
