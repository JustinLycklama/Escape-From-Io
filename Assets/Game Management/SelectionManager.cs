using System.Collections.Generic;
using UnityEngine;

//public interface StatusDelegate {
//    void InformCurrentTask(MasterGameTask task, GameTask gameTask);
//}

public interface Selectable : TaskStatusNotifiable, UserActionNotifiable {
    string description { get; }

    void SetSelected(bool selected);
    //void SetStatusDelegate(StatusDelegate statusDelegate);
    //UserAction[] UserAction

}

public interface SelectionManagerDelegate {
    void NotifyUpdateSelection(Selection selection);
}

public class SelectionManager : MonoBehaviour {

    Selection currentSelection;
    List<SelectionManagerDelegate> delegateList = new List<SelectionManagerDelegate>();

    private void UpdateSelection(Selection newSelection) {
        RemoveSelection();

        currentSelection = newSelection;
        
        foreach(SelectionManagerDelegate notificationDelegate in delegateList) {
            notificationDelegate.NotifyUpdateSelection(currentSelection);
        }
    } 

    public Selection SelectTerrain(LayoutCoordinate coordinate) {       
        Selection selection = new Selection();
        selection.setTerrain(coordinate);

        //MonoBehaviour.print("-----");

        //MonoBehaviour.print("LayoutCoordinate: " + coordinate.description);

        //MapCoordinate mapCoordinate = new MapCoordinate(coordinate);
        //MonoBehaviour.print("MapCoordinate: " + mapCoordinate.description);

        //WorldPosition worldPosition = new WorldPosition(mapCoordinate);
        //MonoBehaviour.print("WorldPosition: " + worldPosition.description);

        //MapCoordinate newMapCoordinate = MapCoordinate.FromWorldPosition(worldPosition);
        //MonoBehaviour.print("Back Into MapCoordinate: " + newMapCoordinate.description);

        //LayoutCoordinate layoutCoordinate = new LayoutCoordinate(newMapCoordinate);
        //MonoBehaviour.print("Back Into LayoutCoordinate: " + layoutCoordinate.description);

        //PathGridCoordinate anyGridCoordinate = PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(layoutCoordinate)[0][0];
        //MonoBehaviour.print("Any PathGridCoordiate: " + anyGridCoordinate.description);

        //MapCoordinate secondMapCoordinate = MapCoordinate.FromGridCoordinate(anyGridCoordinate);
        //MonoBehaviour.print("MapCoordinate For PathGrid: " + secondMapCoordinate.description);

        //WorldPosition pathGridWorldPosition = new WorldPosition(secondMapCoordinate);
        //MonoBehaviour.print("WorldPosition For PathGrid: " + worldPosition.description);

        //MapCoordinate thirdMapCoordinate = MapCoordinate.FromWorldPosition(pathGridWorldPosition);
        //MonoBehaviour.print("Back Into MapCoordinate: " + thirdMapCoordinate.description);

        //PathGridCoordinate newPathGrid = PathGridCoordinate.fromMapCoordinate(thirdMapCoordinate);
        //MonoBehaviour.print("Back Into PathGrid: " + newPathGrid.description);

        //MonoBehaviour.print("-----");

        //MonoBehaviour.print("-----");

        //foreach(PathGridCoordinate[] anyGridCoordinates in PathGridCoordinate.pathCoordiatesFromLayoutCoordinate(coordinate)) {
        //    foreach(PathGridCoordinate anyGridCoordinate in anyGridCoordinates) {
        //        MonoBehaviour.print("PathGridCoordiate: " + anyGridCoordinate.description);
        //    }
        //}

        //MonoBehaviour.print("-----");

        UpdateSelection(selection);
        return selection;
    }

    public Selection SelectSelectable(Selectable selectable) {
        Selection selection = new Selection();
        selection.selectionType = Selection.SelectionType.Selectable;
        selection.selection = selectable;

        selectable.SetSelected(true);
        //selectable.SetStatusDelegate(Script.Get<UIManager>());

        UpdateSelection(selection);
        return selection;
    }

    public void RemoveSelection() {
        if (currentSelection == null) {
            return;
        }

        currentSelection.Deselect();
    }

    public void RegisterForNotifications(SelectionManagerDelegate notificationDelegate) {
        delegateList.Add(notificationDelegate);
    }

    public void EndNotifications(SelectionManagerDelegate notificationDelegate) {
        delegateList.Remove(notificationDelegate);
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

    // Mutators


    public void Deselect() {
        if(selectionType == SelectionType.Terrain) {
        } else if(selectionType == SelectionType.Selectable) {
            selection.SetSelected(false);
            //selection.SetStatusDelegate(null);
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