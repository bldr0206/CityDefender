using Unity.VisualScripting;
using UnityEngine;
using Zenject;
public class LevelManagementInstaller : MonoInstaller
{
    [SerializeField] private LevelSceneLogic levelSceneLogic;
    [SerializeField] private LevelSceneUIController levelSceneUIController;
    [SerializeField] private LevelValuesManager levelValuesManager;

    public override void InstallBindings()
    {
        Container.Bind<LevelSceneLogic>().FromInstance(levelSceneLogic).AsSingle();
        Container.Bind<LevelSceneUIController>().FromInstance(levelSceneUIController).AsSingle();
        Container.Bind<LevelValuesManager>().FromInstance(levelValuesManager).AsSingle();
    }
}