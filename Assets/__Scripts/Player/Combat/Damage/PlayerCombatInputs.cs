using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using GameManagement;
using Inputs;

[DefaultExecutionOrder(-6)]
public sealed class PlayerCombatInputs : MonoBehaviour
{
    [SerializeField] private int allowedWeaponsCount = 3;
    private InputManager inputManager;
    private CombatInputQuery InputQuery => inputManager.CombatInputs;
    [SerializeField] private WeaponScriptableObject[] weapons;
    private WeaponHandler weaponHandler;
    private int currentWeaponIndex;
    [SerializeField] private LayerMask layersToHit;
    private static readonly string ScrollWheelAxis = "Mouse ScrollWheel";

    private void OnValidate()
    {
        if (weapons.Length > allowedWeaponsCount)
        {
            var temp = new WeaponScriptableObject[allowedWeaponsCount];
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
        inputManager = GetComponent<InputManager>();
        UpdateWeapon();
    }

    private void Update()
    {
        // so far there might be an exploit -> init gun after each shot effectively reseting its cd
        HandleWeaponSwitch(); // upon check: there is


        weaponHandler.UpdateAimingState(InputQuery.Aim);

        if (InputQuery.Reload)
        {
            weaponHandler.Reload();
        }

        weaponHandler.UpdateState(InputQuery.Shoot);
    }

    private void HandleWeaponSwitch()
    {
        if (InputQuery.FirstGun)
        {
            UpdateWeaponWithIndex(0);
            return;
        }
        else if (InputQuery.SecondGun)
        {
            UpdateWeaponWithIndex(1);
            return;
        }
        else if (InputQuery.ThirdGun)
        {
            UpdateWeaponWithIndex(2);
            return;
        }

        var scrollWheelInput = Input.GetAxis(ScrollWheelAxis);
        if (scrollWheelInput != 0f)
        {
            UpdateWeaponWithIndex(
                MyUtility.Utility.ModuloThatWorksWithNegatives(scrollWheelInput > 0f ? ++currentWeaponIndex : --currentWeaponIndex, allowedWeaponsCount)
            );
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
}