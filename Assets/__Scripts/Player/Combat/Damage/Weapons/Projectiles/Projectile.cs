using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] protected float lifetime;

    protected float speed;
    protected float bulletDrop;
    protected ushort damage;
    protected ulong attackerNetworkID;
    protected bool canBreakThings;
    protected LayerMask layersToHit;

    protected virtual void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public virtual void Init(ushort damage_, float speed_, float bulletDrop_, ulong attackerNetworkID_, bool canBreakThings_, LayerMask layersToHit_)
    {
        damage = damage_;
        layersToHit = layersToHit_;
        bulletDrop = bulletDrop_;
        speed = speed_;
        attackerNetworkID = attackerNetworkID_;
        canBreakThings = canBreakThings_;

        Destroy(gameObject, lifetime);
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        print("found sth");
        if (other.TryGetComponent<IShootable>(out var shootableComponent))
        {
            print("found match");
            shootableComponent.ReactShot(damage, transform.forward, Vector3.zero, attackerNetworkID, canBreakThings);
        }
    }

    protected virtual void FixedUpdate()
    {
        transform.position += transform.forward * speed;
        transform.position -= transform.up * bulletDrop;
    }
}
