using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AutoDestroy : MonoBehaviour
{
    public void StartDestroySequence(float lifeTime)
    {
        Destroy(gameObject, lifeTime);
    }
}
