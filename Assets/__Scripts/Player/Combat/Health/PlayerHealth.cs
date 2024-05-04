using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;

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

        //if (Input.GetKeyDown(KeyCode.L)) { TakeDamageClientRpc(3, false); }
    }

    private void ResetHealth()
    {
        CurrentHealth = HealthData.MaxHealth;
        currentShieldSlots = HealthData.MaxShieldSlots;
        currentShieldSlotRemainingPower = HealthData.ShieldSlotHealth;

        Shield = new(HealthData.MaxShieldSlots, HealthData.ShieldSlotHealth);
    }

    [ClientRpc]
    public void TakeDamageClientRpc(float damage, bool ignoreShield) // add shield only modifier ?
    {
        if (!IsOwner) { return; }

        if (damage <= 0) { return; }

        if (!ignoreShield && Shield)
        {
            damage = Shield.TakeDamage(damage);
        }

        if (damage <= 0) { return; }

        CurrentHealth -= damage;
    }


    public void RawHeal(float healProficiency)
    {
        RegenerateShield(RegenerateHealth(healProficiency), true);
    }

    public float RegenerateHealth(float healProficiency)
    {
        CurrentHealth += healProficiency;
        if (CurrentHealth < HealthData.MaxHealth) { return 0; }

        var excessHeal = CurrentHealth - HealthData.MaxHealth;
        CurrentHealth = HealthData.MaxHealth;
        return excessHeal;
   
    }

    public void RegenerateShield(float healProficiency, bool canReviveCell)
    {
        Shield.Heal(healProficiency, canReviveCell);

        //if (canReviveCell)
        //{
        //    var totalShieldAfterHeal = TotalShield + howMuch;
        //    currentShieldSlots = (int)(totalShieldAfterHeal / healthData.ShieldSlotHealth);
        //    currentShieldSlotRemainingPower = totalShieldAfterHeal - currentShieldSlots * healthData.ShieldSlotHealth;
        //    currentShieldSlots++; // the half filled slot
        //}
        //else
        //{
        //    currentShieldSlots++;
        //    currentShieldSlotRemainingPower = healthData.ShieldSlotHealth;
        //}
        
        //if (currentShieldSlots > healthData.MaxShieldSlots)
        //{
        //    currentShieldSlots = healthData.MaxShieldSlots;
        //    currentShieldSlotRemainingPower = healthData.ShieldSlotHealth;
        //}
    }
}