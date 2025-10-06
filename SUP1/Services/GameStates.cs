namespace SUP.Services;

// TODO: Utöka med Rounds/annan state som delas mellan vyer
public sealed class GameState
// Den här första versionen innehåller bara namn
{
    public const string DefaultX = "Spelare X";
    public const string DefaultO = "Spelare O";

    public string PlayerXName { get; set; } = DefaultX;
    public string PlayerOName { get; set; } = DefaultO;

    public void ResetNamesToDefaults()
    {
        PlayerXName = DefaultX;
        PlayerOName = DefaultO;
    }
}

