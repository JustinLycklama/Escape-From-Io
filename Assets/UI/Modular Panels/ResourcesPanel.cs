using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourcesPanel : MonoBehaviour, OreUpdateDelegate {

    [System.Serializable]
    public struct ImageValuePanel {
        public MineralType mineralType;
        public ImageValueCell imageValueCell;
    }

    public List<ImageValuePanel> imageValuePanels;
    Dictionary<MineralType, ImageValueCell> mineralTypeToValueCell;

    private void Awake() {
        mineralTypeToValueCell = new Dictionary<MineralType, ImageValueCell>();

        foreach(ImageValuePanel panel in imageValuePanels) {
            ImageValueCell cell = panel.imageValueCell;

            cell.SetIntegerValue(0);
            mineralTypeToValueCell[panel.mineralType] = cell;
        }
    }

    private void Start() {
        Script.Get<GameResourceManager>().RegisterFoOreNotifications(this);
    }

    public void NewOreCreated(Ore ore) {
        mineralTypeToValueCell[ore.mineralType].Increment();
    }

    public void OreRemoved(Ore ore) {
        mineralTypeToValueCell[ore.mineralType].Decrement();
    }
}
