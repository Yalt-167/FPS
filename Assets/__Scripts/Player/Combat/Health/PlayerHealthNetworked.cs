using System;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using Unity.Netcode;

using GameManagement;


[DefaultExecutionOrder(-4)]
[Serializable]
public sealed class PlayerHealthNetworked : NetworkBehaviour
{
    [field: SerializeField] public PlayerHealthData HealthData { get; private set; }

    public float CurrentHealth { get; private set; }
    private int currentShieldSlots;
    private float currentShieldSlotRemainingPower;
    private float TotalShield => (currentShieldSlots - 1) * HealthData.ShieldSlotHealth + currentShieldSlotRemainingPower;

    public Shield Shield { get; private set; }
    
    public bool Alive => CurrentHealth > 0;
    private PlayerFrame ownerFrame;
    public ushort TeamNumber => ownerFrame == null ? (ushort)0 : ownerFrame.TeamNumber;
    private bool respawning;
    private void Awake()
    {
        ownerFrame = GetComponent<PlayerFrame>();
        ResetHealth();
        PassiveRegen();
    }

    private void PassiveRegen()
    {
        StartCoroutine(PassiveHealthRegen());
        //StartCoroutine(PassiveShieldRegen());
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
            RegenerateShield(HealthData.PassiveShieldRegenerationAmount, canReviveCell: false);
        }
    }

    private void Update()
    {
        if (!Alive && !respawning)
        {
            respawning = true;
            RequestRespawnServerRpc();
            return;
        }

        MyDebug.DebugOSD.Display("CurrentHealth", CurrentHealth);
        MyDebug.DebugOSD.Display("TotalShield", TotalShield);
    }

    private void ResetHealth()
    {
        CurrentHealth = HealthData.MaxHealth;
        currentShieldSlots = HealthData.MaxShieldSlots;
        currentShieldSlotRemainingPower = HealthData.ShieldSlotHealth;

        Shield = new(HealthData.MaxShieldSlots, HealthData.ShieldSlotHealth);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TakeDamageClientRpc(ushort damage, BodyParts bodyPartShot, bool ignoreShield, ulong attackerNetworkObjectID) // add shield only modifier
    {
        // send the info about wether shielded here (bool)Shield
        if (IsOwner)
        {
            SendDamageLogInfosServerRpc(MapBodyPartToTargetType(bodyPartShot, Shield.HasShield), damage, attackerNetworkObjectID);
        }

        if (damage <= 0) { return; }


        if (!ignoreShield && Shield.HasShield)
        {
            damage = Shield.TakeDamage(damage);
        }

        if (damage <= 0) { return; }

        CurrentHealth -= damage;
    }

    [UnityEditor.MenuItem("Developer/DamageLocalPlayer")]
    public static void DamageLocalPlayer()
    {
        var damage = 10;

        MyDebug.DebugOSD.Display("Damage before hitting shield", damage);

        if (PlayerFrame.LocalPlayer.Health.Shield.HasShield)
        {
            damage = PlayerFrame.LocalPlayer.Health.Shield.TakeDamage((ushort)damage);
        }

        MyDebug.DebugOSD.Display("Damage after hitting shield", damage);

        if (damage <= 0) { return; }

        PlayerFrame.LocalPlayer.Health.CurrentHealth -= damage;
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
        if (healProficiency ==  0) { return; }

        Shield.Heal(healProficiency, canReviveCell);
    }

    [Rpc(SendTo.Server)]
    public void SendDamageLogInfosServerRpc(TargetType targetType, ushort damage, ulong attackerNetworkObjectID)
    {
        DisplayDamageLogsClientRpc(targetType, damage, attackerNetworkObjectID);
    }

    [Rpc(SendTo.ClientsAndHost)] // called on all client on this INSTANCE of this script
    public void DisplayDamageLogsClientRpc(TargetType targetType, ushort damageDealt, ulong attackerNetworkObjectID)
    {
        if (IsOwner) { return; }

        Game.RetrievePlayerFromNetworkObjectID(attackerNetworkObjectID).WeaponHandler.SpawnDamageLog(targetType, damageDealt);
    }


    private TargetType MapBodyPartToTargetType(BodyParts bodyPart, bool hadShield)
    {
        return (TargetType)(bodyPart switch
        {
            BodyParts.HEAD => (int)TargetType.HEAD,
            BodyParts.BODY => (int)TargetType.BODY,
            BodyParts.LEGS => (int)TargetType.LEGS,
            _ => throw new NotImplementedException(),
        } - (hadShield ? 3 : 0));
    }

    [Rpc(SendTo.Server)]
    private void RequestRespawnServerRpc()
    {
        RespawnClientRpc(Game.Manager.GetSpawnPosition(TeamNumber));
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void RespawnClientRpc(Vector3 spawnPosition)
    {
        respawning = false;
        ResetHealth();
        transform.position = spawnPosition;
    }
}