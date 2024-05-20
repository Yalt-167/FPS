using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 Position => transform.position;


    [SerializeField] protected float lifetime;

    protected float speed;
    protected float bulletDrop;
    protected ushort damage;
    protected ulong attackerNetworkID;
    protected bool canBreakThings;
    protected LayerMask layersToHit;
    protected bool active;
    [SerializeField] protected ProjectileOnHitWallBehaviour projectileOnHitWallBehaviour;
    [SerializeField] protected ProjectileOnHitPlayerBehaviour projectileOnHiPlayerBehaviour;

    protected virtual void Awake()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    public virtual void Init(ushort damage_, float speed_, float bulletDrop_, ulong attackerNetworkID_, bool canBreakThings_, LayerMask layersToHit_)
    {
        active = true;
        damage = damage_;
        layersToHit = layersToHit_;
        bulletDrop = bulletDrop_;
        speed = speed_;
        attackerNetworkID = attackerNetworkID_;
        canBreakThings = canBreakThings_;

        StartCoroutine(CleanUp());
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IShootable>(out var shootableComponent))
        {
            shootableComponent.ReactShot(damage, transform.forward, Vector3.zero, attackerNetworkID, canBreakThings);
            if (projectileOnHiPlayerBehaviour != null)
            {
                projectileOnHiPlayerBehaviour.OnHitPlayer(this, shootableComponent);
            }
            active = false;
        }
        else if (other.TryGetComponent<Ground>(out var _))
        {
            if (projectileOnHitWallBehaviour != null)
            {
                projectileOnHitWallBehaviour.OnHitWall(this, other);
            }
        }
    }

    protected virtual void FixedUpdate()
    {
        transform.position += transform.forward * speed;
        transform.position -= transform.up * bulletDrop;
    }

    protected virtual IEnumerator CleanUp()
    {
        var startTime = Time.time;

        yield return new WaitUntil(() => startTime + lifetime < Time.time || !active);

        Destroy(gameObject);
    }
}
