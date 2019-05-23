using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapContainer : MonoBehaviour
{
    //MeshFilter meshFilter;
    //MeshRenderer meshRenderer;

    Map map;

    private void Start() {
        //meshFilter = GetComponent<MeshFilter>();
        //meshRenderer = GetComponent<MeshRenderer>();
    }

    public void setMap(Map map) {
        this.map = map;

        DrawMesh(map.meshData, map.meshTexture);
    }

    public Map getMap() {
        return map;
    }

    public void DrawMesh(MeshData meshData, Texture2D meshTexture) {
        MeshFilter meshFilter = GetComponent<MeshFilter>();
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();

        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = meshTexture;
    }
}
