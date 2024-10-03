using System;
using System.Collections;
using System.Collections.Generic;

using MyEditorUtilities;

using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/Weapon")]
public sealed class WeaponScriptableObject : ScriptableObject
{

    [Header("Global Stats")]
    public WeaponStats Stats;

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