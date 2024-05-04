using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PersistentComponent : MonoBehaviour
{
    private void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }
}
