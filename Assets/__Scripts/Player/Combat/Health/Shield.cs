using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

public sealed class Shield
{
    private ShieldCell[] shieldCells;
    private ushort shieldCellAmount;
    private ushort shieldCellHealth;
    private ushort lastHealthyShieldCellIndex;
    private ushort LastHealthyShieldCellIndex
    {
        get
        {
            return lastHealthyShieldCellIndex;
        }
        set
        {
            lastHealthyShieldCellIndex = (ushort)Mathf.Clamp(value, 0, shieldCellAmount - 1);
        }
    }
    private bool FullShield => shieldCells[shieldCellAmount - 1].Shield == shieldCellHealth;
    public Shield(ushort shieldCellAmount_, ushort shieldCellHealth_)
    {
        shieldCellAmount = shieldCellAmount_;
        LastHealthyShieldCellIndex = (ushort)(shieldCellAmount - 1);

        shieldCellHealth = shieldCellHealth_;

        shieldCells = new ShieldCell[shieldCellAmount_];
        for (int i = 0; i < shieldCellAmount_; i++)
        {
            shieldCells[i] = new(shieldCellHealth);
        }
    }

    public ushort TakeDamage(ushort damage)
    {
        while (damage > 0 && this)
        {
            damage = shieldCells[LastHealthyShieldCellIndex].TakeDamage(damage);
            if (damage > 0) LastHealthyShieldCellIndex--;
        }
        return damage;
    }


    public void Heal(ushort healProficiency, bool canReviveCell)
    {
        while (healProficiency > 0 && !FullShield)
        {
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
