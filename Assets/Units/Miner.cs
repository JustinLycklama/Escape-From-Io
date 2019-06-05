using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Miner : Unit {
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Mine;
}
