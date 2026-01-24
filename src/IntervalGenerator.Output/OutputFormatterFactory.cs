namespace IntervalGenerator.Output;

/// <summary>
/// Factory for creating output formatters by format name.
/// </summary>
public static class OutputFormatterFactory
{
    private static readonly Dictionary<string, Func<IOutputFormatter>> Formatters = new(StringComparer.OrdinalIgnoreCase)
    {
        { "csv", () => new CsvOutputFormatter() },
        { "json", () => new ElectralinkJsonFormatter() }
    };

    /// <summary>
    /// Creates an output formatter for the specified format.
    /// </summary>
    /// <param name="format">The format name (csv, json).</param>
    /// <returns>The output formatter.</returns>
    /// <exception cref="ArgumentException">Thrown if format is not supported.</exception>
    public static IOutputFormatter Create(string format)
    {
        if (string.IsNullOrWhiteSpace(format))
        {
            throw new ArgumentException("Format cannot be null or empty.", nameof(format));
        }

        if (Formatters.TryGetValue(format, out var factory))
        {
            return factory();
        }

        throw new ArgumentException(
            $"Unknown format '{format}'. Supported formats: {string.Join(", ", Formatters.Keys)}",
            nameof(format));
    }

    /// <summary>
    /// Gets all supported format names.
    /// </summary>
    /// <returns>Collection of supported format names.</returns>
    public static IReadOnlyCollection<string> GetSupportedFormats()
    {
        return Formatters.Keys;
    }

    /// <summary>
    /// Checks if a format is supported.
    /// </summary>
    /// <param name="format">The format name to check.</param>
    /// <returns>True if supported; false otherwise.</returns>
    public static bool IsSupported(string format)
    {
        return !string.IsNullOrWhiteSpace(format) && Formatters.ContainsKey(format);
    }

    /// <summary>
    /// Gets the appropriate file extension for a format.
    /// </summary>
    /// <param name="format">The format name.</param>
    /// <returns>The file extension including the dot.</returns>
    public static string GetFileExtension(string format)
    {
        return Create(format).FileExtension;
    }
}
