// Строковые теги Unity в одном месте, чтобы не плодить магические строки по геймплей-коду.
// Значения обязаны совпадать с тегами в ProjectSettings/TagManager.asset.

public static class GameTags
{
    // Встроенные теги Unity
    public const string Player = "Player";
    public const string Finish = "Finish";

    // Теги проекта
    public const string Collectable = "Collectable";
    public const string Interactable = "Interactable";
    public const string Breakable = "Breakable";
    public const string Door = "Door";
    public const string Contact = "Contact";
    public const string Lift = "Lift";
    public const string LiftTrigger = "LiftTrigger";
    public const string QuestDestinationMarker = "QuestDestinationMarker";
}
