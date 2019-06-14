using UnityEngine;

public interface StatusDelegate {
    void InformCurrentTask(MasterGameTask task);
}

public interface Selectable {
    void SetSelected(bool selected);
    void SetStatusDelegate(StatusDelegate statusDelegate);

    string description { get; }
}

public class Selection {

    public enum SelectionType { Terrain, Selectable }
    public SelectionType selectionType;

    // Terrain Properties
    public LayoutCoordinate coordinate;

    Selectable selection;

    private Selection() { }

    // Accessors
    public static Selection createTerrainSelection(LayoutCoordinate coordinate) {
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


        return selection;
    }

    public void setTerrain(LayoutCoordinate coordinate) {
        selectionType = SelectionType.Terrain;
        this.coordinate = coordinate;

        // Select
        Constants constants = Script.Get<Constants>();
        Material mapMaterial = Script.Get<MapsManager>().GetMaterialForMap(coordinate);

        mapMaterial.SetFloat("selectedXOffsetLow", coordinate.x * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));
        mapMaterial.SetFloat("selectedXOffsetHigh", (coordinate.x + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapWidth * constants.featuresPerLayoutPerAxis / 2f));

        mapMaterial.SetFloat("selectedYOffsetLow", coordinate.y * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));
        mapMaterial.SetFloat("selectedYOffsetHigh", (coordinate.y + 1) * constants.featuresPerLayoutPerAxis - (constants.layoutMapHeight * constants.featuresPerLayoutPerAxis / 2f));

        mapMaterial.SetFloat("hasSelection", 1);

        // Set info
    }

    public static Selection createSelectableSelection(Selectable selectable) {
        Selection selection = new Selection();
        selection.selectionType = SelectionType.Selectable;
        selection.selection = selectable;

        selectable.SetSelected(true);
        selectable.SetStatusDelegate(Script.Get<UIManager>());

        return selection;
    }


    // Mutators


    public void deselectCurrent() {
        if(selectionType == SelectionType.Terrain) {
            Material mapMaterial = Script.Get<MapsManager>().GetMaterialForMap(coordinate);
            mapMaterial.SetFloat("hasSelection", 0);

        } else if(selectionType == SelectionType.Selectable) {
            selection.SetSelected(false);
            selection.SetStatusDelegate(null);
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

    public UserAction[] UserActions() {
        switch(selectionType) {
            case SelectionType.Terrain:
                return Script.Get<MapsManager>().ActionsAvailableAt(coordinate);
            case SelectionType.Selectable:

                return new UserAction[0];
            default:
                return new UserAction[0];
        }
    }
}