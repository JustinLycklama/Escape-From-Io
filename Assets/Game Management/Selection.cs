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