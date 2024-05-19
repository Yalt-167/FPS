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
    public Effects EffectsInflicted;
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

public enum Effects
{
    None,
    Fire, // depletes shield & health (stacking until a certain threshold
    Freeze, // slows + increased damage when already frozen
    Bleeding, // depletes life
    Poison, // deplete life
            // some character have resistance on one but not both
            // &&
            // some heal may fix one but not both
    Slowness, // slows
    Obscurity, // blindness
    Fading, // reduce your Vital Essence (instant -> not lingering) (not replenishing) (when no more Vital Essence -> dead) (get a new Vital Essence gauge when respawning)
    // ex when shot u lose say 50% of ur Vital Essence even if you heal the next shot will drop that gauge to 0 -> you die next Fading Shot
    Shock, // damage nearby players (chaining) + can make weapon jam ?
    // ex with a range of 3:
    // if u get shot and a teammate is less than 3units from u he gets shocked too (for less dmg) and if another teammate is less than 3units from him (not necessarily you) it chains too (the more depth of chaining the less dmg
    Charm, // damage reduced on the one who inflicted you this
    Curse, // damage boosted on the cursed 


}

public interface IEffectProficiency { } // interface for polymorphism on the effect data
// have an override for * (for chargedWaps and influence on the proficiency of the effect)



#endregion

#region Bullet Settings

#region Hitscan Bullet Settings

[Serializable]
public struct HitscanBulletSettings
{
    public bool PierceThroughPlayers; // account for that
    public HitscanBulletActionOnHitWall ActionOnHitWall;
    public BouncingHitscanBulletsSettings BouncingBulletsSettings;
    public ExplodingHitscanBulletsSettings ExplodingBulletsSettings;
}

public interface IHitscanBulletEffectSettings { }

[Serializable]
public struct BouncingHitscanBulletsSettings : IHitscanBulletEffectSettings
{
    public int BouncesAmount;

    public BouncingHitscanBulletsSettings(int bouncesAmount)
    {
        BouncesAmount = bouncesAmount;
    }


    public static BouncingHitscanBulletsSettings operator --(BouncingHitscanBulletsSettings relevantStruct)
    {
        return new BouncingHitscanBulletsSettings(--relevantStruct.BouncesAmount);
    }
}

[Serializable]
public struct ExplodingHitscanBulletsSettings : IHitscanBulletEffectSettings
{
    public int ExplosionRadius;
    public int ExplosionDamage;
}

// redo hit sequence when not piercing player as we should account for walls and bullet effect still
public enum HitscanBulletActionOnHitWall : byte
{
    Classic, // exit
    Explosive, // do exit for same reason 
    ThroughWalls, // don t exit
    BounceOnWalls, // do exit cah those caught so far are prolly not in the bounce too so that would be unfair
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