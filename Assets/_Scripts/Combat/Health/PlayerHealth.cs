using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private PlayerHealthData healthData;

    private float currentHealth;
    private int currentShieldSlots;
    private float currentShieldSlotRemainingPower;
    private float TotalShield => (currentShieldSlots - 1) * healthData.ShieldSlotHealth + currentShieldSlotRemainingPower;

    private Slider healthSlider;
    private Slider shieldSlider;
    private bool Alive => currentHealth > 0;

    private void Awake()
    {
        ResetHealth();
        PassiveRegen();
        healthSlider = transform.GetChild(4).GetChild(0).GetComponent<Slider>();
        shieldSlider = transform.GetChild(4).GetChild(1).GetComponent<Slider>();
    }

    private void PassiveRegen()
    {
        StartCoroutine(PassiveHealthRegen());
        StartCoroutine(PassiveShieldRegen());
    }

    private IEnumerator PassiveHealthRegen()
    {
        float lastHealTime;
        for (; ; )
        {
            lastHealTime = Time.time;
            yield return new WaitUntil(() => lastHealTime + healthData.PassiveHealthRegenerationRate < Time.time && currentHealth < healthData.MaxHealth);
            RegenerateHealth(healthData.PassiveHealthRegenerationAmount);
        }
    }

    private IEnumerator PassiveShieldRegen()
    {
        float lastHealTime;
        for (; ; )
        {
            lastHealTime = Time.time;
            yield return new WaitUntil(() => lastHealTime + healthData.PassiveShieldRegenerationRate < Time.time && currentHealth == healthData.MaxHealth);
            RegenerateShield(healthData.PassiveShieldRegenerationAmount, false);
        }
    }

    private void Update()
    {
        if (!Alive) { return; }

        healthSlider.value = currentHealth / healthData.MaxHealth;
        shieldSlider.value = TotalShield / (healthData.MaxShieldSlots * healthData.ShieldSlotHealth);

        if (Input.GetKeyDown(KeyCode.L)) { TakeDamage(10, false); }

    }

    private void ResetHealth()
    {
        currentHealth = healthData.MaxHealth;
        currentShieldSlots = healthData.MaxShieldSlots;
        currentShieldSlotRemainingPower = healthData.ShieldSlotHealth;
    }

    public void TakeDamage(float damage, bool ignoreShield)
    {
        if (damage < 0) { return; }

        while (!ignoreShield && damage > 0 && currentShieldSlots > 0)
        {
            currentShieldSlotRemainingPower -= damage;
            if (currentShieldSlotRemainingPower < 0)
            {
                damage = -currentShieldSlotRemainingPower;
                currentShieldSlotRemainingPower = healthData.ShieldSlotHealth;
                currentShieldSlots--;
            }
            else
            {
                damage = 0;
            }
        }

        currentHealth -= damage;
    }


    public void RawHeal(int howMuch)
    {
        RegenerateShield(RegenerateHealth(howMuch), true);
    }

    public int RegenerateHealth(int howMuch)
    {
        currentHealth += howMuch;
        if (currentHealth < healthData.MaxHealth) { return 0; }

        var excessHeal = currentHealth - healthData.MaxHealth;
        currentHealth = healthData.MaxHealth;
        return (int)excessHeal;
   
    }

    public void RegenerateShield(int howMuch, bool canGoBeyondGauge)
    {
        if (canGoBeyondGauge)
        {
            var totalShieldAfterHeal = TotalShield + howMuch;
            currentShieldSlots = (int)(totalShieldAfterHeal / healthData.ShieldSlotHealth);
            currentShieldSlotRemainingPower = totalShieldAfterHeal - currentShieldSlots * healthData.ShieldSlotHealth;
            currentShieldSlots++; // the half filled slot
        }
        else
        {
            currentShieldSlots++;
            currentShieldSlotRemainingPower = healthData.ShieldSlotHealth;
        }
        
        if (currentShieldSlots > healthData.MaxShieldSlots)
        {
            currentShieldSlots = healthData.MaxShieldSlots;
            currentShieldSlotRemainingPower = healthData.ShieldSlotHealth;
        }
    }
}
