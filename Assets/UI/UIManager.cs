using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public Text text;
    public Button button;

    // Start is called before the first frame update
    void Awake()
    {
    }

    // Update is called once per frame
    void Start()
    {
        button.onClick.AddListener(buttonPress);
        button.enabled = false;
    }

    public void SetSelection(Selection selection) {
        text.text = selection.Title();

        button.enabled = selection.selectionType == Selection.SelectionType.Terrain;
    }

    private void buttonPress() {


        //Building building = new Building();
        LayoutCoordinate coordinate = Script.Get<PlayerBehaviour>().selection.coordinate;
        MapCoordinate mapCoordinate = new MapCoordinate(coordinate);
       
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localPosition = new WorldPosition(mapCoordinate).vector3;

        cube.AddComponent<Building>();
        cube.transform.localScale = new Vector3(25, 25, 25);

        TaskQueue queue = Script.Get<TaskQueue>();
        queue.QueueBuilding();



    }

   /* public void textCreation() {
        Font arial;
        arial = (Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf");

        // Create Canvas GameObject.
        GameObject canvasGO = new GameObject();
        canvasGO.name = "Canvas";
        canvasGO.AddComponent<Canvas>();
        canvasGO.AddComponent<CanvasScaler>();
        canvasGO.AddComponent<GraphicRaycaster>();

        // Get canvas from the GameObject.
        Canvas canvas;
        canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Create the Text GameObject.
        GameObject textGO = new GameObject();
        textGO.transform.parent = canvasGO.transform;
        textGO.AddComponent<Text>();

        // Set Text component properties.
        text = textGO.GetComponent<Text>();
        text.font = arial;
        text.text = "No Selection";
        text.fontSize = 15;
        text.alignment = TextAnchor.MiddleCenter;

        // Provide Text position and size using RectTransform.
        RectTransform rectTransform;
        rectTransform = text.GetComponent<RectTransform>();
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.localPosition = new Vector3(0, 0, 0);

        //Rect parentRect = rectTransform.GetComponentInParent<RectTransform>().rect;

        //rectTransform.anchoredPosition = new Vector2(-parentRect.size.x, -parentRect.size.y / 2f);



        rectTransform.anchorMin = new Vector2(0.8f, 0.8f);
        rectTransform.anchorMax = new Vector2(1, 1);


        //rectTransform.sizeDelta = new Vector2(600, 200);
    }*/
}
