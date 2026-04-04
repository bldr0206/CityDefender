using Zenject;

namespace ColorChargeTD.Installers
{
    public sealed class BootSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Boot only needs the shared project services.
        }
    }
}
