using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerHealthData", menuName = "ScriptableObjects/PlayerHealthData")]
public sealed class PlayerHealthData : ScriptableObject
{
    public ushort MaxHealth;
    public ushort PassiveHealthRegenerationAmount;
    public ushort HealthRegeneratedOnKill;
    public float PassiveHealthRegenerationRate;
    public float TimeBeforeInitiatingHealthRegeneration;

    public ushort MaxShieldSlots;
    public ushort ShieldSlotHealth;
    public ushort PassiveShieldRegenerationAmount;
    public float PassiveShieldRegenerationRate;
    public ushort ShieldRegeneratedOnKill;
    public float TimeBeforeInitiatingShieldRegeneration;

    public ushort MaxBonusShieldSlots;
}

// the player has several gauges of shield

// this shield regens within the current gauge but not above (unless he does a kill and has a Leech effect)

// the health regens for some characters