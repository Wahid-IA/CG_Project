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

    [Header("Local Camera Framing (Over-the-Shoulder)")]
    [Tooltip("Camera position relative to player local space (X = side, Y = height, Z = distance behind).")]
    public Vector3 menuLocalOffset = new Vector3(-0.8f, 1.5f, -2.2f); 
    
    [Tooltip("Point the camera looks at relative to player local space.")]
    public Vector3 lookAtLocalOffset = new Vector3(0.5f, 1.2f, 3.0f);

    [Header("Transition Settings")]
    public float fadeDuration = 0.4f;
    public float standUpDelay = 0.6f; 
    public float transitionDuration = 1.0f; 

    [Header("Walk & Stand Settings")]
    public float turnAngleOffset = 0f;
    public float moveForwardDistance = 0.8f;
    public string walkAnimBoolName = "IsMoving"; 
    public string walkSpeedFloatName = "Speed";
    public float walkSpeedValue = 1.0f;

    public static bool isMainMenuActive = true;

    private Camera mainCam;

    void Start()
    {
        isMainMenuActive = true;
        mainCam = Camera.main;

        if (playerAnimator == null && playerTransform != null)
        {
            playerAnimator = playerTransform.GetComponentInChildren<Animator>();
        }

        // 1. Disable gameplay camera & player controls
        if (cameraFollowScript != null) cameraFollowScript.enabled = false;
        if (playerControllerScript != null) playerControllerScript.enabled = false;
        if (hudContainer != null) hudContainer.SetActive(false);

        // 2. Setup UI & Cursor
        if (mainMenuCanvasGroup != null)
        {
            mainMenuCanvasGroup.gameObject.SetActive(true);
            mainMenuCanvasGroup.alpha = 1f;
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 3. Trigger Sitting Pose
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", true);
        }

        // Initial camera snap
        PositionMenuCamera();
    }

    void LateUpdate()
    {
        // Continuously update camera during main menu to track physics/animations correctly
        if (isMainMenuActive && playerTransform != null)
        {
            PositionMenuCamera();
        }
    }

    private void PositionMenuCamera()
    {
        if (playerTransform == null || mainCam == null) return;

        // Calculate unscaled offsets so object scale doesn't distort camera position
        Vector3 menuWorldPos = playerTransform.position + (playerTransform.rotation * menuLocalOffset);
        Vector3 lookWorldTarget = playerTransform.position + (playerTransform.rotation * lookAtLocalOffset);

        mainCam.transform.position = menuWorldPos;
        mainCam.transform.rotation = Quaternion.LookRotation(lookWorldTarget - menuWorldPos);
    }

    public void OnStartGameClicked()
    {
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        isMainMenuActive = false; // Stop LateUpdate positioning override

        if (playerAnimator == null && playerTransform != null)
        {
            playerAnimator = playerTransform.GetComponentInChildren<Animator>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 1. Fade out UI Panel
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

        // 2. Trigger Stand Up Animation
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", false);
            playerAnimator.SetTrigger("StandUp");
        }

        yield return new WaitForSeconds(standUpDelay);

        // 3. Blend camera smoothly to gameplay position
        if (playerTransform != null && mainCam != null && cameraFollowScript != null)
        {
            SetWalkAnimation(true);

            Vector3 camStartPos = mainCam.transform.position;
            Quaternion camStartRot = mainCam.transform.rotation;

            Quaternion playerStartRot = playerTransform.rotation;
            Quaternion playerTargetRot = Quaternion.Euler(0f, playerTransform.eulerAngles.y + turnAngleOffset, 0f);

            elapsedTime = 0f;
            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / transitionDuration;
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                // Smooth player turn
                playerTransform.rotation = Quaternion.Slerp(playerStartRot, playerTargetRot, smoothT);

                // Forward step movement
                if (moveForwardDistance > 0f)
                {
                    float moveStep = (moveForwardDistance / transitionDuration) * Time.deltaTime;
                    playerTransform.position += playerTransform.forward * moveStep;
                }

                // Blend camera toward cameraFollowScript position
                Vector3 gameplayTargetPos = GetGameplayCameraPosition();
                Quaternion gameplayTargetRot = GetGameplayCameraRotation();

                mainCam.transform.position = Vector3.Lerp(camStartPos, gameplayTargetPos, smoothT);
                mainCam.transform.rotation = Quaternion.Slerp(camStartRot, gameplayTargetRot, smoothT);

                yield return null;
            }

            SetWalkAnimation(false);
        }

        // 4. Enable gameplay controls
        if (cameraFollowScript != null) cameraFollowScript.enabled = true;
        if (playerControllerScript != null) playerControllerScript.enabled = true;
        if (hudContainer != null) hudContainer.SetActive(true);
    }

    private Vector3 GetGameplayCameraPosition()
    {
        if (cameraFollowScript == null || playerTransform == null) return mainCam.transform.position;

        Quaternion rotation = Quaternion.Euler(15f, playerTransform.eulerAngles.y, 0f);
        Vector3 targetPosition = playerTransform.position + cameraFollowScript.targetOffset;
        return targetPosition - (rotation * Vector3.forward * cameraFollowScript.distance);
    }

    private Quaternion GetGameplayCameraRotation()
    {
        if (playerTransform == null) return mainCam.transform.rotation;
        return Quaternion.Euler(15f, playerTransform.eulerAngles.y, 0f);
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