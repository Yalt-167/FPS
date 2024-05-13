using System.Collections;
using System.Collections.Generic;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

//[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] protected float lifetime;

    protected float speed;
    protected float bulletDrop;
    protected ushort damage;
    protected LayerMask layersToHit;
    public virtual void Init(ushort damage_, float speed_, float bulletDrop_,  LayerMask layersToHit_)
    {
        damage = damage_;
        layersToHit = layersToHit_;
        bulletDrop = bulletDrop_;
        speed = speed_;

        Destroy(gameObject, lifetime);
    }

    protected virtual void DealDamage()
    {

    }

    protected virtual void FixedUpdate()
    {
        transform.position += transform.forward * speed;
        transform.position -= transform.up * bulletDrop;
    }
}
