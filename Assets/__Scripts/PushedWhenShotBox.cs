using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public class PushedWhenShotBox : MonoBehaviour// , IShootable
{
    private Rigidbody rb;
    [SerializeField] private float pushForce;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    //public void ReactShotServerRpc(Vector3 hitPoint, Vector3 shootingAngle)
    //{
    //    rb.AddForceAtPosition(shootingAngle * pushForce, hitPoint, ForceMode.Impulse);
    //}
}