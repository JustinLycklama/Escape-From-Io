using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : Unit {
    public override int duration => 12;
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Mine;

    public MoenenGames.VoxelRobot.Weapon[] weaponSet; 

    protected override void Animate() {
        foreach(MoenenGames.VoxelRobot.Weapon weapon in weaponSet) {
            weapon.Animate();
        }
    }

    public override float SpeedForTask(MasterGameTask.ActionType actionType) {
        switch(actionType) {
            case MasterGameTask.ActionType.Mine:
                return 0.75f;
            case MasterGameTask.ActionType.Build:
                return 0.3f;
            case MasterGameTask.ActionType.Move:
                return 0.3f;
        }

        return 0.1f;
    }
}
