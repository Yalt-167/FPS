using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class DeletePlayer : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Destroy(PlayerMovement.Instance.gameObject);
        Destroy(TimerDisplay.Instance.gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}

    
