using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletTrail : MonoBehaviour
{
    private LineRenderer lineRenderer;
    [SerializeField] private float lifeTime;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void Set(Vector3 origin, Vector3 end)
    {
        lineRenderer.SetPosition(0, origin);
        lineRenderer.SetPosition(1, end);
        Destroy(gameObject, lifeTime);

    }
}
