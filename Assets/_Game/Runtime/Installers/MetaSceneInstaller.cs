using Zenject;

namespace ColorChargeTD.Installers
{
    public sealed class MetaSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            // Meta scene consumes the same project services as menu and result flows.
        }
    }
}
