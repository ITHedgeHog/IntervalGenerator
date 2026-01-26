using IntervalGenerator.Core.Profiles;

namespace IntervalGenerator.Profiles;

/// <summary>
/// Registry for managing consumption profiles by business type.
/// </summary>
public class ProfileRegistry
{
    private readonly Dictionary<string, IConsumptionProfile> _profiles;

    public ProfileRegistry()
    {
        _profiles = new Dictionary<string, IConsumptionProfile>(StringComparer.OrdinalIgnoreCase)
        {
            { "Office", new OfficeProfile() },
            { "Manufacturing", new ManufacturingProfile() },
            { "Retail", new RetailProfile() },
            { "DataCenter", new DataCenterProfile() },
            { "Educational", new EducationalInstitutionProfile() }
        };
    }

    /// <summary>
    /// Gets a consumption profile by business type name.
    /// </summary>
    /// <param name="businessType">The business type (case-insensitive).</param>
    /// <returns>The consumption profile.</returns>
    /// <exception cref="ArgumentException">Thrown if business type is not found.</exception>
    public IConsumptionProfile GetProfile(string businessType)
    {
        if (string.IsNullOrWhiteSpace(businessType))
            throw new ArgumentException("Business type cannot be null or empty.", nameof(businessType));

        if (_profiles.TryGetValue(businessType, out var profile))
            return profile;

        throw new ArgumentException(
            $"Unknown business type '{businessType}'. Available types: {string.Join(", ", _profiles.Keys)}",
            nameof(businessType));
    }

    /// <summary>
    /// Gets all available business types.
    /// </summary>
    /// <returns>Collection of available business type names.</returns>
    public IReadOnlyCollection<string> GetAvailableBusinessTypes()
    {
        return _profiles.Keys;
    }

    /// <summary>
    /// Checks if a business type is registered.
    /// </summary>
    /// <param name="businessType">The business type to check.</param>
    /// <returns>True if registered; false otherwise.</returns>
    public bool IsRegistered(string businessType)
    {
        return !string.IsNullOrWhiteSpace(businessType) && _profiles.ContainsKey(businessType);
    }

    /// <summary>
    /// Registers a custom profile (replaces existing if present).
    /// </summary>
    /// <param name="profile">The profile to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if profile is null.</exception>
    public void RegisterProfile(IConsumptionProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);

        _profiles[profile.BusinessType] = profile;
    }
}
