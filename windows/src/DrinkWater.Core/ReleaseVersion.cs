using System.Globalization;

namespace DrinkWater.Core;

public readonly record struct ReleaseVersion(int Major, int Minor, int Patch) : IComparable<ReleaseVersion>
{
    public static bool TryParse(string? text, out ReleaseVersion version)
    {
        version = default;
        string[] parts = text?.Split('.') ?? [];
        if (parts.Length != 3)
        {
            return false;
        }

        int[] values = new int[3];
        for (int index = 0; index < parts.Length; index++)
        {
            if (parts[index].Length == 0 || (parts[index].Length > 1 && parts[index][0] == '0')
                || !int.TryParse(parts[index], NumberStyles.None, CultureInfo.InvariantCulture, out values[index]))
            {
                return false;
            }
        }

        version = new(values[0], values[1], values[2]);
        return true;
    }

    public int CompareTo(ReleaseVersion other)
    {
        int major = Major.CompareTo(other.Major);
        int minor = Minor.CompareTo(other.Minor);
        return major != 0 ? major : minor != 0 ? minor : Patch.CompareTo(other.Patch);
    }

    public override string ToString() => string.Create(CultureInfo.InvariantCulture, $"{Major}.{Minor}.{Patch}");
}
