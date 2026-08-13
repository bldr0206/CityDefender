using CityDef.Gameplay.Logic;
using UnityEngine.Localization;

/// <summary> Ключи локализации экрана «Боты» (таблица Localisation_main). </summary>
public static class BotsMenuLoc
{
    public const string Table = "Localisation_main";

    public const string Title = "ui.bots.title";
    public const string Empty = "ui.bots.empty";
    public const string Close = "ui.bots.close";
    public const string BotName = "ui.bots.bot_name";
    public const string LevelShort = "ui.bots.level_short";

    public static LocalizedString Make(string key)
    {
        return new LocalizedString(Table, key);
    }

    public static LocalizedString MakeStatName(BotStatType type)
    {
        switch (type)
        {
            case BotStatType.MaxHealth: return Make("ui.bots.stat.max_health");
            case BotStatType.Mining: return Make("ui.bots.stat.mining");
            case BotStatType.AttackSpeed: return Make("ui.bots.stat.attack_speed");
            case BotStatType.MiningSpeed: return Make("ui.bots.stat.mining_speed");
            case BotStatType.Energy: return Make("ui.bots.stat.energy");
            case BotStatType.HealthRegen: return Make("ui.bots.stat.health_regen");
            default: return Make("ui.bots.stat." + type.ToString().ToLowerInvariant());
        }
    }
}
