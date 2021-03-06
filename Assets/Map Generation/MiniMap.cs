﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class MiniMap : MonoBehaviour, BuildingsUpdateDelegate {

    public RectTransform rectTransform;

    public Image mainCameraImage;
    public Image builderUnitImage;

    public Image copperIcon;
    public Image silverIcon;
    public Image goldIcon;

    public Image lightTowerIcon;

    MapsManager mapsManager;

    Dictionary<Behaviour, Image> objectToIconMap = new Dictionary<Behaviour, Image>();
    Dictionary<Behaviour, Image> staticObjectToIconMap = new Dictionary<Behaviour, Image>();

    HashSet<Behaviour> remainingKeys = new HashSet<Behaviour>();

    public void Initialize() {
        mapsManager = Script.Get<MapsManager>();

        objectToIconMap[Camera.main] = mainCameraImage;

        builderUnitImage.gameObject.SetActive(false);

        copperIcon.gameObject.SetActive(false);
        silverIcon.gameObject.SetActive(false);
        goldIcon.gameObject.SetActive(false);

        lightTowerIcon.gameObject.SetActive(false);

        BuildingManager buildingManager = Script.Get<BuildingManager>();
        buildingManager.RegisterFoBuildingNotifications(this);

        StartCoroutine(UpdateMap());   
    }

    private enum ObjectType {
        Camera, Builder, Copper, Silver, Gold, LightTower
    }

    private Image ImageBaseFromObjectType(ObjectType objectType) {
        switch(objectType) {
            case ObjectType.Camera:
                return mainCameraImage;
            case ObjectType.Builder:
                return builderUnitImage;
            case ObjectType.Copper:
                return copperIcon;
            case ObjectType.Silver:
                return silverIcon;
            case ObjectType.Gold:
                return goldIcon;
            case ObjectType.LightTower:
                return lightTowerIcon;
        }

        return null;
    }

    IEnumerator UpdateMap() {
        while(true) {
            remainingKeys.Clear();
            remainingKeys = new HashSet<Behaviour>(objectToIconMap.Keys);

            DoUpdate(Camera.main, ObjectType.Camera);

            UnitManager unitManager = Script.Get<UnitManager>();
            Unit[] miners = unitManager.GetPlayerUnitsOfType(MasterGameTask.ActionType.Mine);

            foreach(Unit unit in miners) {
                DoUpdate(unit, ObjectType.Builder);
            }

            GameResourceManager gameResourceManager = Script.Get<GameResourceManager>();
            List<Ore> oreList = gameResourceManager.globalOreList;

            Ore[] copperList = oreList.Where(ore => ore.mineralType == MineralType.Copper).ToArray();

            foreach(Ore copper in copperList) {
                DoUpdate(copper, ObjectType.Copper);
            }

            Ore[] silverList = oreList.Where(ore => ore.mineralType == MineralType.Silver).ToArray();

            foreach(Ore silver in silverList) {
                DoUpdate(silver, ObjectType.Silver);
            }

            Ore[] goldList = oreList.Where(ore => ore.mineralType == MineralType.Gold).ToArray();

            foreach(Ore gold in goldList) {
                DoUpdate(gold, ObjectType.Gold);
            }

            foreach(Behaviour key in remainingKeys) {
                Image removingImage = objectToIconMap[key];
                removingImage.gameObject.SetActive(false);

                Destroy(removingImage);

                objectToIconMap.Remove(key);
            }

            yield return new WaitForSeconds(0.1f);
        }
    }

    private Image DoUpdate(Behaviour gameObject, ObjectType type, Dictionary<Behaviour, Image> fromList = null) {

        if (fromList == null) {
            fromList = objectToIconMap;
        }

        Vector3 localPosition = mapsManager.transform.InverseTransformPoint(gameObject.transform.position);
        Vector2 localPoint = new Vector2(localPosition.x, localPosition.z);

        float percentX = (localPoint.x + (mapsManager.mapsBoundaries.width / 2f)) / mapsManager.mapsBoundaries.width; 
        float percentY = (localPoint.y + (mapsManager.mapsBoundaries.height / 2f)) / mapsManager.mapsBoundaries.height;

        if(!fromList.ContainsKey(gameObject)) {
            Image newImage = Image.Instantiate<Image>(ImageBaseFromObjectType(type));
            newImage.rectTransform.localPosition = Vector3.zero;

            newImage.transform.SetParent(transform, false);
            newImage.gameObject.SetActive(true);

            fromList[gameObject] = newImage;
        }

        Image icon = fromList[gameObject];

        icon.rectTransform.anchorMin = new Vector2(percentX, percentY);
        icon.rectTransform.anchorMax = new Vector2(percentX, percentY);

        remainingKeys.Remove(gameObject);

        return icon;
    }

    /*
     * BuildingsUpdateDelegate Interface
     * */



    public void NewBuildingStarted(Building building) {
        Image icon = DoUpdate(building, ObjectType.LightTower, staticObjectToIconMap);

        Color color = icon.color;
        color.a = 0.5f;
        icon.color = color;
    }

    public void BuildingFinished(Building building) {
        Image icon = DoUpdate(building, ObjectType.LightTower, staticObjectToIconMap);

        Color color = icon.color;
        color.a = 1f;
        icon.color = color;
    }
}
