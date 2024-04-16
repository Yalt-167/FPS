using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ExplodeWhenShot : MonoBehaviour, IShootable
{
    [SerializeField] private float explosionForce;
    [SerializeField] private float explosionRadius;
    [SerializeField] private float verticalBoostCoefficient;
    public void ReactShot(Vector3 _, Vector3 __)
    {
        print("baboom");
        var vecPlayerBarrelSqrMagnitude = (PlayerMovement.Instance.transform.position - transform.position).sqrMagnitude;
        if (vecPlayerBarrelSqrMagnitude < explosionRadius * explosionRadius)
        {
            //MyPlayerMovement.Instance.AddExternalForces((transform.position - MyPlayerMovement.Instance.transform.position).normalized, explosionForce * Mathf.InverseLerp();
            // Calculate the normalized direction vector from the player to the explosion point
            Vector3 direction = (PlayerMovement.Instance.transform.position - transform.position).normalized;

            // Calculate the distance factor using InverseLerp
            //float distanceFactor = Mathf.Sqrt(Mathf.InverseLerp(0, explosionRadius * explosionRadius, vecPlayerBarrelSqrMagnitude));
            //print(distanceFactor);

            direction.y *= verticalBoostCoefficient;
            // Apply force with a magnitude that depends on the distance factor
            PlayerMovement.Instance.AddExternalForces(direction, explosionForce);
        }
        
        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
