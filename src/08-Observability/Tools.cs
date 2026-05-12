using System.ComponentModel;

namespace Observability;

public class QuestTools
{
    [Description("Looks up the active quest available in a given region of the realm.")]
    public QuestInfo GetQuestDetails([Description("The region to search for quests")] string region)
    {
        var quests = new Dictionary<string, QuestInfo>(StringComparer.OrdinalIgnoreCase)
        {
            ["forest"] = new("The Whispering Woods", "Retrieve the Enchanted Acorn from the Forest Guardian", "High"),
            ["mountains"] = new("Peaks of Peril", "Slay the frost wyrm nesting in the northern caves", "Extreme"),
            ["swamp"] = new("The Murky Depths", "Recover the lost crown from the Bog King's lair", "Medium"),
            ["desert"] = new("Sands of Sorrow", "Find the buried temple and claim the Sun Scepter", "High"),
        };

        return quests.TryGetValue(region, out var quest)
            ? quest
            : new("Unknown Territory", $"Scout the mysterious {region} and report back", "Unknown");
    }
}

public class SupplyTools
{
    [Description("Checks whether a specific item is available in the quartermaster's inventory.")]
    public InventoryResult CheckInventory([Description("The item to look for")] string item)
    {
        var stock = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
        {
            ["sword"] = 12,
            ["shield"] = 7,
            ["health potion"] = 25,
            ["rope"] = 40,
            ["torch"] = 18,
            ["rations"] = 100,
            ["map"] = 3,
            ["antidote"] = 5,
        };

        var available = stock.TryGetValue(item, out var qty) && qty > 0;
        return new InventoryResult(item, available, available ? qty : 0);
    }
}

public record QuestInfo(string Name, string Objective, string DangerLevel);

public record InventoryResult(string Item, bool Available, int Quantity);
