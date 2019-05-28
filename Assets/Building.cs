using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Building : MonoBehaviour
{
    float percentComplete = 0;
    Color materialColor;

    MeshRenderer renderer;

    // Start is called before the first frame update
    void Start()
    {
        renderer = GetComponent<MeshRenderer>();
        renderer.material.shader = Shader.Find("Transparent/Diffuse");

        materialColor = renderer.material.color;
    }

    // Update is called once per frame
    void Update()
    {
        materialColor.a = Mathf.Clamp(percentComplete, 0.10f, 1f);
        renderer.material.color = materialColor;
    }
}
