using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mover : Unit {
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Move;
}
