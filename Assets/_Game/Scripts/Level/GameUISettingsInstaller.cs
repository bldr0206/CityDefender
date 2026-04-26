using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "GameUISettingsInstaller", menuName = "Scriptable Objects/GameUISettingsInstaller")]
public class GameUISettingsInstaller : ScriptableObjectInstaller<GameUISettingsInstaller>
{
    [SerializeField] private GameUISettings gameUISettings;

    public override void InstallBindings()
    {
        Container.BindInstance(gameUISettings).AsSingle();
    }
}
