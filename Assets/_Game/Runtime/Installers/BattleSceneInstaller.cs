using Zenject;

namespace ColorChargeTD.Installers
{
    public sealed class BattleSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Battle runtime resolves only shared services and its own scene components.
        }
    }
}
