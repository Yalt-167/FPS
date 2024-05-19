using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    [SerializeField] protected float lifetime;

    protected float speed;
    protected float bulletDrop;
    protected ushort damage;
    protected ulong attackerNetworkID;
    protected bool canBreakThings;
    protected LayerMask layersToHit;
    protected bool active;

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
            active = false;
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
