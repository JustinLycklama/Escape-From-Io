using System.Collections.Generic;
using NUnit.Framework;

// Pure task-model logic. MasterGameTasks here have no child GameTasks: the GameTask constructor converts its
// target through scene objects (Constants/MapsManager), so anything needing GameTasks belongs in a play-mode check.
public class MasterGameTaskTests {

    class RecordingDelegate : MasterTaskUpdateDelegate {
        public List<string> events = new List<string>();
        public void RepeatCountUpdated(MasterGameTask t, int count) { events.Add($"repeat:{count}"); }
        public void TaskCancelled(MasterGameTask t) { events.Add("cancelled"); }
        public void TaskFinished(MasterGameTask t) { events.Add("finished"); }
        public void TaskBlockerRemoved(MasterGameTask t) { events.Add("unblocked"); }
        public void TaskUnitAssigned(MasterGameTask t) { events.Add("assigned"); }
    }

    static MasterGameTask NewTask(MasterGameTask.ActionType type = MasterGameTask.ActionType.Build, List<MasterGameTask> blockers = null) {
        return new MasterGameTask(type, "test", new GameTask[0], blockers);
    }

    [Test]
    public void TaskNumbersAreUniqueAndIncreasing() {
        var a = NewTask();
        var b = NewTask();
        Assert.Greater(b.taskNumber, a.taskNumber);
    }

    [Test]
    public void BlockedTaskCannotStartUntilBlockerFinishes() {
        var blocker = NewTask(MasterGameTask.ActionType.Move);
        var blocked = NewTask(MasterGameTask.ActionType.Build, new List<MasterGameTask> { blocker });
        var recorder = new RecordingDelegate();
        blocked.RegisterForTaskStatusNotifications(recorder);

        Assert.AreSame(blocked, blocker.taskBlockedByThis);
        Assert.IsFalse(blocked.SatisfiesStartRequirements());
        Assert.IsTrue(blocked.CancellableByUI, "the blocked (top-level) task itself is cancellable");
        Assert.IsFalse(blocker.CancellableByUI, "a blocker task is not cancellable from the UI");

        blocker.MarkTaskFinished();

        Assert.IsTrue(blocked.SatisfiesStartRequirements());
        Assert.IsNull(blocker.taskBlockedByThis);
        CollectionAssert.Contains(recorder.events, "unblocked");
    }

    [Test]
    public void TaskWithMultipleBlockersNeedsAllFinished() {
        var b1 = NewTask(MasterGameTask.ActionType.Move);
        var b2 = NewTask(MasterGameTask.ActionType.Move);
        var blocked = NewTask(MasterGameTask.ActionType.Build, new List<MasterGameTask> { b1, b2 });

        b1.MarkTaskFinished();
        Assert.IsFalse(blocked.SatisfiesStartRequirements());

        b2.MarkTaskFinished();
        Assert.IsTrue(blocked.SatisfiesStartRequirements());
    }

    [Test]
    public void RegisteringReportsCurrentRepeatCountImmediately() {
        var task = NewTask();
        task.repeatCount = 3;
        var recorder = new RecordingDelegate();

        task.RegisterForTaskStatusNotifications(recorder);

        CollectionAssert.AreEqual(new[] { "repeat:3" }, recorder.events);
    }

    [Test]
    public void CloneTaskConsumesRepeatCount() {
        var task = NewTask(MasterGameTask.ActionType.Move);
        task.repeatCount = 2;

        var c1 = task.CloneTask();
        var c2 = task.CloneTask();
        var c3 = task.CloneTask();

        Assert.IsNotNull(c1);
        Assert.IsNotNull(c2);
        Assert.IsNull(c3, "no clone once repeatCount is exhausted");
        Assert.AreEqual(0, task.repeatCount);
        Assert.AreEqual(task.actionType, c1.actionType);
        Assert.AreNotEqual(c1.taskNumber, c2.taskNumber);
    }

    [Test]
    public void ParentFinishesOnlyAfterAllClonesFinish() {
        // e.g. "Collect 2 Copper": two clones go out; the build it blocks may start only when both are delivered
        var collect = NewTask(MasterGameTask.ActionType.Move);
        collect.repeatCount = 2;
        var build = NewTask(MasterGameTask.ActionType.Build, new List<MasterGameTask> { collect });
        var recorder = new RecordingDelegate();
        collect.RegisterForTaskStatusNotifications(recorder);

        var c1 = collect.CloneTask();
        var c2 = collect.CloneTask();

        c1.MarkTaskFinished();
        CollectionAssert.DoesNotContain(recorder.events, "finished");
        Assert.IsFalse(build.SatisfiesStartRequirements());

        c2.MarkTaskFinished();
        CollectionAssert.Contains(recorder.events, "finished");
        Assert.IsTrue(build.SatisfiesStartRequirements());
    }

    [Test]
    public void SetAssignedUnitNotifiesDelegates() {
        var task = NewTask();
        var recorder = new RecordingDelegate();
        task.RegisterForTaskStatusNotifications(recorder);

        task.SetAssignedUnit(null);

        CollectionAssert.Contains(recorder.events, "assigned");
        Assert.IsNull(task.assignedUnit);
    }

    [Test]
    public void EndingNotificationsStopsEvents() {
        var task = NewTask();
        var recorder = new RecordingDelegate();
        task.RegisterForTaskStatusNotifications(recorder);
        task.EndTaskStatusNotifications(recorder);
        recorder.events.Clear();

        task.MarkTaskFinished();

        CollectionAssert.IsEmpty(recorder.events);
    }
}
