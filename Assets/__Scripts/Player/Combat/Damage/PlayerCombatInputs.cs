using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using Inputs;

[DefaultExecutionOrder(-6)]
public sealed class PlayerCombatInputs : MonoBehaviour
{
    private InputManager inputManager;
    private CombatInputQuery InputQuery => inputManager.CombatInputs;
    private WeaponHandler weaponHandler;
    private int currentWeaponIndex;
    private static readonly string ScrollWheelAxis = "Mouse ScrollWheel";

#pragma warning disable
    private void Awake()
#pragma warning restore
    {
        weaponHandler = GetComponent<WeaponHandler>();
        inputManager = GetComponent<InputManager>();
    }

#pragma warning disable
    private void Update()
#pragma warning restore
    {
        HandleWeaponSwitch();

        weaponHandler.UpdateAimingState(InputQuery.Aim); // switch Aim for SecondaryFire ? could be nice for some fancy waps

        if (InputQuery.Reload)
        {
            weaponHandler.Reload();
        }

        weaponHandler.UpdateState(InputQuery.Fire);
    }

    private void HandleWeaponSwitch()
    {
        if (InputQuery.FirstGun) // so far fixed for 3 waps but might need to find another way to do this
        {
            UpdateWeapon(0);
            return;
        }
        else if (InputQuery.SecondGun)
        {
            UpdateWeapon(1);
            return;
        }
        else if (InputQuery.ThirdGun)
        {
            UpdateWeapon(2);
            return;
        }

        var scrollWheelInput = Input.GetAxis(ScrollWheelAxis);
        if (scrollWheelInput != 0f)
        {
            UpdateWeapon(scrollWheelInput > 0f ? ++currentWeaponIndex : --currentWeaponIndex);
        }
    }

    private void UpdateWeapon(int index)
    {
        currentWeaponIndex = weaponHandler.SetWeapon(index);
    }
}