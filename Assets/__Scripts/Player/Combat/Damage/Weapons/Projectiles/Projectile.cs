using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Projectiles
{
    public class Projectile : MonoBehaviour
    {
        public Vector3 Position => transform.position;
        public Vector3 Direction => transform.forward;
        public ulong Owner => attackerNetworkObjectID;
        public bool CanBreakThings => canBreakThings;

        [SerializeField] protected float lifetime;

        protected float speed;
        protected float bulletDrop;
        protected DamageDealt damage;
        protected ulong attackerNetworkObjectID;
        protected bool canBreakThings;
        protected LayerMask layersToHit;
        protected bool active;
        protected ProjectileOnHitWallBehaviour onHitWallBehaviour;
        protected ProjectileOnHitPlayerBehaviour onHitPlayerBehaviour;
        protected ushort shooterTeamNumber;

        public virtual void Init(
            DamageDealt damage_, float speed_, float bulletDrop_, ulong attackerNetworkObjectID_, bool canBreakThings_, LayerMask layersToHit_,
            ProjectileOnHitWallBehaviour onHitWallBehaviour_,
            ProjectileOnHitPlayerBehaviour onHitPlayerBehaviour_,
            ushort shooterTeamNumber_
            )
        {
            active = true;
            damage = damage_;
            layersToHit = layersToHit_;
            bulletDrop = bulletDrop_;
            speed = speed_;
            attackerNetworkObjectID = attackerNetworkObjectID_;
            canBreakThings = canBreakThings_;

            onHitWallBehaviour = onHitWallBehaviour_;
            onHitPlayerBehaviour = onHitPlayerBehaviour_;

            shooterTeamNumber = shooterTeamNumber_;

            StartCoroutine(CleanUp());
        }

        protected void OnCollisionEnter(Collision collision)
        {
            var col = collision.collider;
            if (col.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(damage, transform.forward, Vector3.zero, attackerNetworkObjectID, shooterTeamNumber, canBreakThings);
                onHitPlayerBehaviour.OnHitPlayer(this, shootableComponent);
                active = false;
            }
            else if (col.TryGetComponent<Ground>(out var _))
            {
                onHitWallBehaviour.OnHitWall(this, col);
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

            Destroy(gameObject); // add some sort of object pooling at some point
        }

        public void SetDirection(Vector3 newDirection)
        {
            transform.LookAt(transform.position + newDirection); ;
        }

        public void Deactivate()
        {
            active = false;
        }
    }
}