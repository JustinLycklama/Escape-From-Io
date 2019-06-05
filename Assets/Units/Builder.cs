using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Builder : Unit {
    public override MasterGameTask.ActionType primaryActionType => MasterGameTask.ActionType.Build;
}
