using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "ScriptableObjects/WeaponStats")]
public class WeaponStats : ScriptableObject
{
    public ushort Damage;
    public WeaponClass WeaponClass;

    [Header("Bullet Travel Settings")]
    public bool IsHitscan;
    public HitscanBulletSettings HitscanBulletSettings;
    public TravelTimeBulletSettings TravelTimeBulletSettings;


    [Header("Magazine")]
    public ushort MagazineSize;
    public bool NeedReload;
    public float ReloadSpeed; // some that can reload one per one
    public ushort AmmoLeftInMagazineToWarn;

    [Header("Shooting Style")]
    public ShootingStyle ShootingStyle;
    [Space(8)]
    public SimpleShotStats SimpleShotStats;
    public SimpleShotStats AimingSimpleShotStats;
    
    [Space(4)]
    public ShotgunStats ShotgunStats;
    //public ShotgunStats AimingShotgunStats;
   

    [Header("Shooting Rythm")]
    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;
    [Space(8)]
    public BurstStats BurstStats;

    [Space(4)]
    public RampUpStats RampUpStats;

    [Space(4)]
    public ChargeStats ChargeStats;


    [Header("ADS and Scopes")]
    public AimAndScopeStats AimingAndScopeStats;
    

    [Header("Recoil")]
    public RecoilStats HipfireRecoilStats;
    public RecoilStats AimingRecoilStats;


    [Header("Kickback")]
    public KickbackStats KickbackStats;
    

    public bool CanBreakThings;
}

#region Shooting Style

public enum ShootingStyle : byte
{
    Single,
    Shotgun,
}

[Serializable]
public struct SimpleShotStats
{
    public float MaxSpread;
    public float SpreadAngleAddedPerShot;
    public float SpreadRegulationSpeed;
}

[Serializable]
public struct ShotgunStats
{
    public ushort PelletsDamage;
    public ushort PelletsCount;
    public float PelletsSpreadAngle;
    public float AimingPelletsSpreadAngle;
    public float PelletsRange;
}

#endregion 

#region Shooting Rythm

public enum ShootingRythm : byte
{
    Single,
    Burst,
    RampUp,
    Charge
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
public struct ChargeStats
{
    public float ChargeDuration;
    public ushort AmmoConsumedByFullyChargedShot;
    [Range(.1f, 1f)] public float MinChargeRatioToShoot;
}

#endregion

#region Miscellaneous

public enum WeaponClass
{
    Primary,
    Secondary,
    Melee,
    // add duel weapon?
}

[Serializable]
public struct AimAndScopeStats
{
    public float AimingFOV; //perhaps just a cameraMove instead
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

public enum Effect
{
    Fire,
    Freeze,
    Bleeding,
    Poison,
    Slowness,
    Obscurity,
    Fading
}

#endregion

#region Bullet Settings

#region Hitscan Bullet Settings

[Serializable]
public struct HitscanBulletSettings
{
    public bool PierceThroughPlayers;
    public HitscanBulletActionOnHitWall ActionOnHitWall;
    
    public int ExplosionRadius;
    public int ExplosionDamage;
}

public interface IHitscanBulletEffectSettings { }

[Serializable]
public struct BouncingHitscanBulletsSettings : IHitscanBulletEffectSettings
{
    public int BouncesAmount;
}

// redo hit sequence when not piercing player as we should account for walls and bullet effect still
public enum HitscanBulletActionOnHitWall : byte
{
    Classic, // exit
    Explosive, // do exit for same reason 
    ThroughWalls, // don t exit
    BounceOnWalls, // do exit cah those caught so far are prolly ont in the bounce too so that would be unfair
}

#endregion

#region Travel Time Bullet Settings

[Serializable]
public struct TravelTimeBulletSettings
{
    public GameObject BulletPrefab;
    public float BulletDrop;
    public float BulletSpeed;
}

#endregion

#endregion

// some InfernoDragon Style weapons
// for weapons that can do several things according to sth simply do some shenangans to switch the weaponStats

// add a second healthbar beneath the actual one for a lerping one that slowly goes toward your health
//just for aesthetic
// add a report bug key ingame