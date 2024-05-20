using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDamageable
{
    public ulong OwnerHealthNetworkID { get; }
    //public bool AreTheSameTarget(List<IDamageable> all);
}
