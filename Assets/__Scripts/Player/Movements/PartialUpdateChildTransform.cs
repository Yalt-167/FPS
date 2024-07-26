using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PartialUpdateChildTransform : MonoBehaviour
{
    private MyTransform previousData;

    [SerializeField] private bool lockPosition;

    [SerializeField] private bool lockScale;

    [SerializeField] private bool lockRotation;


    private void Awake()
    {
        previousData = new MyTransform(transform);
    }

    private void FixedUpdate()
    {
        if (lockPosition)
        {
            transform.position = previousData.Position;
        }

        if (lockScale)
        {
            transform.localScale = previousData.Scale;
        }

        if (lockRotation)
        {
            transform.rotation = previousData.Rotation;
        }
    }
}
