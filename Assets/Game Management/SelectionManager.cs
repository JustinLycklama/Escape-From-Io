using System.Collections.Generic;
using UnityEngine;

//public interface StatusDelegate {
//    void InformCurrentTask(MasterGameTask task, GameTask gameTask);
//}

public interface DeletionWatch {
    void ObjectDeleted(Selectable selectable);
}

public interface Selectable : TaskStatusNotifiable, UserActionNotifiable {
    string description { get; }

    void SetSelected(bool selected);
    //void SetStatusDelegate(StatusDelegate statusDelegate);
    //UserAction[] UserAction

    void SubscribeToDeletionWatch(DeletionWatch watcher);
    void EndDeletionWatch(DeletionWatch watcher);
}

public interface SelectionManagerDelegate {
    void NotifyUpdateSelection(Selection selection);
}

public class SelectionManager : MonoBehaviour, DeletionWatch {

    Selection currentSelection;
    List<SelectionManagerDelegate> delegateList = new List<SelectionManagerDelegate>();

    private void UpdateSelection(Selection newSelection) {
        RemoveSelection();
        Script.Get<UIManager>().PopToRoot();

        currentSelection = newSelection;
        
        foreach(SelectionManagerDelegate notificationDelegate in delegateList) {
            notificationDelegate.NotifyUpdateSelection(currentSelection);
        }
    } 

    public Selection SelectTerrain(LayoutCoordinate coordinate) {       
        Selection selection = new Selection();
        selection.setTerrain(coordinate);

        UpdateSelection(selection);
        return selection;
    }

    public Selection SelectSelectable(Selectable selectable) {
        Selection selection = new Selection();
        selection.selectionType = Selection.SelectionType.Selectable;
        selection.selection = selectable;

        UpdateSelection(selection);
        selectable.SetSelected(true);

        selectable.SubscribeToDeletionWatch(this);

        return selection;
    }

    public void RemoveSelection() {
        if (currentSelection == null) {
            return;
        }

        currentSelection.Deselect(this);      
    }

    public void RegisterForNotifications(SelectionManagerDelegate notificationDelegate) {
        delegateList.Add(notificationDelegate);
    }

    public void EndNotifications(SelectionManagerDelegate notificationDelegate) {
        delegateList.Remove(notificationDelegate);
    }

    /*
     * DeletionWatch Interface
     * */

    public void ObjectDeleted(Selectable selectable) {
        if (currentSelection != null && currentSelection.Is(selectable)) {
            currentSelection = null;
            UpdateSelection(currentSelection);
        }
    }
}

public class Selection {

    public enum SelectionType { Terrain, Selectable }
    public SelectionType selectionType;

    // Terrain Properties
    public LayoutCoordinate coordinate;

    public Selectable selection;   

    // Accessors

    public void setTerrain(LayoutCoordinate coordinate) {
        selectionType = SelectionType.Terrain;
        this.coordinate = coordinate;
    }

    public bool Is(Selectable selectable) {
        return selectionType == SelectionType.Selectable && selection == selectable;
    }

    // Mutators

    public void Deselect(DeletionWatch watcher) {
        if(selectionType == SelectionType.Terrain) {
        } else if(selectionType == SelectionType.Selectable) {
            selection?.SetSelected(false);
            selection.EndDeletionWatch(watcher);
        }
    }

    // Properties
    public string Title() {
        switch(selectionType) {
            case SelectionType.Terrain:
                return coordinate.mapContainer.map.GetTerrainAt(coordinate).name;
            case SelectionType.Selectable:
                return selection.description;
            default:
                return "";
        }
    }

    /*
    * TaskStatusNotifiable Passthrough to selection
    * */

    public void SubscribeToTaskStatus(TaskStatusUpdateDelegate taskStatusUpdateDelegate) {
        switch(selectionType) {
            case SelectionType.Terrain:
                coordinate.mapContainer.map.RegisterForTaskStatusNotifications(taskStatusUpdateDelegate, coordinate);
                break;
            case SelectionType.Selectable:
                selection.RegisterForTaskStatusNotifications(taskStatusUpdateDelegate);
                break;
        }
    }

    public void EndSubscriptionToTaskStatus(TaskStatusUpdateDelegate taskStatusUpdateDelegate) {
        switch(selectionType) {
            case SelectionType.Terrain:
                coordinate.mapContainer.map.EndTaskStatusNotifications(taskStatusUpdateDelegate, coordinate);
                break;
            case SelectionType.Selectable:
                selection.EndTaskStatusNotifications(taskStatusUpdateDelegate);
                break;
        }
    }
      
    /*
    * UserActionNotifiable Passthrough to selection
    * */

    public void SubscribeToUserActions(UserActionUpdateDelegate userActionDelegate) {
        switch(selectionType) {
            case SelectionType.Terrain:
                coordinate.mapContainer.map.RegisterForUserActionNotifications(userActionDelegate, coordinate);
                //return Script.Get<MapsManager>().ActionsAvailableAt(coordinate);
                break;
            case SelectionType.Selectable:
                selection.RegisterForUserActionNotifications(userActionDelegate);
                break;
        }
    }

    public void EndSubscriptionToUserActions(UserActionUpdateDelegate userActionDelegate) {
        switch(selectionType) {
            case SelectionType.Terrain:
                coordinate.mapContainer.map.EndUserActionNotifications(userActionDelegate, coordinate);
                break;
            case SelectionType.Selectable:
                selection.EndUserActionNotifications(userActionDelegate);
                break;

        }
    }


}