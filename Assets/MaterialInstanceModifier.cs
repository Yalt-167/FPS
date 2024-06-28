using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialInstanceModifier : MonoBehaviour
{
    [SerializeField] Vector2 tiling;
    [SerializeField] Color color;

    void Start()
    {
        var renderer = GetComponent<MeshRenderer>();
        var material = renderer.material;
        material.color = color;
        //material.SetTextureScale(Shader.PropertyToID("_MainTex_ST"), tiling); // "_MainTex_ST" is not a valid property :/
    }
}
