using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "ScriptableObjects/WeaponStats")]
public class WeaponStats : ScriptableObject
{
    public ushort Damage;
    public WeaponClass WeaponClass;

    [Header("Magazine")]
    public ushort MagazineSize;
    // public bool NeedReload
    public float ReloadSpeed; // some that can reload one per one
    public ushort AmmoLeftInMagazineToWarn;

    [Header("Shooting Style")]
    public ShootingStyle ShootingStyle;
    [Space(8)]
    public ShotgunStats ShotgunStats;
    
    [Space(4)]
    public SimpleShotStats SimpleShotStats;
   

    [Header("Shooting Rythm")]
    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;
    [Space(8)]
    public BurstStats BurstStats;
    
    [Space(4)]
    public RampUpStats RampUpStats;
   

    [Header("ADS and Scopes")]
    public AimAndScopeStats AimingAndScopeStats;
    

    [Header("Recoil")]
    public RecoilStats HipfireRecoilStats;
    public RecoilStats AimingRecoilStats;


    [Header("Kickback")]
    public KickbackStats KickbackStats;
    

    public bool CanBreakThings;
    // add special spread while ADS
}

[Serializable]
public struct SimpleShotStats
{
    public float MaxSpread;
    public float SpreadAccumulatedPerShot;
    public float SpreadRegulationTime;
}

[Serializable]
public struct ShotgunStats
{
    public ushort PelletsDamage;
    public ushort PelletsCount;
    public float PelletsSpreadAngle;
    public float PelletsRange;
}

[Serializable]
public struct BurstStats
{
    public float CooldownBetweenShotsOfBurst;
    public ushort BulletsPerBurst;
}


[Serializable]
public struct RampUpStats
{
    public float RampUpMaxCooldownBetweenShots;
    public float RampUpMinCooldownBetweenShots;
    public float RampUpCooldownMultiplierPerShot;
    public float RampUpCooldownRegulationMultiplier;
}

[Serializable]
public struct AimAndScopeStats
{
    public float AimingCameraMovement; //perhaps just a cameraMove instead
    [Range(1, 5)][Tooltip("Set to 1 for no Scope")] public float ScopeMagnification;
    public float TimeToADS;
    public float TimeToUnADS;
}

[Serializable]
public struct KickbackStats
{
    public float WeaponKickBackPerShot;
    public float WeaponKickBackRegulationTime;
}

[Serializable]
public struct RecoilStats
{
    [Tooltip("Upward recoil")] public float RecoilForceX;
    [Tooltip("Sideway recoil")] public float RecoilForceY;
    [Tooltip("Camera rotation on side (somewhat screen shake)")] public float RecoilForceZ;
    public float RecoilRegulationSpeed;
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
    Charge
}

public enum BulletTravelType : byte
{
    Hitscan,
    TravelTime,
}

public enum BulletTravelTypeBehaviour : byte
{
    BulletDrop,
    NoDrop,
}

public enum BulletType : byte
{
    Classic,
    Explosive,
    ThroughWalls,
    BounceOnWalls,
}

public enum WeaponClass
{
    Primary,
    Secondary,
    Melee,
    // add duel weapon?
}

public enum Effect
{
    Fire,
    Freeze,
    Poison,
    Slowness,
    Obscurity,

}

// some InfernoDragon Style weapons
// for weapons that can do several things according to sth simply do some shenangans to switch the weaponStats