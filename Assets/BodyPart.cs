using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BodyPart : MonoBehaviour, IShootable
{
    //private PlayerHealth playerHealth;
    [SerializeField] private DamageMultipliers relevantDamageMultiplier;

    public void ReactShot(Vector3 _, Vector3 __)
    {
        throw new System.NotImplementedException();
    }
}

public enum DamageMultipliers
{
    HEAD,
    BODY,
    LEGS
}