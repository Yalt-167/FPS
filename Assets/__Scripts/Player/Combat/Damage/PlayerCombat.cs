using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-6)]
public class PlayerCombat : MonoBehaviour
{
    private Transform cameraTransform;

    [SerializeField] private CombatInputQuery InputQuery;
    [SerializeField] private WeaponHandler weaponHandler;
    [SerializeField] private LayerMask layersToHit;

    #region Katana Setup

    [SerializeField] private float slashingCooldown;
    private bool slashingOnCooldown;
    private readonly float slashBuffer = .1f;
    private float lastSlashPressed;
    private bool HasBufferedSlash => lastSlashPressed + slashBuffer > Time.time;

    [SerializeField] private BoxCaster[] katanaChecks;
    [SerializeField] private float slashSpeed;


    #endregion

    private void Awake()
    {
        cameraTransform = transform.GetChild(0).GetChild(0);
        InputQuery.Init();
    }

    private void Update()
    {
        if (InputQuery.SwitchGun)
        {
            weaponHandler.InitGun(); // so far there might be an exploit -> initing gun after each shot effectively reseting its cd
        }

        weaponHandler.UpdateAimingState(InputQuery.Aim);

        if (InputQuery.Reload)
        {
            weaponHandler.Reload();
        }

        weaponHandler.UpdateState(InputQuery.Shoot);
    }

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