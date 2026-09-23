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
    [Tooltip("Matches Left Turn clip length (~1.6 seconds).")]
    public float transitionDuration = 1.6f; 

    [Tooltip("Curve controlling zoom-out speed and camera pullback smoothing.")]
    public AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

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
        if (isMainMenuActive && playerTransform != null)
        {
            PositionMenuCamera();
        }
    }

    private void PositionMenuCamera()
    {
        if (playerTransform == null || mainCam == null) return;

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
        isMainMenuActive = false; // Stop LateUpdate menu camera tracking

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

        // 2. Trigger Stand Up / Exit Sitting Animation (Left Turn)
        if (playerAnimator != null)
        {
            playerAnimator.SetBool("IsSitting", false);
            playerAnimator.SetTrigger("StandUp");
        }

        // 3. Zoom out camera with STRICTLY FIXED rotation while player aligns to camera view
        if (playerTransform != null && mainCam != null && cameraFollowScript != null)
        {
            Vector3 camStartPos = mainCam.transform.position;
            
            // Freeze camera rotation to exact menu perspective (NO ROTATION DURING TRANSITION)
            Quaternion fixedCamRotation = mainCam.transform.rotation;

            // Compute player target rotation so they end up facing forward in camera's view direction
            Quaternion playerStartRot = playerTransform.rotation;
            Vector3 camForwardFlat = Vector3.ProjectOnPlane(fixedCamRotation * Vector3.forward, Vector3.up).normalized;
            Quaternion playerTargetRot = Quaternion.LookRotation(camForwardFlat);

            elapsedTime = 0f;
            while (elapsedTime < transitionDuration)
            {
                elapsedTime += Time.deltaTime;
                float t = elapsedTime / transitionDuration;
                float smoothT = transitionCurve != null ? transitionCurve.Evaluate(t) : Mathf.SmoothStep(0f, 1f, t);

                // Target gameplay position (Zoomed out & offset up) based on fixed camera angle
                Vector3 targetGameplayPos = playerTransform.position 
                                            + cameraFollowScript.targetOffset 
                                            - (fixedCamRotation * Vector3.forward * cameraFollowScript.distance);

                // 1. Translate camera position straight back and up
                mainCam.transform.position = Vector3.Lerp(camStartPos, targetGameplayPos, smoothT);
                
                // 2. Lock camera rotation
                mainCam.transform.rotation = fixedCamRotation;

                // 3. Turn player smoothly during the animation to match camera view direction
                playerTransform.rotation = Quaternion.Slerp(playerStartRot, playerTargetRot, smoothT);

                yield return null;
            }

            // Lock exact final pose and position
            mainCam.transform.rotation = fixedCamRotation;
            playerTransform.rotation = playerTargetRot;
            mainCam.transform.position = playerTransform.position 
                                        + cameraFollowScript.targetOffset 
                                        - (fixedCamRotation * Vector3.forward * cameraFollowScript.distance);
        }

        // 4. Hand off directly to gameplay controls with zero camera angle shift
        if (cameraFollowScript != null) 
        {
            cameraFollowScript.SyncRotationFromTransform();
            cameraFollowScript.enabled = true;
        }

        if (playerControllerScript != null) playerControllerScript.enabled = true;
        if (hudContainer != null) hudContainer.SetActive(true);
    }

    public void OnQuitClicked()
    {
        Application.Quit();
        Debug.Log("Game Quit!");
    }
}