using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-6)]
public class PlayerCombat : MonoBehaviour, IPlayerFrameMember
{

    public PlayerFrame PlayerFrame { get; set; }

    public void InitPlayerFrame(PlayerFrame playerFrame)
    {
        PlayerFrame = playerFrame;
    }


    [SerializeField] private int allowedWeaponsCount = 3;
    [SerializeField] private CombatInputQuery inputQuery;
    [SerializeField] private Weapon[] weapons;
    private WeaponHandler weaponHandler;
    private int currentWeaponIndex;
    [SerializeField] private LayerMask layersToHit;
    private static readonly string ScrollWheelAxis = "Mouse ScrollWheel";

    #region Katana Setup

    [SerializeField] private float slashingCooldown;
    private bool slashingOnCooldown;
    private readonly float slashBuffer = .1f;
    private float lastSlashPressed;
    private bool HasBufferedSlash => lastSlashPressed + slashBuffer > Time.time;

    [SerializeField] private BoxCaster[] katanaChecks;
    [SerializeField] private float slashSpeed;


    #endregion

    private void OnValidate()
    {
        if (weapons.Length > allowedWeaponsCount)
        {
            var temp = new Weapon[allowedWeaponsCount];
            for (int i = 0; i < allowedWeaponsCount; i++)
            {
                temp[i] = weapons[i];
            }
            weapons = temp;
        }
    }

    private void Awake()
    {
        //cameraTransform = transform.GetChild(0).GetChild(0);
        weaponHandler = GetComponent<WeaponHandler>();
        inputQuery.Init();
        UpdateWeapon();
    }

    private void Update()
    {
        // so far there might be an exploit -> init gun after each shot effectively reseting its cd
        HandleWeaponSwitch(); // upon check: there is


        weaponHandler.UpdateAimingState(inputQuery.Aim);

        if (inputQuery.Reload)
        {
            weaponHandler.Reload();
        }

        weaponHandler.UpdateState(inputQuery.Shoot);
    }

    private void HandleWeaponSwitch()
    {
        if (inputQuery.FirstGun)
        {
            UpdateWeaponWithIndex(0);
            return;
        }
        else if (inputQuery.SecondGun)
        {
            UpdateWeaponWithIndex(1);
            return;
        }
        else if (inputQuery.ThirdGun)
        {
            UpdateWeaponWithIndex(2);
            return;
        }

        

        var scrollWheelInput = Input.GetAxis(ScrollWheelAxis);
        if (scrollWheelInput != 0f)
        {
            currentWeaponIndex = Utility.ModuloThatWorksWithNegatives(scrollWheelInput > 0f ? ++currentWeaponIndex : --currentWeaponIndex, allowedWeaponsCount);
            UpdateWeapon();
        }
    }


    private void UpdateWeaponWithIndex(int index)
    {
        weaponHandler.SetWeapon(weapons[currentWeaponIndex = index]);
    }

    private void UpdateWeapon()
    {
        weaponHandler.SetWeapon(weapons[currentWeaponIndex]);
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
                        //slashableComponent.ReactSlash(cameraTransform.forward);
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