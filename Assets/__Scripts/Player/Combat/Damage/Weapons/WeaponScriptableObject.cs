using System;
using System.Collections;
using System.Collections.Generic;

using MyEditorUtilities;

using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/Weapon")]
public sealed class WeaponScriptableObject : ScriptableObject
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

    [Header("Mobility")]

    [Space(8)]
    [Header("ADS and Scopes")]
    public AimAndScopeStats AimingAndScopeStats;

    [Space(8)]
    [Header("Recoil")]
    public WeaponRecoilStats HipfireRecoilStats;
    public WeaponRecoilStats AimingRecoilStats;

    [Space(8)]
    [Header("Kickback")]
    public KickbackStats KickbackStats;

    public bool CanBreakThings;
    public Effects EffectsInflicted;

    [Space(16)]
    public Mesh Model;

    [Space(16)]
    public WeaponSounds Sounds;
}

[Serializable]
public struct WeaponSounds
{
    public AudioClip ShootingSound;
    public AudioClip ReloadSound;
    public AudioClip NearEmptyMagazineShootSound;
}