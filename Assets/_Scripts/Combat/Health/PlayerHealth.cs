using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [field: SerializeField] public PlayerHealthData HealthData { get; private set; }

    public float CurrentHealth { get; private set; }
    private int currentShieldSlots;
    private float currentShieldSlotRemainingPower;
    private float TotalShield => (currentShieldSlots - 1) * HealthData.ShieldSlotHealth + currentShieldSlotRemainingPower;

    public Shield Shield { get; private set; }
    //private Slider healthSlider;
    //private Slider[] shieldCellsSliders;
    //[SerializeField] private GameObject shieldCellPrefab;
    //[SerializeField] private Transform healthCanvasTransform;

    //private static readonly int leftMostHealthBarExtent = 460;
    //private static readonly int healthBarLength = 1000;
    //[SerializeField] private int shieldCellPadding = 10;
    private static readonly int shieldBarY = -420;
    private bool Alive => CurrentHealth > 0;

    private void Awake()
    {
        ResetHealth();
        PassiveRegen();
        //healthSlider = transform.GetChild(4).GetChild(0).GetComponent<Slider>();
    }

    //private void Start()
    //{
    //    SetupHealthBar();
    //}

    //private void SetupHealthBar()
    //{
    //    shieldCellsSliders = new Slider[HealthData.MaxShieldSlots];

    //    var allocatedPaddingsAmount = HealthData.MaxShieldSlots - 1;
    //    var spaceDedicatedToPadding = allocatedPaddingsAmount * shieldCellPadding;
    //    var spaceDedicatedToShieldCells = healthBarLength - spaceDedicatedToPadding;
    //    var spaceAllocatedPerShieldCell = (float)spaceDedicatedToShieldCells / HealthData.MaxShieldSlots;

    //    var halfCellSize = spaceAllocatedPerShieldCell / 2;
    //    for (int i = 0; i < HealthData.MaxShieldSlots; i++)
    //    {
    //        var shieldCelleGameObject = Instantiate(shieldCellPrefab, healthCanvasTransform);
    //        shieldCellsSliders[i] = shieldCelleGameObject.GetComponent<Slider>();
    //        var shieldCellRect = shieldCelleGameObject.GetComponent<RectTransform>();

    //        var sizeDeltaWhateverThatMeans = shieldCellRect.sizeDelta;
    //        sizeDeltaWhateverThatMeans.x = spaceAllocatedPerShieldCell;
    //        shieldCellRect.sizeDelta = sizeDeltaWhateverThatMeans;

    //        shieldCellRect.anchoredPosition = new(leftMostHealthBarExtent + halfCellSize + i * (spaceAllocatedPerShieldCell + shieldCellPadding), shieldBarY);
    //    }

    //    //var x = leftMostHealthBarExtent + spaceAllocatedPerShieldCell / 2;
    //    //for (int i = 0; i < healthData.MaxShieldSlots; i++)
    //    //{
    //    //    var shieldCelleGameObject = Instantiate(shieldCellPrefab, healthCanvasTransform);
    //    //    shieldCells[i] = shieldCelleGameObject.GetComponent<Slider>();
    //    //    var shieldCellRect = shieldCelleGameObject.GetComponent<RectTransform>();

    //    //    var sizeDeltaWhateverThatMeans = shieldCellRect.sizeDelta;
    //    //    sizeDeltaWhateverThatMeans.x = spaceAllocatedPerShieldCell;
    //    //    shieldCellRect.sizeDelta = sizeDeltaWhateverThatMeans;

    //    //    shieldCellRect.anchoredPosition = new(x, healthBarY);
    //    //    x += spaceAllocatedPerShieldCell + shieldCellPadding;
    //    //}
    //}

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
            yield return new WaitUntil(() => lastHealTime + HealthData.PassiveHealthRegenerationRate < Time.time && CurrentHealth < HealthData.MaxHealth);
            RegenerateHealth(HealthData.PassiveHealthRegenerationAmount);
        }
    }

    private IEnumerator PassiveShieldRegen()
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
        if (!Alive) { return; }

        //healthSlider.value = CurrentHealth / HealthData.MaxHealth;
        //foreach (var idxValueTuple in Shield.AsSliderValues())
        //{
        //    shieldCellsSliders[idxValueTuple.Index].value = idxValueTuple.Value;
        //}


        if (Input.GetKeyDown(KeyCode.L)) { TakeDamage(3, false); }

    }

    private void ResetHealth()
    {
        CurrentHealth = HealthData.MaxHealth;
        currentShieldSlots = HealthData.MaxShieldSlots;
        currentShieldSlotRemainingPower = HealthData.ShieldSlotHealth;

        Shield = new(HealthData.MaxShieldSlots, HealthData.ShieldSlotHealth);
    }

    public void TakeDamage(float damage, bool ignoreShield)
    {
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