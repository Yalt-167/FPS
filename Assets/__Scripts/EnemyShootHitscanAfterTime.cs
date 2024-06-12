using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyShootHitscanAfterTime : MonoBehaviour //, IShootable
{
    [SerializeField] private float timeToShoot;
    [SerializeField] private float viewDistance;
    private bool isLockedIn;
    [SerializeField] private LayerMask groundAndPlayerLayers;
    private Vector3 PlayerPosition => Vector3.zero; // PlayerMovement.Instance.Position;

    private LineRenderer lineRenderer;

    private void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.enabled = false;
    }

    private void Update()
    {
        transform.LookAt(PlayerPosition);

        if (CanSeePlayer())
        {
            lineRenderer.enabled = true;
            lineRenderer.SetPosition(0, transform.position);
            lineRenderer.SetPosition(1, PlayerPosition);
            if (!isLockedIn)
            {
                StartCoroutine(LockIn());
            }
        }
        else
        {
            lineRenderer.enabled = false;
        }

    }

    private IEnumerator LockIn()
    {
        isLockedIn = true;

        var startTime = Time.time;
        yield return new WaitUntil(() => !CanSeePlayer() || startTime + timeToShoot < Time.time);

        if (startTime + timeToShoot < Time.time) // if left because time is over
        {
            // actual shooting logic when I ll bother to do it
        }

        isLockedIn = false;
    }

    private bool CanSeePlayer()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, viewDistance, groundAndPlayerLayers))
        {
            return hit.collider.CompareTag("Player");   
        }
        return false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = CanSeePlayer() ? Color.red : Color.green;
        Gizmos.DrawLine(transform.position, transform.position + transform.forward * viewDistance);
    }

    //public void ReactShotServerRpc(Vector3 shootingAngle, Vector3 hitPoint)
    //{
    //    Destroy(gameObject);
    //}
}
