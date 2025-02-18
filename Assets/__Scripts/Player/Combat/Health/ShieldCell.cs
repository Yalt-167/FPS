using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public sealed class ShieldCell
{
    public int Shield { get; private set; }
    public int MaxShield { get; private set; }
    private bool broken;

    public ShieldCell(ushort maxShield_)
    {
        Shield = MaxShield = maxShield_;
        broken = false;
    }

    
    public ushort TakeDamage(int damage)
    {
        Shield -= damage;
        if (Shield <= 0)
        {
            damage = -Shield;
            Break();
            return (ushort)damage; // return excess damage to pass it onto the next shield cell
        }
        return 0;
    }

    public void Break()
    {
        broken = true;
        Shield = 0;
    }

    public void Repair()
    {
        broken = false;
    }

    public void InvertState() // could be a funny modifier
    {
        broken = !broken;
    }

    public ushort Heal(int healProficiency, bool canReviveCell)
    {
        if (broken && !canReviveCell) { return 0; }

        Repair(); // doesn t make much sense but if arrived there then either the shield cell is ealready health or the canReviveCell flag is true

        Shield += healProficiency;
        if (Shield > MaxShield)
        {
            healProficiency = Shield - MaxShield;
            Shield = MaxShield;
            return (ushort)healProficiency; // return the excess heal in order to pass it onto the next shield cell
        }
        return 0;
        
    }

    public float AsSliderValue()
    {
        return (float)Shield / MaxShield;
    }
}
