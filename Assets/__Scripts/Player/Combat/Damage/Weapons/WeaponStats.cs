using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "ScriptableObjects/WeaponStats")]
public class WeaponStats : ScriptableObject
{
    public ushort Damage;
    public ShootingStyle ShootingStyle;
    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;
    public float CooldownBetweenShotsOfBurst;

    public float RampUpMinCooldown;
    public float RampUpMaxCooldown;
    public float RampUpCooldownIncreasePerShot;
    public float RampUpCooldownRegulationDuration;

    public ushort MagazineSize; //
    public ushort MaxAmmoCount; //
    public float ReloadSpeed; //
    public ushort AmmoLeftInMagazineToWarn;

    public bool CanHoldShootingKey;

    public float ADS_FOV;
    [Range(0, 5)][Tooltip("Set to 0 for no Scope")] public float ScopeMagnification;
    public float TimeToADS;
    public float TimeToUnADS;

    public float MaxSpread;
    public float SpreadAccumulatedPerShot;
    public float SpreadRegulationTime;

    public float RecoilForce;
    public float RecoilRegulationTime;

    public float WeaponKickBackPerShot;
    public float WeaponKickBackRegulationTime;
    public float FarthestPointBehindInitialPosition;
}

public enum ShootingStyle : byte
{
    Single,
    Shotgun,
}

public enum ShootingRythm : byte
{
    Single,
    Burst,
    RampUp,
}