using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Weapon", menuName = "ScriptableObjects/Weapon")]
public class Weapon : ScriptableObject
{
    public WeaponStats Stats;
    public Mesh Model;
    public Sounds Sounds;
}

[Serializable]
public struct Sounds
{
    public AudioClip ShootingSound;
    public AudioClip ReloadSound;
    public AudioClip NearEmptyMagazineShootSound;
}