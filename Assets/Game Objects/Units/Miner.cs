using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : Unit {
    public override int duration => 90;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Mine;

    public MoenenGames.VoxelRobot.Weapon[] weaponSet; 

    protected override void Animate() {
        foreach(MoenenGames.VoxelRobot.Weapon weapon in weaponSet) {
            weapon.Animate();
        }
    }

    public override float SpeedForTask(GameTask gameTask) {
        switch(gameTask.action) {
            case GameTask.ActionType.Build:
                break;
            case GameTask.ActionType.Mine:
                return 0.3f;
            case GameTask.ActionType.PickUp:
                break;
            case GameTask.ActionType.DropOff:
                break;
        }

        return 0.5f;
    }
}
