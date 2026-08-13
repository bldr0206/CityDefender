namespace CityDef.Gameplay.Logic
{
    /// <summary>
    /// Характеристики бота. Имена уходят в сейвы строками (BotStats.CaptureSaveData) —
    /// переименование ломает старые сейвы, добавление новых значений безопасно.
    /// </summary>
    public enum BotStatType
    {
        MaxHealth,
        Mining,
        AttackSpeed,
        MiningSpeed,
        Energy,
        HealthRegen,
    }
}
