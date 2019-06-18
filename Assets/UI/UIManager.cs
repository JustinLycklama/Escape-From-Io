using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;



public class UIManager : MonoBehaviour
{
    public Text selectionTitle;
    //public Button button;

    //public TableView actionsTable;
    //public TableView queueTable;

    public MasterAndGameTaskCell selectionCurrentTaskCell;


    Selection selection;



    public NavigationPanel currentTopPanel;

    public class Blueprint : PrefabBlueprint {
        public Blueprint(string fileName, string description, Type type) : base(fileName, description, type) { }

        public static Blueprint TaskAndUnitDetail = new Blueprint("TaskAndUnitDetailPanel", "Task and Unit Details", typeof(TaskAndUnitDetailPanel));
    }

    public NavigationPanel Push(Blueprint blueprint) {
        NavigationPanel panel = blueprint.Instantiate() as NavigationPanel;
        panel.PushOntoStackFrom(currentTopPanel);
        currentTopPanel = panel;

        return panel;
    }

    public void Pop() {
        currentTopPanel = currentTopPanel.PopFromStack();
    }

    public void SetSelection(Selection selection) {
        this.selection = selection;

        selectionTitle.text = selection.Title();
        

        //actionsTable.ReloadData(this);

        //button.enabled = selection.selectionType == Selection.SelectionType.Terrain;

    }

    private void buttonPress() {


        ////Building building = new Building();
        //LayoutCoordinate coordinate = Script.Get<PlayerBehaviour>().selection.coordinate;
        //MapCoordinate mapCoordinate = new MapCoordinate(coordinate);
        //WorldPosition worldPosition = new WorldPosition(mapCoordinate);

        //worldPosition.y += 25 / 2f;

        //GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        //cube.transform.position = worldPosition.vector3;

        //Material newMat = Resources.Load("BuildingMaterial", typeof(Material)) as Material;
        //cube.GetComponent<MeshRenderer>().material = newMat;

        //cube.AddComponent<Building>();

        //cube.transform.localScale = new Vector3(25, 25, 25);

        //TaskQueueManager queue = Script.Get<TaskQueueManager>();
        //queue.QueueTask(new MasterGameTask(worldPosition, MasterGameTask.ActionType.Build, cube.GetComponent<Building>()));
    }


    public void UpdateActionsList() {
        //if(itemLabelObjects.Count < taskList.Length) {
        //    for(int i = itemLabelObjects.Count; i < taskList.Length; i++) {
        //        GameObject newLabel = Instantiate(taskItemLabel);

        //        newLabel.transform.SetParent(taskLayoutGroup.transform, true);
        //        itemLabelObjects.Add(newLabel);
        //    }
        //} else if(itemLabelObjects.Count > taskList.Length) {
        //    for(int i = itemLabelObjects.Count - 1; i >= taskList.Length; i--) {
        //        Destroy(itemLabelObjects[i]);
        //        itemLabelObjects.RemoveAt(i);
        //    }
        //}

        //for(int i = 0; i < taskList.Length; i++) {
        //    itemLabelObjects[i].GetComponent<TaskItemLabel>().SetTask(taskList[i]);
        //}
    }


    //MasterGameTask[] taskList;

    //public void UpdateTaskList(MasterGameTask[] taskList, MasterGameTask.ActionType taskType) {
    //    this.taskList = taskList;


        //queueTable.ReloadData(this);



        //    if (itemLabelObjects.Count < taskList.Length) {
        //        for (int i = itemLabelObjects.Count; i < taskList.Length; i++) {
        //            GameObject newLabel = Instantiate(taskItemLabel);

        //            newLabel.transform.SetParent(taskLayoutGroup.transform, true);
        //            itemLabelObjects.Add(newLabel);
        //        }
        //    } else if (itemLabelObjects.Count > taskList.Length) {
        //        for (int i = itemLabelObjects.Count - 1; i >= taskList.Length; i--) {
        //            Destroy(itemLabelObjects[i]);
        //            itemLabelObjects.RemoveAt(i);
        //        }
        //    }

        //    for (int i = 0; i < taskList.Length; i++) {
        //        itemLabelObjects[i].GetComponent<TaskItemLabel>().SetTask(taskList[i]);
        //    }
    //}

    // TABLEVIEW DELEGATE
    //public int NumberOfRows(TableView table) {
    //    if(table == queueTable) {
    //        print("Displaying " + taskList.Length);
    //        return taskList.Length;
    //    } else if(table == actionsTable) {
    //        return selection.UserActions().Length;
    //    }

    //    return 0;
    //}

    //public void CellForRowAtIndex(TableView table, int row, GameObject cell) {
    //    if(table == queueTable) {
    //        //cell.GetComponent<TaskItemCell>().SetTask(taskList[row]);
    //    } else if(table == actionsTable) {
    //        cell.GetComponent<ActionItemCell>().SetAction(selection.UserActions()[row]);
    //    }
    //}

    // MARK Status Delegate

    //public void InformCurrentTask(MasterGameTask task, GameTask gameTask) {
    //    selectionCurrentTaskCell.SetTask(task, gameTask);
    //}

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
