using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-6)]
public class PlayerCombat : MonoBehaviour
{
    private Transform cameraTransform;

    [SerializeField] private CombatInputQuery InputQuery;
    [SerializeField] private LayerMask layersToHit;

    #region Shoot Setup

    [SerializeField] private float shootingCooldown;
    private bool shootingOnCooldown;
    private readonly float shootBuffer = .1f;
    private float lastShootPressed;
    private bool HasBufferedShoot => lastShootPressed + shootBuffer > Time.time;

    #endregion

    [SerializeField] private GameObject landingShotEffect;
    [SerializeField] private float hitEffectLifetime;


    #region Katana Setup

    [SerializeField] private float slashingCooldown;
    private bool slashingOnCooldown;
    private readonly float slashBuffer = .1f;
    private float lastSlashPressed;
    private bool HasBufferedSlash => lastSlashPressed + slashBuffer > Time.time;

    private BoxCaster[] katanaChecks;
    [SerializeField] private float slashSpeed;


    #endregion

    private void Awake()
    {
        //cameraTransform = Camera.main.transform;
        cameraTransform = transform.GetChild(0).GetChild(0);
        InputQuery.Init();
        katanaChecks = new BoxCaster[]
        {
            BoxCasterManager.Instance.RetrieveBoxCasterFromInstance(BoxCasterInstances.KATANA_LEFT_MOST),
            BoxCasterManager.Instance.RetrieveBoxCasterFromInstance(BoxCasterInstances.KATANA_LEFT),
            BoxCasterManager.Instance.RetrieveBoxCasterFromInstance(BoxCasterInstances.KATANA_MIDDLE),
            BoxCasterManager.Instance.RetrieveBoxCasterFromInstance(BoxCasterInstances.KATANA_RIGHT),
            BoxCasterManager.Instance.RetrieveBoxCasterFromInstance(BoxCasterInstances.KATANA_RIGHT_MOST),
        };
    }

    private void Update()
    {
        if (InputQuery.Shoot)
        { 
            TryShoot(false);
        }
        else if (HasBufferedShoot)
        {
            TryShoot(true);
        }
        else if (InputQuery.Slash) 
        {
            TrySlash(false);
        }
        else if (HasBufferedSlash)
        {
            TrySlash(true);
        }
    }

    #region Shooting

    private void TryShoot(bool fromBuffer)
    {
        if (!fromBuffer)
        {
            lastShootPressed = Time.time;
        }

        if (shootingOnCooldown)
        {
            return;
        }

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit hit, float.PositiveInfinity, layersToHit, QueryTriggerInteraction.Ignore))
        {
            if (hit.collider.gameObject.TryGetComponent<IShootable>(out var shootableComponent))
            {
                shootableComponent.ReactShot(hit.point, cameraTransform.forward);
            }

            Destroy(Instantiate(landingShotEffect, hit.point - cameraTransform.forward * .1f, Quaternion.identity), hitEffectLifetime) ;
        }

        StartCoroutine(ShootingCooldown());

    }

    private IEnumerator ShootingCooldown()
    {
        shootingOnCooldown = true;

        yield return new WaitForSeconds(shootingCooldown);
        
        shootingOnCooldown = false;
    }

    #endregion

    #region Slashing

    private void TrySlash(bool fromBuffer)
    {
        if (!fromBuffer)
        {
            lastSlashPressed = Time.time;
        }

        if (slashingOnCooldown)
        {
            return;
        }

        StartCoroutine(Slash(false));

    }

    private IEnumerator Slash(bool fromLeft)
    {
        slashingOnCooldown = true;

        foreach (var check in fromLeft ? GetKatanaChecksFromLeft() : GetKatanaChecksFromRight())
        {
            if (check.ReturnCast(layersToHit, out var collidersHit))
            {
                foreach(var collider in collidersHit)
                {
                    if (collider.gameObject.TryGetComponent<ISlashable>(out var slashableComponent))
                    {
                        slashableComponent.ReactSlash(cameraTransform.forward);
                    }
                }
            }
            
            yield return new WaitForSeconds(slashSpeed);
        }

        yield return new WaitForSeconds(slashingCooldown);

        slashingOnCooldown = false;
    }

    private IEnumerable<BoxCaster> GetKatanaChecksFromLeft()
    {
        for (int i = 0; i < 5; i++)
        {
            yield return katanaChecks[i];
        }
    }

    private IEnumerable<BoxCaster> GetKatanaChecksFromRight()
    {
        for (int i = 4; i > -1; i--)
        {
            yield return katanaChecks[i];
        }
    }

    #endregion



}

// bullet bounce??


