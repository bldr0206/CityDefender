using UnityEngine;
using Zenject;
public class BattleInputInstaller : MonoInstaller
{
    [SerializeField] private Joystick moveJoystick;
    public override void InstallBindings()
    {
        Container.Bind<Joystick>().FromInstance(moveJoystick).AsSingle();
    }
}