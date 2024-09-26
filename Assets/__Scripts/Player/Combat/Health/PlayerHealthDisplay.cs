using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class PlayerHealthDisplay : MonoBehaviour
{
    private PlayerHealthNetworked playerHealth;
    private Slider healthSlider;
    private Slider[] shieldCellsSliders;
    private PlayerHealthData HealthData => playerHealth.HealthData;
    private Shield Shield => playerHealth.Shield;
    private float Health => playerHealth.CurrentHealth;

    private static readonly int leftMostHealthBarExtent = 460;
    private static readonly int healthBarLength = 1000;
    private static readonly int shieldCellPadding = 10;
    private static readonly int shieldBarY = -420;

    [SerializeField] private GameObject shieldCellPrefab;
    [SerializeField] private Transform healthCanvasTransform;


    private void Awake()
    {
        playerHealth = GetComponent<PlayerHealthNetworked>();
        healthSlider = transform.GetChild(4).GetChild(0).GetComponent<Slider>();
        SetupHealthBar();
    }


    private void SetupHealthBar()
    {
        shieldCellsSliders = new Slider[HealthData.MaxShieldSlots];

        var allocatedPaddingsAmount = HealthData.MaxShieldSlots - 1;
        var spaceDedicatedToPadding = allocatedPaddingsAmount * shieldCellPadding;
        var spaceDedicatedToShieldCells = healthBarLength - spaceDedicatedToPadding;
        var spaceAllocatedPerShieldCell = (float)spaceDedicatedToShieldCells / HealthData.MaxShieldSlots;

        var halfCellSize = spaceAllocatedPerShieldCell / 2;
        for (int i = 0; i < HealthData.MaxShieldSlots; i++)
        {
            var shieldCelleGameObject = Instantiate(shieldCellPrefab, healthCanvasTransform);
            shieldCellsSliders[i] = shieldCelleGameObject.GetComponent<Slider>();
            var shieldCellRect = shieldCelleGameObject.GetComponent<RectTransform>();

            var sizeDeltaWhateverThatMeans = shieldCellRect.sizeDelta;
            sizeDeltaWhateverThatMeans.x = spaceAllocatedPerShieldCell;
            shieldCellRect.sizeDelta = sizeDeltaWhateverThatMeans;

            shieldCellRect.anchoredPosition = new(leftMostHealthBarExtent + halfCellSize + i * (spaceAllocatedPerShieldCell + shieldCellPadding), shieldBarY);
        }

        //var x = leftMostHealthBarExtent + spaceAllocatedPerShieldCell / 2;
        //for (int i = 0; i < healthData.MaxShieldSlots; i++)
        //{
        //    var shieldCelleGameObject = Instantiate(shieldCellPrefab, healthCanvasTransform);
        //    shieldCells[i] = shieldCelleGameObject.GetComponent<Slider>();
        //    var shieldCellRect = shieldCelleGameObject.GetComponent<RectTransform>();

        //    var sizeDeltaWhateverThatMeans = shieldCellRect.sizeDelta;
        //    sizeDeltaWhateverThatMeans.x = spaceAllocatedPerShieldCell;
        //    shieldCellRect.sizeDelta = sizeDeltaWhateverThatMeans;

        //    shieldCellRect.anchoredPosition = new(x, healthBarY);
        //    x += spaceAllocatedPerShieldCell + shieldCellPadding;
        //}
    }

    private void Update()
    {
        healthSlider.value = Health / HealthData.MaxHealth;

        foreach (var idxValueTuple in Shield.AsSliderValues())
        {
            shieldCellsSliders[idxValueTuple.Index].value = idxValueTuple.Value;
        }

        //for (int i = 0; i < Shield.ShieldCellAmount; i++)
        //{
        //    MyDebug.DebugOSD.Display($"{i}_", $"{Shield[i].Shield}/{Shield[i].MaxShield} = {(float)Shield[i].Shield / Shield[i].MaxShield}");
        //}
    }
}
