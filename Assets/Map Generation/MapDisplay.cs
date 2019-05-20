using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapDisplay : MonoBehaviour
{
    public Renderer textureRenderer;

    public Renderer layoutTextureRenderer;
    public Renderer featuresTextureRenderer;

    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;

    public MeshFilter layoutMeshFilter;
    public MeshRenderer layoutMeshRenderer;
    public MeshFilter featuresMeshFilter;
    public MeshRenderer featuresMeshRenderer;

    public void DrawTexture(Texture2D texture) {
        //  textureRenderer.meaterial is instatiated at run time, the shared material is persistant.
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height);
    }

    public void DrawTextures(Texture2D layoutTexture, Texture2D featuresTexture) {
        layoutTextureRenderer.sharedMaterial.mainTexture = layoutTexture;
        layoutTextureRenderer.transform.localScale = new Vector3(layoutTexture.width, 1, layoutTexture.height);

        featuresTextureRenderer.sharedMaterial.mainTexture = featuresTexture;
        featuresTextureRenderer.transform.localScale = new Vector3(featuresTexture.width, 1, featuresTexture.height);
    }

    public void DrawMesh(MeshData meshData, Texture2D meshTexture) {
        meshFilter.sharedMesh = meshData.CreateMesh();
        meshRenderer.sharedMaterial.mainTexture = meshTexture;
    }

    public void DrawMeshes(MeshData layoutMeshData, Texture2D layoutMeshTexture,
        MeshData fraturesMeshData, Texture2D featuresMeshTexture) {

        layoutMeshFilter.sharedMesh = layoutMeshData.CreateMesh();
        layoutMeshRenderer.sharedMaterial.mainTexture = layoutMeshTexture;

        featuresMeshFilter.sharedMesh = fraturesMeshData.CreateMesh();
        featuresMeshRenderer.sharedMaterial.mainTexture = featuresMeshTexture;
    }
}
