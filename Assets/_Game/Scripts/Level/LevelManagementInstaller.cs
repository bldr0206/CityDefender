using UnityEngine;
using Zenject;
public class LevelManagementInstaller : MonoInstaller
{
    [SerializeField] private LevelSceneLogic levelSceneLogic;
    [SerializeField] private LevelSceneUIController levelSceneUIController;
    [SerializeField] private LevelValuesManager levelValuesManager;
    [SerializeField] private QuestPanel questPanel;
    [SerializeField] private DialogueScreen dialogueScreen;

    public override void InstallBindings()
    {
        Container.Bind<LevelSceneLogic>().FromInstance(levelSceneLogic).AsSingle();
        Container.Bind<LevelSceneUIController>().FromInstance(levelSceneUIController).AsSingle();
        Container.Bind<LevelValuesManager>().FromInstance(levelValuesManager).AsSingle();
        Container.Bind<QuestPanel>().FromInstance(questPanel).AsSingle();
        Container.Bind<DialogueScreen>().FromInstance(dialogueScreen).AsSingle();
        Container.Bind<PlayerController>().FromComponentInHierarchy().AsSingle();
        Container.Bind<PlayerCollector>().FromComponentInHierarchy().AsSingle();
        Container.Bind<BreakableTrigger>().FromComponentInHierarchy().AsSingle();
        Container.Bind<SaveService>().AsSingle();
        Container.BindInterfacesAndSelfTo<LevelSaveController>().AsSingle();
    }
}