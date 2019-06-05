using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskQueueManager : MonoBehaviour
{
    List<MasterGameTask> taskList;
    UIManager uiManager;

    void Awake()
    {
        taskList = new List<MasterGameTask>();
        uiManager = Script.Get<UIManager>();
    }

    public int Count() {
        return taskList.Count;
    }

    public void QueueTask(MasterGameTask task) {
        taskList.Add(task);
        uiManager.UpdateTaskList(taskList.ToArray());
    }

    public MasterGameTask GetNextDoableTask(Unit unit) {

        float shortestDistance = float.MaxValue;
        MasterGameTask shortestTask = null;

        foreach(MasterGameTask masterTask in taskList) {
            if (masterTask.SatisfiesStartRequirements()) {

                if (masterTask.childTasks[0].pathRequestTargetType != PathRequestTargetType.Unknown) {
                    float distance = Vector3.Distance(masterTask.childTasks[0].target.vector3, unit.transform.position);

                    if (distance < shortestDistance) {
                        shortestDistance = distance;
                        shortestTask = masterTask;
                    }
                } else {
                    if (shortestTask == null) {
                        shortestTask = masterTask;
                    }

                    // If we have an unknown task, this is the end of our search. 
                    // Either we have a task above that is more important, and the shortest of those is the victor, or we use this unknown as the shortest task;
                    break; 
                }
            }
        }

        if (shortestTask != null) {
            taskList.Remove(shortestTask);
            uiManager.UpdateTaskList(taskList.ToArray());
        }       

        return shortestTask;       
    }
}
