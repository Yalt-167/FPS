using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public sealed class CustomChildTransform : MonoBehaviour
{
    [SerializeField] private Transform parentTransform;

    private MyTransform parentTransformUnupdated;

    [SerializeField] private bool copyPosition;
    [SerializeField] private bool copyScale;
    [SerializeField] private bool copyRotation;


    private void Start()
    {
        parentTransformUnupdated = new MyTransform(parentTransform);
        print(parentTransformUnupdated.ToString());
    }
    private void Update()
    {
        if (parentTransform.hasChanged)
        {
            if (copyPosition) { CopyPosition(); }
            if (copyScale) { CopyScale(); }
            if (copyRotation) { CopyRotation(); }
        }
    }


    private void LateUpdate()
    {
        if (parentTransform.hasChanged)
        {
            parentTransform.hasChanged = false;
        }
    }


    private void CopyPosition()
    {
        transform.position += parentTransform.position - parentTransformUnupdated.Position;
    }

    private void CopyScale()
    {
        var scale = parentTransform.localScale;
        var previousScale = parentTransformUnupdated.Scale;

        var ratios = new Vector3(
            scale.x / previousScale.x,
            scale.y / previousScale.y,
            scale.z / previousScale.z
            );
        
        transform.localScale = transform.localScale.Mask(ratios);
    }

    private void CopyRotation()
    {
        //transform.rotation += parentTransform.rotation - parentTransformUnupdated.Rotation;
    }
}

public struct MyTransform
{
    public Vector3 Position;
    public Vector3 Scale;
    public Quaternion Rotation;
    
    public MyTransform(Transform modelTransform)
    {
        Position = modelTransform.position;
        Scale = modelTransform.localScale;
        Rotation = modelTransform.rotation;
    }

    public void CopyDataFrom(Transform modelTransform)
    {
        Position = modelTransform.position;
        Scale = modelTransform.localScale;
        Rotation = modelTransform.rotation;
    }

    public readonly new string ToString()
    {
        return $"MyTransform with position {Position}, scale {Scale}, rotation {Rotation}";
    }
}
