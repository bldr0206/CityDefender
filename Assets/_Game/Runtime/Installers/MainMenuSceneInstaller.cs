using ColorChargeTD.Presentation;
using Zenject;

namespace ColorChargeTD.Installers
{
    public sealed class MainMenuSceneInstaller : MonoInstaller
    {
        public override void InstallBindings()
        {
            Container.Bind<NavigationCommandRouter>().FromComponentInHierarchy().AsSingle();
        }
    }
}
