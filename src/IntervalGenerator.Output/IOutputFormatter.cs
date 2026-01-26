using IntervalGenerator.Core.Models;

namespace IntervalGenerator.Output;

/// <summary>
/// Interface for output formatters that write interval readings to various formats.
/// </summary>
public interface IOutputFormatter
{
    /// <summary>
    /// Gets the format name (e.g., "csv", "json").
    /// </summary>
    string FormatName { get; }

    /// <summary>
    /// Gets the default file extension for this format.
    /// </summary>
    string FileExtension { get; }

    /// <summary>
    /// Writes readings to a stream.
    /// </summary>
    /// <param name="readings">The readings to write.</param>
    /// <param name="stream">The output stream.</param>
    /// <param name="options">Optional formatting options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteAsync(
        IEnumerable<IntervalReading> readings,
        Stream stream,
        OutputOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes readings to a file.
    /// </summary>
    /// <param name="readings">The readings to write.</param>
    /// <param name="filePath">The output file path.</param>
    /// <param name="options">Optional formatting options.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task WriteToFileAsync(
        IEnumerable<IntervalReading> readings,
        string filePath,
        OutputOptions? options = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Options for output formatting.
/// </summary>
public record OutputOptions
{
    /// <summary>
    /// Site name to include in output (used in JSON format).
    /// </summary>
    public string? SiteName { get; init; }

    /// <summary>
    /// Whether to include headers in CSV output.
    /// </summary>
    public bool IncludeHeaders { get; init; } = true;

    /// <summary>
    /// Whether to pretty-print JSON output.
    /// </summary>
    public bool PrettyPrint { get; init; }
}
