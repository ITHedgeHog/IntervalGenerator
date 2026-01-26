namespace IntervalGenerator.Core.Utilities;

/// <summary>
/// Utility class for MPAN (Meter Point Administration Number) generation and conversion.
/// </summary>
public static class MpanGenerator
{
    /// <summary>
    /// Generates an MPAN from a GUID.
    /// Uses the first 13 hex digits of the GUID to create a 13-digit numeric MPAN.
    /// </summary>
    /// <param name="guidMeterId">The GUID meter ID.</param>
    /// <returns>A 13-digit MPAN as a string.</returns>
    public static string GenerateMpan(Guid guidMeterId)
    {
        // Get hex string without hyphens
        string hexString = guidMeterId.ToString("N");

        // Take first 13 characters and convert to numeric string
        string mpanString = hexString.Substring(0, 13);

        // Convert hex to decimal representation
        // Using modulo to ensure we get a 13-digit number
        ulong hexValue = ulong.Parse(mpanString, System.Globalization.NumberStyles.HexNumber);

        // Convert to 13-digit decimal string (pad with zeros if needed)
        string mpan = (hexValue % 10000000000000).ToString("D13");

        return mpan;
    }

    /// <summary>
    /// Generates multiple unique MPANs from a list of GUIDs.
    /// </summary>
    /// <param name="meterIds">The list of GUID meter IDs.</param>
    /// <returns>A dictionary mapping GUIDs to their corresponding MPANs.</returns>
    public static Dictionary<Guid, string> GenerateMpans(IEnumerable<Guid> meterIds)
    {
        var result = new Dictionary<Guid, string>();
        var usedMpans = new HashSet<string>();

        foreach (var meterId in meterIds)
        {
            string mpan = GenerateMpan(meterId);

            // Ensure uniqueness by appending a counter if needed
            int counter = 0;
            string uniqueMpan = mpan;
            while (usedMpans.Contains(uniqueMpan))
            {
                counter++;
                // Shift digits and append counter
                uniqueMpan = (ulong.Parse(mpan) + (ulong)counter).ToString("D13");
            }

            result[meterId] = uniqueMpan;
            usedMpans.Add(uniqueMpan);
        }

        return result;
    }

    /// <summary>
    /// Validates that a string is a valid MPAN format (13 digits).
    /// </summary>
    /// <param name="mpan">The MPAN string to validate.</param>
    /// <returns>True if valid; false otherwise.</returns>
    public static bool IsValidMpan(string mpan)
    {
        if (string.IsNullOrWhiteSpace(mpan))
            return false;

        if (mpan.Length != 13)
            return false;

        return mpan.All(char.IsDigit);
    }
}
