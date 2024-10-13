using System;
using System.Collections;
using System.Collections.Generic;

using MyEditorUtilities;

using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/Weapon")]
public sealed class WeaponScriptableObject : ScriptableObject
{

    [Tooltip("Will be overriden by PelletDamage if the weapon is a shotgun")] public DamageDealt Damage;
    public WeaponClass WeaponClass;

    [Header("Bullet Travel Settings")]
    public bool IsHitscan;
    [SerializeFieldIf(nameof(IsHitscan))] public HitscanBulletSettings HitscanBulletSettings;
    [SerializeFieldIf(nameof(IsHitscan), invertCondition: true)] public TravelTimeBulletSettings TravelTimeBulletSettings;


    [Header("Magazine")]
    public ushort MagazineSize;
    public bool NeedReload;
    public float ReloadSpeed;
    [Tooltip("0 to ignore")] public float TimeToReloadOneRound; // some that can reload one per one
    public ushort AmmoLeftInMagazineToWarn;

    [Header("Shooting Style")]
    public ShootingStyle ShootingStyle;
    [Space(16)]

    [SerializeFieldIfMatchConstant(nameof(ShootingStyle), ShootingStyle.Single)] public SimpleShotStats SimpleShotStats;
    [SerializeFieldIfMatchConstant(nameof(ShootingStyle), ShootingStyle.Single)]  public SimpleShotStats AimingSimpleShotStats;
    
    [Space(4)]
    [SerializeFieldIfMatchConstant(nameof(ShootingStyle), ShootingStyle.Shotgun)] public ShotgunStats ShotgunStats;


    [Header("Shooting Rythm")]
    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;
    [Space(16)]
    [SerializeFieldIfMatchConstant(nameof(ShootingRythm), ShootingRythm.Burst)] public BurstStats BurstStats;

    [Space(4)]
    [SerializeFieldIfMatchConstant(nameof(ShootingRythm), ShootingRythm.RampUp)] public RampUpStats RampUpStats;

    [Space(4)]
    [SerializeFieldIfMatchConstant(nameof(ShootingRythm), ShootingRythm.Charge)] public ChargeStats ChargeStats;

    [Space(16)]
    [Header("ADS and Scopes")]
    public AimAndScopeStats AimingAndScopeStats;

    [Space(16)]
    [Header("Recoil")]
    public WeaponRecoilStats HipfireRecoilStats;
    public WeaponRecoilStats AimingRecoilStats;

    [Space(16)]
    [Header("Kickback")]
    public KickbackStats KickbackStats;

    [Space(16)]
    public bool CanBreakThings;

    [Space(16)]
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