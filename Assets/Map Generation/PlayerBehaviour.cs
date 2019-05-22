using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerBehaviour : MonoBehaviour
{
    public Material material;

    MapGenerator mapGenerator;
    public MapPoint selectedPoint = new MapPoint(0, 0);

    // Start is called before the first frame update
    void Start()
    {
        mapGenerator = GetComponent<MapGenerator>();
        mapGenerator.GenerateMap();

    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetMouseButtonDown(0)) {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit)) {

                MapDisplay display = FindObjectOfType<MapDisplay>();
                Transform mapMeshSpace = display.meshRenderer.transform;

                Vector3 selectionPoint = hit.point;
                Vector3 selectedPointOnMap = mapMeshSpace.InverseTransformPoint(selectionPoint);

                Debug.Log("Mouse Down Hit the following object: " + selectedPointOnMap);

                selectedPoint = new MapPoint(selectedPointOnMap.x + (MapGenerator.layoutMapWidth * MapGenerator.featuresPerLayoutPerAxis / 2f),
                    (-selectedPointOnMap.z) + (MapGenerator.layoutMapHeight * MapGenerator.featuresPerLayoutPerAxis / 2f));

                //mapGenerator.GenerateMap();

                material.SetFloat("selectedXOffsetLow", selectedPoint.virtualX * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapWidth * MapGenerator.featuresPerLayoutPerAxis / 2f));
                material.SetFloat("selectedXOffsetHigh", (selectedPoint.virtualX + 1) * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapWidth * MapGenerator.featuresPerLayoutPerAxis / 2f));

                material.SetFloat("selectedYOffsetLow", selectedPoint.virtualY * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapHeight * MapGenerator.featuresPerLayoutPerAxis / 2f));
                material.SetFloat("selectedYOffsetHigh", (selectedPoint.virtualY + 1) * MapGenerator.featuresPerLayoutPerAxis - (MapGenerator.layoutMapHeight * MapGenerator.featuresPerLayoutPerAxis / 2f));
            }

        }
    }
}
