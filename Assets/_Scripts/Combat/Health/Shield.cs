using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public class Shield
{
    private ShieldCell[] shieldCells;
    private int shieldCellAmount;
    private int shieldCellHealth;
    private int lastHealthyShieldCellIndex;
    private int LastHealthyShieldCellIndex
    {
        get
        {
            return lastHealthyShieldCellIndex;
        }
        set
        {
            lastHealthyShieldCellIndex = Mathf.Clamp(value, 0, shieldCellAmount - 1);
        }
    }
    private bool FullShield => shieldCells[shieldCellAmount - 1].Shield == shieldCellHealth;
    public Shield(int shieldCellAmount_, int shieldCellHealth_)
    {
        shieldCellAmount = shieldCellAmount_;
        LastHealthyShieldCellIndex = shieldCellAmount - 1;

        shieldCellHealth = shieldCellHealth_;

        shieldCells = new ShieldCell[shieldCellAmount_];
        for (int i = 0; i < shieldCellAmount_; i++)
        {
            shieldCells[i] = new(shieldCellHealth);
        }
    }

    public float TakeDamage(float damage)
    {
        while (damage > 0 && this)
        {
            damage = shieldCells[LastHealthyShieldCellIndex].TakeDamage(damage);
            if (damage > 0) LastHealthyShieldCellIndex--;
        }
        return damage;
    }


    public void Heal(float healProficiency, bool canReviveCell)
    {
        while (healProficiency > 0 && !FullShield)
        {
            Debug.Log(LastHealthyShieldCellIndex);
            healProficiency = shieldCells[LastHealthyShieldCellIndex].Heal(healProficiency, canReviveCell);
            if (healProficiency > 0) LastHealthyShieldCellIndex++;
        }
    }

    public IEnumerable<TupleIndexValue> AsSliderValues()
    {
        for (int i = 0; i < shieldCellAmount; i++)
        {
            yield return new(i, shieldCells[i].AsSliderValue());
        }
    }
    
    public static implicit operator bool(Shield shield)
    {
        return shield.shieldCells[0].Shield > 0;
    }
}

public struct TupleIndexValue
{
    public int Index;
    public float Value;

    public TupleIndexValue(int  index, float value)
    {
        Index = index;
        Value = value;
    }
}
