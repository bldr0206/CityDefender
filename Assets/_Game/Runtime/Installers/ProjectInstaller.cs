using ColorChargeTD.Core;
using ColorChargeTD.Data;
using ColorChargeTD.Product;
using UnityEngine;
using Zenject;

namespace ColorChargeTD.Installers
{
    public sealed class ProjectInstaller : MonoInstaller
    {
        [SerializeField] private GameContentConfig gameContentConfig;

        public override void InstallBindings()
        {
            if (gameContentConfig == null)
            {
                gameContentConfig = Resources.Load<GameContentConfig>("GameContentConfig");
            }

            Container.BindInstance(gameContentConfig).IfNotBound();
            Container.Bind<IGameContentService>().To<GameContentService>().AsSingle();
            Container.Bind<ISaveService>().To<PlayerPrefsJsonSaveService>().AsSingle();
            Container.Bind<IPlayerProfileService>().To<PlayerProfileService>().AsSingle();
            Container.Bind<ISceneLoader>().To<UnitySceneLoader>().AsSingle();
            Container.Bind<IGameStateMachine>().To<GameStateMachine>().AsSingle();
            Container.Bind<ILevelSelectionService>().To<LevelSelectionService>().AsSingle();

            NavigationSceneCoroutineHost sceneCoroutineHost = GetComponent<NavigationSceneCoroutineHost>();
            if (sceneCoroutineHost == null)
            {
                sceneCoroutineHost = gameObject.AddComponent<NavigationSceneCoroutineHost>();
            }

            Container.BindInstance(sceneCoroutineHost).AsSingle();
            Container.Bind<IProgressionService>().To<ProgressionService>().AsSingle();
            Container.Bind<IGameNavigationService>().To<GameNavigationService>().AsSingle();
        }
    }
}
