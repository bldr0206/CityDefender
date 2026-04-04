using ColorChargeTD.Presentation;
using Zenject;

namespace ColorChargeTD.Installers
{
    public sealed class MetaSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<NavigationCommandRouter>().FromComponentInHierarchy().AsSingle();
        }
    }
}
