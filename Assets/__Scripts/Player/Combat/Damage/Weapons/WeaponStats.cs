using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "ScriptableObjects/WeaponStats")]
public class WeaponStats : ScriptableObject
{
    public ushort Damage;

    [Header("Magazine")]
    public ushort MagazineSize;
    // public bool NeedReload
    public float ReloadSpeed; // some that can reload one per one
    public ushort AmmoLeftInMagazineToWarn;

    [Header("Shooting Style")]
    public ShootingStyle ShootingStyle;
    [Space(8)]
    public ShotgunStats_ ShotgunStats;
    [Serializable]
    public struct ShotgunStats_
    {
        public ushort PelletsDamage;
        public ushort PelletsCount;
        public float PelletsSpreadAngle;
        public float PelletsRange;
    }
    [Space(4)]
    public SimpleShotStats_ SimpleShotStats;
    [Serializable]
    public struct SimpleShotStats_
    {
        public float MaxSpread;
        public float SpreadAccumulatedPerShot;
        public float SpreadRegulationTime;
    }

    [Header("Shooting Rythm")]
    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;
    [Space(8)]
    public BurstStats_ BurstStats;
    [Serializable]
    public struct BurstStats_
    {
        public float CooldownBetweenShotsOfBurst;
        public ushort BulletsPerBurst;
    }
    [Space(4)]
    public RampUpStats_ RampUpStats;
    [Serializable]
    public struct RampUpStats_
    {
        public float RampUpMaxCooldownBetweenShots;
        public float RampUpMinCooldownBetweenShots;
        public float RampUpCooldownMultiplierPerShot;
        public float RampUpCooldownRegulationMultiplier;
    }

    [Header("ADS and Scopes")]
    public float ADS_FOV; //perhaps just a cameraMove instead
    [Range(1, 5)][Tooltip("Set to 1 for no Scope")] public float ScopeMagnification;
    public float TimeToADS;
    public float TimeToUnADS;

    [Header("Recoild")]
    public float RecoilForce;
    public float RecoilRegulationTime;

    [Header("kickback")]
    public float WeaponKickBackPerShot;
    public float WeaponKickBackRegulationTime;
    public float FarthestPointBehindInitialPosition;

    public bool CanBreakThings;
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
// some InfernoDragon Style weapons
// for weapons that can do several things according to sth simply do some shenangans to switch the weaponStats