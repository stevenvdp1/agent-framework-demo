using System.ComponentModel;

namespace DevUI;

public class MedievalWeatherTool
{
    [Description("Predicts the weather for a questing region using ancient folklore and nature's omens.")]
    public string PredictWeather([Description("The region of the kingdom (e.g., The North, The Iron Hills)")] string region)
    {
        string[] omens = {
            "The swallows fly low today; a heavy tempest is surely brewing.",
            "The sheep's wool is damp before the dew; the heavens shall weep by noon.",
            "A red sky at morning; the shepherd's warning is clear—danger in the air.",
            "The cows lie down in the meadow; 'tis a sign of rain coming from the West.",
            "The smoke from the hearth rises straight and tall; the gods grant us clear skies for travel."
        };

        var random = new Random();
        string omen = omens[random.Next(omens.Length)];

        return $"[OMEN FOR {region.ToUpper()}]: {omen}";
    }
}
