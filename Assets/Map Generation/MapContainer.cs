using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapContainer : MonoBehaviour
{
    Map map;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    private void Awake() {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
    }

    public void setMap(Map map) {
        this.map = map;

        DrawMesh();
    }

    public Map getMap() {
        return map;
    }

    public void DrawMesh() {
        meshFilter.sharedMesh = map.meshData.FinalizeMesh();
        meshRenderer.sharedMaterial.mainTexture = map.meshTexture;
    }
}
