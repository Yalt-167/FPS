using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/Weapon")]
public sealed class Weapon : ScriptableObject
{
    public WeaponStats Stats;

    [Space(12)]
    public Mesh Model;

    [Space(12)]
    public WeaponSounds Sounds;
}

[Serializable]
public struct WeaponSounds
{
    public AudioClip ShootingSound;
    public AudioClip ReloadSound;
    public AudioClip NearEmptyMagazineShootSound;
}