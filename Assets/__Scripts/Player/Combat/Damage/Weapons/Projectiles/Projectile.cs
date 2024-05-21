using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public Vector3 Position => transform.position;
    public Vector3 Direction => transform.forward;

    [SerializeField] protected float lifetime;

    protected float speed;
    protected float bulletDrop;
    protected ushort damage;
    protected ulong attackerNetworkID;
    protected bool canBreakThings;
    protected LayerMask layersToHit;
    protected bool active;
    protected ProjectileOnHitWallBehaviour projectileOnHitWallBehaviour;
    protected ProjectileOnHitPlayerBehaviour projectileOnHiPlayerBehaviour;

    protected virtual void Awake()
    {
        projectileOnHitWallBehaviour = GetComponent<ProjectileOnHitWallBehaviour>();
        projectileOnHiPlayerBehaviour = GetComponent<ProjectileOnHitPlayerBehaviour>();
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

    protected void OnCollisionEnter(Collision collision)
    {
        var col = collision.collider;
        if (col.TryGetComponent<IShootable>(out var shootableComponent))
        {
            shootableComponent.ReactShot(damage, transform.forward, Vector3.zero, attackerNetworkID, canBreakThings);
            if (projectileOnHiPlayerBehaviour != null)
            {
                projectileOnHiPlayerBehaviour.OnHitPlayer(this, shootableComponent);
            }
            active = false;
        }
        else if (col.TryGetComponent<Ground>(out var _))
        {
            if (projectileOnHitWallBehaviour != null)
            {
                projectileOnHitWallBehaviour.OnHitWall(this, col);
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

    public void SetDirection(Vector3 newDirection)
    {
        transform.LookAt(transform.position + newDirection); ;
    }
}
