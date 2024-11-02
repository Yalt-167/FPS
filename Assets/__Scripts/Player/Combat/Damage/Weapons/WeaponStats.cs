using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;


[Serializable]
[Obsolete("Only there for reference in case I break sth in WeaponScriptableObject (which is the new class to use")]
public struct WeaponStats
{
    [Tooltip("The damage dealt by the shot (Will be overriden by PelletDamage) if the weapon is a shotgun")] public DamageDealt Damage;
    public WeaponClass WeaponClass;

    [Header("Bullet Travel Settings")]
    public bool IsHitscan;
    [Tooltip("Define how the bullet should act (Only considered if the IsHitscan param above is true)")] public HitscanBulletSettings HitscanBulletSettings;
    [Tooltip("Define how the bullet should act (Only considered if the IsHitscan param above is false)")] public TravelTimeBulletSettings TravelTimeBulletSettings;


    [Header("Magazine")]
    public ushort MagazineSize;
    public bool NeedReload;
    public float ReloadSpeed;
    [Tooltip("If the weapon can reload one round by one round else leave it at 0")] public float TimeToReloadOneRound; // some that can reload one per one -> only consider when non-zero
    public ushort AmmoLeftInMagazineToWarn;

    [Header("Shooting Style")]
    [Tooltip("Wether it shoots simples shots of shotgun shots")] public ShootingStyle ShootingStyle;
    [Space(8)]
    [Tooltip("Stats of the simple shots (Only considered when the ShootingStyle selected is Simple)")] public SimpleShotStats SimpleShotStats;
    [Tooltip("Stats of the simple shots (Only considered when the ShootingStyle selected is Simple)")] public SimpleShotStats AimingSimpleShotStats;
    
    [Space(4)]
    [Tooltip("Stats of the shotgun shots (Only considered when the ShootingStyle selected is Shotgun)")] public ShotgunStats ShotgunStats;
   

    [Header("Shooting Rythm")]
    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;
    [Space(8)]
    [Tooltip("Only considered when the ShootingRythm selected is Burst")] public BurstStats BurstStats;

    [Space(4)]
    [Tooltip("Only considered when the ShootingRythm selected is RampUp")] public RampUpStats RampUpStats;

    [Space(4)]
    [Tooltip("Only considered when the ShootingRythm selected is Charge")] public ChargeStats ChargeStats;


    //[Header("ADS and Scopes")]
    //public AimAndScopeStats AimingAndScopeStats;
    

    //[Header("Recoil")]
    //public WeaponRecoilStats HipfireRecoilStats;
    //public WeaponRecoilStats AimingRecoilStats;


    //[Header("Kickback")]
    //public KickbackStats KickbackStats;
    

    public bool CanBreakThings;
    public Effects EffectsInflicted;
}


[Serializable]
public struct DamageDealt
{
    public ushort HeadshotDamage;
    public ushort BodyshotDamage;
    public ushort LegshotDamage;

    public DamageDealt(ushort high, ushort base_, ushort low_)
    {
        HeadshotDamage = high;
        BodyshotDamage = base_;
        LegshotDamage = low_;
    }

    public DamageDealt(DamageDealt previousStruct, float coefficient)
    {
        HeadshotDamage = (ushort)(previousStruct.HeadshotDamage * coefficient);
        BodyshotDamage = (ushort)(previousStruct.BodyshotDamage * coefficient);
        LegshotDamage = (ushort)(previousStruct.LegshotDamage * coefficient);
    }

    public static DamageDealt operator *(DamageDealt relevantStruct, float coefficient)
    {
        return new(relevantStruct, coefficient);
    }

    public static DamageDealt operator /(DamageDealt relevantStruct, float coefficient)
    {
        return new(relevantStruct, 1 / coefficient);
    }

    public readonly ushort this[BodyParts bodyPart]
        {
            get
            {
                return bodyPart switch
                {
                    BodyParts.HEAD => HeadshotDamage,
                    BodyParts.BODY => BodyshotDamage,
                    BodyParts.LEGS => LegshotDamage,
                    _ => BodyshotDamage,
                };
            }
        }
}

#region Shooting Style

public enum ShootingStyle : byte
{
    Single,
    Shotgun,
}

[Serializable]
public struct SimpleShotStats : INetworkSerializable
{
    public float MaxSpread;
    public float SpreadAngleAddedPerShot;
    public float SpreadRegulationSpeed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MaxSpread);
        serializer.SerializeValue(ref SpreadAngleAddedPerShot);
        serializer.SerializeValue(ref SpreadRegulationSpeed);
    }
}

[Serializable]
public struct ShotgunStats : INetworkSerializable
{
    public ushort PelletsCount;
    public float HipfirePelletsSpreadAngle;
    public float AimingPelletsSpreadAngle;
    public float PelletsRange;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PelletsCount);
        serializer.SerializeValue(ref HipfirePelletsSpreadAngle);
        serializer.SerializeValue(ref AimingPelletsSpreadAngle);
        serializer.SerializeValue(ref PelletsRange);
    }
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
    public float RampUpCooldownMultiplierPerShot; // why tf are these multipliers???
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

#region Bullet Settings

#region Hitscan Bullet Settings

[Serializable]
public struct HitscanBulletSettings
{
    public bool PierceThroughPlayers;
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
    public ushort ExplosionDamage;
    // should it add up ? (hitting directly + explosion) -> yes
    // should it account for different part of the body (? avoid stacking ?)
}

// redo hit sequence when not piercing player as we should account for walls and bullet effect still
public enum HitscanBulletActionOnHitWall : byte
{
    Classic, // do exit
    Explosive, // do exit
    ThroughWalls, // don t exit
    BounceOnWalls, // do exit
}

#endregion

#region Travel Time Bullet Settings

[Serializable]
public struct TravelTimeBulletSettings
{
    public GameObject BulletPrefab;
    public float BulletDrop;
    public float BulletSpeed;
    public bool ChargeAffectsBulletsSpeed;
    public ProjectileBehaviourOnHitWall OnHitWallBehaviour;
    public ProjectileBehaviourOnHitWallParams OnHitWallBehaviourParams;
    public ProjectileBehaviourOnHitPlayer OnHitPlayerBehaviour;
    public ProjectileBehaviourOnHitPlayerParams OnHitPlayerBehaviourParams;
}

# region On Hit Wall Behaviour

public enum ProjectileBehaviourOnHitWall : byte
{
    Stop,
    Pierce,
    Bounce,
    Explode,
}

[Serializable]
public struct ProjectileBehaviourOnHitWallParams // main structs that groups all the small param structs
{
    public ProjectileWallPierceParams ProjectileWallPierceParams;
    public ProjectileWallBounceParams ProjectileWallBounceParams;
    public ProjectileWallExplodeParams ProjectileWallExplodeParams;
}

public interface IProjectileBehaviourOnHitWallParam { } // type for delegate

[Serializable]
public struct ProjectileWallPierceParams : IProjectileBehaviourOnHitWallParam
{
    public ushort MaxWallsToPierce;
}

[Serializable]
public struct ProjectileWallBounceParams : IProjectileBehaviourOnHitWallParam
{
    public ushort MaxBounces;
}

[Serializable]
public struct ProjectileWallExplodeParams : IProjectileBehaviourOnHitWallParam
{
    public ushort ExplosionRadius;
    public ushort ExplosionDamage;
}

# endregion

#region On Hit Player Behviour

public enum ProjectileBehaviourOnHitPlayer : byte
{
    Stop,
    Pierce,
    Explode,
}

[Serializable]
public struct ProjectileBehaviourOnHitPlayerParams
{
    public ProjectilePlayerPierceParams ProjectilePlayerPierceParams;
    public ProjectilePlayerExplodeParams ProjectilePlayerExplodeParams;
}

public interface IProjectileBehaviourOnHitPlayerParam { } // type for delegate

[Serializable]
public struct ProjectilePlayerPierceParams : IProjectileBehaviourOnHitPlayerParam
{
    public ushort MaxPlayersToPierce;
}

[Serializable]
public struct ProjectilePlayerExplodeParams : IProjectileBehaviourOnHitPlayerParam
{
    public ushort ExplosionRadius;
    public ushort ExplosionDamage;
}

#endregion


#endregion

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
public struct KickbackStats : INetworkSerializable
{
    public float WeaponKickBackPerShot;
    public float WeaponKickBackRegulationTime;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref WeaponKickBackPerShot);
        serializer.SerializeValue(ref WeaponKickBackRegulationTime);
    }
}

[Serializable]
public struct WeaponRecoilStats : INetworkSerializable
{
    [Tooltip("Upward recoil (Muzzle Climb)")] public float RecoilForceX;
    [Tooltip("Sideway recoil")] public float RecoilForceY;
    [Tooltip("Camera rotation on side (somewhat screen shake) (Should stay really low)")] public float RecoilForceZ;
    public float RecoilRegulationSpeed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref RecoilForceX);
        serializer.SerializeValue(ref RecoilForceY);
        serializer.SerializeValue(ref RecoilForceZ);
        serializer.SerializeValue(ref RecoilRegulationSpeed);
    }
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
    Shock, // damage nearby players (chaining)
    // ex with a range of 3:
    // if u get shot and a teammate is less than 3units from u he gets shocked too (for less dmg) and if another teammate is less than 3units from him (not necessarily you) it chains too (the more depth of chaining the less dmg
    Charm, // damage reduced on the one who inflicted you this
    Curse, // damage boosted on the cursed 


}

public interface IEffectProficiency { } // interface for polymorphism on the effect data

// have an override for * (for chargedWaps and influence on the proficiency of the effect)



#endregion

// some InfernoDragon Style weapons
// for weapons that can do several things according to sth simply do some shenanigans to switch the weaponStats

// add a second healthbar beneath the actual one for a lerping one that slowly goes toward your health
//just for aesthetic
// add a report bug key ingame



// keep the weaponHandler but no longer as a NetworkBehaviour just it holds the weapon data
// have the weapon behaviour on the root of the weapon -> when  switch it enables the weapon associated
// which subscribes its script to the weaponHandler which uses them