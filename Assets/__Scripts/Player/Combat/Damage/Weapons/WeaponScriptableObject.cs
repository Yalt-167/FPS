using System;
using System.Collections;
using System.Collections.Generic;

using Unity.Netcode;

using UnityEngine;

using MyEditorUtilities;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/Weapon")]
public sealed class WeaponScriptableObject : ScriptableObject, INetworkSerializable, IEquatable<WeaponScriptableObject>
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

    [Space(4)]

    [SerializeFieldIfMatchConstant(nameof(ShootingStyle), ShootingStyle.Single)] public SimpleShotStats SimpleShotStats;
    [SerializeFieldIfMatchConstant(nameof(ShootingStyle), ShootingStyle.Single)] public SimpleShotStats AimingSimpleShotStats;

    [SerializeFieldIfMatchConstant(nameof(ShootingStyle), ShootingStyle.Shotgun)] public ShotgunStats ShotgunStats;


    [Header("Shooting Rythm")]
    public ShootingRythm ShootingRythm;
    public float CooldownBetweenShots;

    [Space(4)]

    [SerializeFieldIfMatchConstant(nameof(ShootingRythm), ShootingRythm.Burst)] public BurstStats BurstStats;

    [SerializeFieldIfMatchConstant(nameof(ShootingRythm), ShootingRythm.RampUp)] public RampUpStats RampUpStats;

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
    [Header("Switch")]
    public float PulloutTime;

    [Space(16)]
    public bool CanBreakThings;

    [Space(16)]
    public Effects EffectsInflicted;

    [Space(16)]
    public WeaponSounds Sounds; // ? chjange to interface at some point to incudes more sounds?

    public bool Equals(WeaponScriptableObject other)
    {
        return other == this;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter // ffs
    {
        Damage.NetworkSerialize(serializer);

        serializer.SerializeValue(ref WeaponClass);

        # region Bullet Travel Settings

        serializer.SerializeValue(ref IsHitscan);

        HitscanBulletSettings.NetworkSerialize(serializer);

        TravelTimeBulletSettings.NetworkSerialize(serializer);

        #endregion

        #region Magazine

        serializer.SerializeValue(ref MagazineSize);
        serializer.SerializeValue(ref NeedReload);
        serializer.SerializeValue(ref ReloadSpeed);
        serializer.SerializeValue(ref TimeToReloadOneRound);
        serializer.SerializeValue(ref AmmoLeftInMagazineToWarn);

        #endregion


        #region Shooting Style

        serializer.SerializeValue(ref ShootingStyle);

        SimpleShotStats.NetworkSerialize(serializer);
        AimingSimpleShotStats.NetworkSerialize(serializer);

        ShotgunStats.NetworkSerialize(serializer);


        #endregion

        #region Shooting Rythm


        serializer.SerializeValue(ref ShootingRythm);
        serializer.SerializeValue(ref CooldownBetweenShots);

        BurstStats.NetworkSerialize(serializer);
        RampUpStats.NetworkSerialize(serializer);
        ChargeStats.NetworkSerialize(serializer);

        #endregion


        AimingAndScopeStats.NetworkSerialize(serializer);

        HipfireRecoilStats.NetworkSerialize(serializer);
        AimingRecoilStats.NetworkSerialize(serializer);

        KickbackStats.NetworkSerialize(serializer);


        serializer.SerializeValue(ref PulloutTime);


        serializer.SerializeValue(ref CanBreakThings);
        serializer.SerializeValue(ref EffectsInflicted);

        Sounds.NetworkSerialize(serializer);
    }
}

[Serializable]
public struct WeaponSounds : INetworkSerializable
{
    public AudioClip ShootingSound;
    public AudioClip ReloadSound;
    public AudioClip NearEmptyMagazineShootSound;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        throw new NotImplementedException();
    }
}