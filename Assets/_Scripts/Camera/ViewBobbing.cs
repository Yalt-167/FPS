using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ViewBobbing : MonoBehaviour
{
    [Header("Bobbing Settings")]
    [SerializeField] private float bobbingSpeed = 0.1f; // Speed of the bobbing motion
    [SerializeField] private float bobbingAmount = 0.1f; // Amount of bobbing motion
    [SerializeField] private Transform cameraTransform; // Reference to the camera transform
    private Vector3 originalPosition; // Original position of the camera

    private float BobbingSpeed =>
        PlayerMovement.Instance.IsGrounded ?
            PlayerMovement.Instance.IsSliding || PlayerMovement.Instance.IsDashing ?
                0f
                :
                PlayerMovement.Instance.CurrentSpeed > 5f ?
                    10f
                    : 1f
            :
        0f;

    private void Start()
    {
        // Store the original position of the camera
        originalPosition = cameraTransform.localPosition;

        // Start the view bobbing coroutine
        if (Game.Manager.GameSettings.viewBobbing) StartCoroutine(Bob());
    }

    private IEnumerator Bob()
    {
        float timer = 0f;

        while (true)
        {
            // Calculate vertical position using a sine wave
            float verticalOffset = Mathf.Sin(timer) * bobbingAmount;

            // Apply the bobbing motion to the camera's local position
            cameraTransform.localPosition = originalPosition + new Vector3(0f, verticalOffset, 0f);

            // Increment timer based on speed
            timer += BobbingSpeed * Time.deltaTime;

            yield return null;
        }
    }
}

