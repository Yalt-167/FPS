using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ScriptableObjects/PlayerHealthData")]
public class PlayerHealthData : ScriptableObject
{
    public int MaxHealth;
    public int PassiveHealthRegenerationAmount;
    public int HealthRegeneratedOnKill;
    public float PassiveHealthRegenerationRate;
    public float TimeBeforeInitiatingHealthRegeneration;

    public int MaxShieldSlots;
    public int ShieldSlotHealth;
    public int PassiveShieldRegenerationAmount;
    public float PassiveShieldRegenerationRate;
    public int ShieldRegeneratedOnKill;
    public float TimeBeforeInitiatingShieldRegeneration;

    public int MaxBonusShieldSlots;
}

// the player has several gauges of shield

// this shield regens within the current gauge but not above (unless he does a kill and has a Leech effect)

// the health regens for some characters