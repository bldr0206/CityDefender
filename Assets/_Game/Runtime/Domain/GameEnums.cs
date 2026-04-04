using UnityEngine;

namespace ColorChargeTD.Domain
{
    public enum ColorCharge
    {
        Red = 0,
        Blue = 1,
    }

    public enum EnemyMovePattern
    {
        SingleLane = 0,
        Loop = 1,
        SharedCoverage = 2,
        Chokepoint = 3,
    }

    public enum UpgradeEffectType
    {
        StartingResourceBonus = 0,
        StartingChargeBonus = 1,
        BetweenWaveRecoveryBonus = 2,
        ExtraHudInfoUnlock = 3,
    }

    public enum LevelCompletionState
    {
        Locked = 0,
        Unlocked = 1,
        Completed = 2,
    }

    public enum BattleOutcome
    {
        None = 0,
        Victory = 1,
        Defeat = 2,
    }

    public enum GameFlowState
    {
        Boot = 0,
        Menu = 1,
        Meta = 2,
        BattleLoading = 3,
        Battle = 4,
        Result = 5,
    }

    public enum ValidationSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
    }

    public static class ColorChargeExtensions
    {
        public static Color ToUnityColor(this ColorCharge colorCharge)
        {
            switch (colorCharge)
            {
                case ColorCharge.Blue:
                    return new Color(0.25f, 0.55f, 1f, 1f);

                case ColorCharge.Red:
                default:
                    return new Color(1f, 0.3f, 0.3f, 1f);
            }
        }
    }
}
