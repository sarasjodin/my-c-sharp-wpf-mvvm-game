namespace SUP;

// TODO: Kolla upp, bör den ligga i en "mappa Core"?
public static class TextHelper

{
    /// <summary>
    /// Normaliserar en textsträng genom att trimma och returnerar valfritt fallback 
    /// Ex. TextHelper.NormalizeText("  Sara  ", "Spelare X"); // -> "Sara"
    /// Ex. TextHelper.NormalizeText(null, "Spelare X");       // -> "Spelare X"
    /// Ex. TextHelper.NormalizeText(" ");                   // -> null
    /// </summary>
    public static string? NormalizeText(string? value, string? fallback = null)
    {
        if (string.IsNullOrWhiteSpace(value))
            return fallback;
        return value.Trim();
    }
}

