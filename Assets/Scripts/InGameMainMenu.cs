using UnityEngine;
using System.Collections;

public class InGameMainMenu : MonoBehaviour
{
    [Header("UI References")]
    public CanvasGroup mainMenuCanvasGroup; 
    public GameObject hudContainer;        

    [Header("Player Components")]
    public Transform playerTransform;
    public Animator playerAnimator;
    public MonoBehaviour playerControllerScript; 

    [Header("Camera Components")]
    public CameraFollow cameraFollowScript;

    [Header("Relative Camera Offsets (No fixed coordinates)")]
    [Tooltip("Camera offset relative to player (X: Right/Left, Y: Up, Z: Forward)")]
    public Vector3 menuOffset = new Vector3(-1.8f, 1.2f, 2.5f); 
    public Vector3 lookAtOffset = new Vector3(0f, 1.0f, 0f);

    [Header("Transition Settings")]
    public float fadeDuration = 0.5f;
    [Tooltip("Time allowed for stand up animation to finish before moving")]
    public float standUpDelay = 0.8f; 
    [Tooltip("Combined duration for player turn/walk and camera blend (Lower = Faster)")]
    public float transitionDuration = 0.8f; 

    [Header("Walk & Movement Settings")]
    [Tooltip("Degrees added to the turn angle (increase for a larger/sharper turn rotation)")]
    public float turnAngleOffset = 0f;
    [Tooltip("Distance in meters the player walks forward while turning")]
    public float moveForwardDistance = 1.5f;
    [Tooltip("Playback speed of the walking animation (1.2 = faster motion)")]
    public float turnAnimationSpeed = 1.2f;
    [Tooltip("Name of the Bool parameter in Animator for movement (e.g. IsMoving, IsWalking)")]
    public string walkAnimBoolName = "IsMoving"; 
    [Tooltip("Name of the Float parameter if your Animator uses Speed/Forward floats")]
    public string walkSpeedFloatName = "Speed";
    public float walkSpeedValue = 1.0f;

    public static bool isMainMenuActive = true;

    void Start()
    {
        isMainMenuActive = true;

        // 1. Disable gameplay camera & player controls
        if (cameraFollowScript != null) cameraFollowScript.enabled = false;
        if (playerControllerScript != null) playerControllerScript.enabled = false;
        if (hudContainer != null) hudContainer.SetActive(false);

        // 2. Position camera relative to player
        if (playerTransform != null && Camera.main != null)
        {
            Vector3 dynamicMenuPos = playerTransform.position + menuOffset;
            Vector3 lookTarget = playerTransform.position + lookAtOffset;

            Camera.main.transform.position = dynamicMenuPos;
            Camera.main.transform.rotation = Quaternion.LookRotation(lookTarget - dynamicMenuPos);
        }

        // 3. Setup UI & Cursor
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.gameObject.SetActive(true);
            mainMenuCanvasGroup.alpha = 1f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 4. Trigger Sitting Pose
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", true);
        }
    }

    public void OnStartGameClicked()
    {
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 1. Fade out UI
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            if (mainMenuCanvasGroup != null)
            {
                mainMenuCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            }
            yield return null;
        }

        if (mainMenuCanvasGroup != null) mainMenuCanvasGroup.gameObject.SetActive(false);

        // 2. Trigger Stand Up Animation FIRST
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", false);
            playerAnimator.SetTrigger("StandUp");
        }

        // Wait for stand up animation to finish before transition
        yield return new WaitForSeconds(standUpDelay);

        // 3. Simultaneously rotate/walk player AND blend camera quickly
        if (playerTransform != null && Camera.main != null && cameraFollowScript != null)
        {
            // Speed up animation playback
            if (playerAnimator != null) playerAnimator.speed = turnAnimationSpeed;
            SetWalkAnimation(true);

            // Initial player & camera states
            Quaternion playerStartRot = playerTransform.rotation;
            Quaternion playerTargetRot = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y + turnAngleOffset, 0f);

            Vector3 camStartPos = Camera.main.transform.position;
            Quaternion camStartRot = Camera.main.transform.rotation;
            Quaternion camTargetRot = Quaternion.Euler(15f, playerTargetRot.eulerAngles.y, 0f);

            elapsedTime = 0f;
            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / transitionDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // A. Rotate player towards target direction
                playerTransform.rotation = Quaternion.Slerp(playerStartRot, playerTargetRot, smoothT);

                // B. Translate player forward along facing vector
                float moveStep = (moveForwardDistance / transitionDuration) * Time.deltaTime;
                playerTransform.position += playerTransform.forward * moveStep;

                // C. Calculate updated gameplay camera position dynamically behind player
                Vector3 currentGameplayCamPos = (playerTransform.position + cameraFollowScript.targetOffset) - (camTargetRot * Vector3.forward * cameraFollowScript.distance);

                // D. Blend camera towards target position & rotation smoothly
                Camera.main.transform.position = Vector3.Lerp(camStartPos, currentGameplayCamPos, smoothT);
                Camera.main.transform.rotation = Quaternion.Slerp(camStartRot, camTargetRot, smoothT);

                yield return null;
            }

            playerTransform.rotation = playerTargetRot;

            // Stop Walk Animation and restore normal animator speed
            SetWalkAnimation(false);
            if (playerAnimator != null) playerAnimator.speed = 1.0f;
        }

        // 4. Enable Gameplay Control
        if (cameraFollowScript != null) cameraFollowScript.enabled = true;
        if (playerControllerScript != null) playerControllerScript.enabled = true;
        if (hudContainer != null) hudContainer.SetActive(true);

        isMainMenuActive = false;
    }

    private void SetWalkAnimation(bool isWalking)
    {
        if (playerAnimator == null) return;

        if (!string.IsNullOrEmpty(walkAnimBoolName))
        {
            playerAnimator.SetBool(walkAnimBoolName, isWalking);
        }

        if (!string.IsNullOrEmpty(walkSpeedFloatName))
        {
            playerAnimator.SetFloat(walkSpeedFloatName, isWalking ? walkSpeedValue : 0f);
        }
    }

    public void OnQuitClicked()
    {
        Application.Quit();
        Debug.Log("Game Quit!");
    }
}