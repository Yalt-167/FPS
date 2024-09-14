using System;
using System.Collections;
using System.Collections.Generic;

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
    private bool IsFullShield => shieldCells[shieldCellAmount - 1].Shield == shieldCellHealth;
    public bool HasShield => shieldCells[0].Shield > 0;
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
        while (damage > 0 && HasShield)
        {
            damage = shieldCells[LastHealthyShieldCellIndex].TakeDamage(damage);
            if (damage > 0) { LastHealthyShieldCellIndex--; }
        }
        return damage;
    }


    public void Heal(ushort healProficiency, bool canReviveCell)
    {
        while (healProficiency > 0 && !IsFullShield)
        {
            healProficiency = shieldCells[LastHealthyShieldCellIndex].Heal(healProficiency, canReviveCell);
            if (healProficiency > 0) { LastHealthyShieldCellIndex++; }
        }
    }

    public IEnumerable<IndexValueTuple<float>> AsSliderValues()
    {
        for (int i = 0; i < shieldCellAmount; i++)
        {
            yield return new(i, shieldCells[i].AsSliderValue());
        }
    }
}

public struct IndexValueTuple<T>
{
    public int Index;
    public T Value;

    public IndexValueTuple(int  index, T value)
    {
        Index = index;
        Value = value;
    }
}
