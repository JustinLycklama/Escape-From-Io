using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AdvancedMiner : Unit {
    public override int duration => 480;
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
                return 1.2f;
            case MasterGameTask.ActionType.Build:
                return 0.3f;
            case MasterGameTask.ActionType.Move:
                return 1f;
        }

        return 0.1f;
    }
}
