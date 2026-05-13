using System.ComponentModel;

namespace DevUI;

public class KnightTools
{
    [Description("Generates a unique URL for a knight's coat of arms shield.")]
    public string GenerateHeraldry([Description("The knight's name")] string name)
    {
        string seed = name.Replace(" ", "_").ToLower();
        return $"https://armoria.herokuapp.com/?size=500&format=svg&seed={seed}";
    }

    [Description("Rolls the sacred dice to determine a knight's physical and mental attributes.")]
    public KnightStats GetKnightStats([Description("The knight's name")] string name)
    {
        var rnd = new Random();
        return new KnightStats
        {
            Bravery = rnd.Next(15, 20),
            Chivalry = rnd.Next(10, 20),
            Health = 100,
            Title = rnd.Next(0, 2) == 0 ? "The Valiant" : "The Stout"
        };
    }
}

public record KnightStats
{
    public int Bravery { get; init; }
    public int Chivalry { get; init; }
    public int Health { get; init; }
    public string Title { get; init; } = "";
}
