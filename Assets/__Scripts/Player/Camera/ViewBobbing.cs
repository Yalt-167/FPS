using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewBobbing : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [SerializeField] private float bobbingSpeed = 0.1f; // Speed of the bobbing motion
    [SerializeField] private float bobbingAmount = 0.1f; // Amount of bobbing motion
    [SerializeField] private Transform cameraTransform; // Reference to the camera transform
    private Vector3 originalPosition; // Original camera position

    private float BobbingSpeed => 0f;
        //PlayerMovement.Instance.IsGrounded ?
        //    PlayerMovement.Instance.IsSliding || PlayerMovement.Instance.IsDashing ?
        //        0f
        //        :
        //        PlayerMovement.Instance.CurrentSpeed > 5f ?
        //            10f
        //            : 1f
        //    :
        //0f;

    private Coroutine bobbingCoroutine;

    private void Awake()
    {
        // Store the original position of the camera
        originalPosition = cameraTransform.localPosition;
    }

    private IEnumerator Bob()
    {
        var timer = 0f;

        for (; ; )
        {
            var verticalOffset = Mathf.Sin(timer) * bobbingAmount;

            cameraTransform.localPosition = originalPosition + new Vector3(0f, verticalOffset, 0f);

            timer += BobbingSpeed * Time.deltaTime;

            yield return null;
        }
    }
}

