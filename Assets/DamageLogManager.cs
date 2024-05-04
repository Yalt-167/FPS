using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageLogManager : MonoBehaviour
{
    [Tooltip("Order: Head(Shielded) -> Body(Shielded) -> Head -> Body")][SerializeField] private Color[] hitColors;
    [SerializeField] private GameObject damageLogPrefab;
    [SerializeField] private List<GameObject> activeDamageLogs;


    private void SummonDamageLog()
    {

    }

}
