using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapContainer : MonoBehaviour
{
    Map map;

    MeshFilter meshFilter;
    MeshRenderer meshRenderer;

    public void setMap(Map map) {
        this.map = map;

        DrawMesh();
    }

    public Map getMap() {
        return map;
    }

    public void DrawMesh() {
        if (meshFilter == null) {
            meshFilter = GetComponent<MeshFilter>();
            meshRenderer = GetComponent<MeshRenderer>();
        }

        meshFilter.sharedMesh = map.meshData.FinalizeMesh();
        meshRenderer.sharedMaterial.mainTexture = map.meshTexture;
    }
}
