using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "WeaponStats", menuName = "ScriptableObjects/WeaponStats")]
public class WeaponStats : ScriptableObject
{
    public ushort Damage;

    public ushort MagazineSize;
    // public bool NeedReload
    public float ReloadSpeed; // some that can reload one per one
    public ushort AmmoLeftInMagazineToWarn;


    public ShootingStyle ShootingStyle;
    public ShotgunStats_ ShotgunStats;
    [Serializable]
    public struct ShotgunStats_
    {
        public ushort PelletsDamage;
        public ushort PelletsCount;
        public float PelletsSpread;
        public float PelletsRange;
    }
    public SimpleShotStats_ SimpleShotStats;
    [Serializable]
    public struct SimpleShotStats_
    {
        public float MaxSpread;
        public float SpreadAccumulatedPerShot;
        public float SpreadRegulationTime;
    }
    


    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;
    public BurstStats_ BurstStats;
    [Serializable]
    public struct BurstStats_
    {
        public float CooldownBetweenShotsOfBurst;
        public ushort BulletsPerBurst;
    }
    //RampUp

    public RampUpStats_ RampUpStats;
    [Serializable]
    public struct RampUpStats_
    {
        public float RampUpMaxCooldownBetweenShots;
        public float RampUpMinCooldownBetweenShots;
        public float RampUpCooldownMultiplierPerShot;
        public float RampUpCooldownRegulationMultiplier;
    }

    public float ADS_FOV; //perhaps just a cameraMove instead
    [Range(1, 5)][Tooltip("Set to 1 for no Scope")] public float ScopeMagnification;
    public float TimeToADS;
    public float TimeToUnADS;

    public float RecoilForce;
    public float RecoilRegulationTime;

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