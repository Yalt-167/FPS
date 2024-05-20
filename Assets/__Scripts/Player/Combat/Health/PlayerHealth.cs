using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using UnityEditor;
using Unity.VisualScripting;

[DefaultExecutionOrder(-4)]
public class PlayerHealth : NetworkBehaviour
{
    [field: SerializeField] public PlayerHealthData HealthData { get; private set; }

    public float CurrentHealth { get; private set; }
    private int currentShieldSlots;
    private float currentShieldSlotRemainingPower;
    private float TotalShield => (currentShieldSlots - 1) * HealthData.ShieldSlotHealth + currentShieldSlotRemainingPower;

    public Shield Shield { get; private set; }
    
    private bool Alive => CurrentHealth > 0;

    private void Awake()
    {
        ResetHealth();
        PassiveRegen();
    }

    private void PassiveRegen()
    {
        StartCoroutine(PassiveHealthRegen());
        StartCoroutine(PassiveShieldRegen());
    }

    private IEnumerator PassiveHealthRegen() // remake those for them to start regen only after a bit while not taking damage
    {
        float lastHealTime;
        for (; ; )
        {
            lastHealTime = Time.time;
            yield return new WaitUntil(() => lastHealTime + HealthData.PassiveHealthRegenerationRate < Time.time && CurrentHealth < HealthData.MaxHealth);
            RegenerateHealth(HealthData.PassiveHealthRegenerationAmount);
        }
    }

    private IEnumerator PassiveShieldRegen() // remake those for them to start regen only after a bit while not taking damage
    {
        float lastHealTime;
        for (; ; )
        {
            lastHealTime = Time.time;
            yield return new WaitUntil(() => lastHealTime + HealthData.PassiveShieldRegenerationRate < Time.time && CurrentHealth == HealthData.MaxHealth);
            RegenerateShield(HealthData.PassiveShieldRegenerationAmount, false);
        }
    }

    private void Update()
    {
        if (!Alive)
        {
            ResetHealth();
            transform.position = Vector3.up * 100;
            return;
        }
    }

    private void ResetHealth()
    {
        CurrentHealth = HealthData.MaxHealth;
        currentShieldSlots = HealthData.MaxShieldSlots;
        currentShieldSlotRemainingPower = HealthData.ShieldSlotHealth;

        Shield = new(HealthData.MaxShieldSlots, HealthData.ShieldSlotHealth);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TakeDamageClientRpc(ushort damage, BodyParts bodyPartShot, bool ignoreShield, ulong attackerNetworkID) // add shield only modifier ?
    {
        // send the info about wether shielded here (bool)Shield
        if (IsOwner) { SendDamageLogInfosServerRpc(MapBodyPartToTargetType(bodyPartShot, Shield), damage, attackerNetworkID); }

        if (damage <= 0) { return; }


        if (!ignoreShield && Shield)
        {
            damage = Shield.TakeDamage(damage);
        }

        if (damage <= 0) { return; }

        CurrentHealth -= damage;
    }


    public void RawHeal(ushort healProficiency)
    {
        RegenerateShield(RegenerateHealth(healProficiency), true);
    }

    public ushort RegenerateHealth(ushort healProficiency)
    {
        CurrentHealth += healProficiency;
        if (CurrentHealth < HealthData.MaxHealth) { return 0; }

        var excessHeal = CurrentHealth - HealthData.MaxHealth;
        CurrentHealth = HealthData.MaxHealth;
        return (ushort)excessHeal;
   
    }

    public void RegenerateShield(ushort healProficiency, bool canReviveCell)
    {
        Shield.Heal(healProficiency, canReviveCell);
    }

    [Rpc(SendTo.Server)]
    public void SendDamageLogInfosServerRpc(TargetType targetType, ushort damage, ulong attackerNetworkID)
    {
        DisplayDamageLogsClientRPC(targetType, damage, attackerNetworkID);
    }

    [Rpc(SendTo.ClientsAndHost)] // called on all client on this INSTANCE of this script
    public void DisplayDamageLogsClientRPC(TargetType targetType, ushort damageDealt, ulong attackerNetworkID)
    {
        if (IsOwner) { return; }

        Game.Manager.GetNetworkedWeaponHandlerFromNetworkObjectID(attackerNetworkID).SpawnDamageLog(targetType, damageDealt);
    }


    private TargetType MapBodyPartToTargetType(BodyParts bodyPart, bool hadShield)
    {
        return bodyPart switch
        {
            BodyParts.HEAD => hadShield ? TargetType.HEAD_SHIELDED : TargetType.HEAD,
            BodyParts.BODY => hadShield ? TargetType.BODY_SHIELDED : TargetType.BODY,
            BodyParts.LEGS => hadShield ? TargetType.LEGS_SHIELDED : TargetType.LEGS,
            _ => throw new NotImplementedException(),
        };
    }
}