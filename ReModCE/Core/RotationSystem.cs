using MelonLoader;
using ReModCE_ARES.Components;
using ReModCE_ARES.ControlSchemes.Interface;
using System;
using System.Collections;
using UnhollowerRuntimeLib;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ReModCE_ARES.Core
{
    public class RotationSystem
    {
        internal bool rotating;

        internal static bool NoClipFlying = true;

        internal static bool BarrelRolling, LockRotation;

        internal static RotationSystem Instance;

        internal static IControlScheme CurrentControlScheme;

        private bool usePlayerAxis, holdingShift;

        private Vector3 originalGravity;

        private Vector3 currentFlyingDirection = Vector3.zero;

        private Utilities.AlignTrackingToPlayerDelegate alignTrackingToPlayer;

        private Transform cameraTransform;

        private Transform playerTransform, originTransform;

        private RotationSystem()
        { }

        // For emmVRC and other mods to be able to check for
        // needs to fly so other mods can break it/this could break them
        public static bool Rotating => Instance.rotating;

        internal void Pitch(float inputAmount)
        {
            if (RotatorComponent._RotationInvertPitch) inputAmount *= -1;
            playerTransform.RotateAround(
                originTransform.position,
                usePlayerAxis ? playerTransform.right : originTransform.right,
                inputAmount * RotatorComponent._RotationSpeed * Time.deltaTime * (holdingShift ? 2f : 1f));
        }

        internal void Yaw(float inputAmount)
        {
            playerTransform.RotateAround(
                originTransform.position,
                usePlayerAxis ? playerTransform.up : originTransform.up,
                inputAmount * RotatorComponent._RotationSpeed * Time.deltaTime * (holdingShift ? 2f : 1f));
        }

        internal void Roll(float inputAmount)
        {
            playerTransform.RotateAround(
                originTransform.position,
                usePlayerAxis ? -playerTransform.forward : -originTransform.forward,
                inputAmount * RotatorComponent._RotationSpeed * Time.deltaTime * (holdingShift ? 2f : 1f));
        }

        internal void Fly(float inputAmount, Vector3 direction)
        {
            currentFlyingDirection += direction * inputAmount;
        }

        internal static bool Initialize()
        {
            if (Instance != null) return false;
            Instance = new RotationSystem();
            MelonCoroutines.Start(GrabMainCamera());
            return true;
        }

        private static IEnumerator GrabMainCamera()
        {
            while (!Instance.cameraTransform)
            {
                yield return new WaitForSeconds(1f);
                foreach (Object component in Object.FindObjectsOfType(Il2CppType.Of<Transform>()))
                {
                    yield return null;
                    Transform transform;
                    if ((transform = component.TryCast<Transform>()) == null) continue;
                    if (!transform.name.Equals("Camera (eye)", StringComparison.OrdinalIgnoreCase)) continue;
                    Instance.cameraTransform = transform;
                    break;
                }
            }
        }

        // bit weird but i've gotten some errors few times where it bugged out a bit
        internal void Toggle(bool state)
        {

            //if (FlyComponent. && state) ReModFly.isOn = false;
            ReModCE_ARES.RotatorEnabled = state;
            if (!rotating) originalGravity = Physics.gravity;

            try
            {
                playerTransform ??= Utilities.GetLocalVRCPlayer().transform;
                rotating = state;

                if (rotating)
                {
                    originalGravity = Physics.gravity;
                    Physics.gravity = Vector3.zero;
                    alignTrackingToPlayer ??= Utilities.GetAlignTrackingToPlayerDelegate;
                }
                else
                {
                    Quaternion local = playerTransform.localRotation;
                    playerTransform.localRotation = new Quaternion(0f, local.y, 0f, local.w);
                    Physics.gravity = originalGravity;
                }
            }
            catch (Exception e)
            {
                ReModCE_ARES.LogDebug("Error Toggling: " + e);
                rotating = false;
            }

            if (Utilities.GetStreamerMode) rotating = false;

            UpdateSettings();

            if (rotating) return;
            Physics.gravity = originalGravity;
            alignTrackingToPlayer?.Invoke();
        }

        private void GrabOriginTransform()
        {
            var isHumanoid = false;

            // ReSharper disable twice Unity.NoNullPropagation
            GameObject localAvatar = Utilities.GetLocalVRCPlayer()?.prop_VRCAvatarManager_0?.prop_GameObject_0;
            Animator localAnimator = localAvatar?.GetComponent<Animator>();

            if (localAnimator != null)
            {
                isHumanoid = localAnimator.isHuman;
                originTransform = isHumanoid ? localAnimator.GetBoneTransform(HumanBodyBones.Hips) : cameraTransform;
            }
            else
            {
                originTransform = cameraTransform;
            }


            usePlayerAxis = originTransform && isHumanoid;
        }

        internal void UpdateSettings()
        {
            if (!playerTransform) return;
            CharacterController characterController = playerTransform.GetComponent<CharacterController>();
            if (!characterController) return;

            if (rotating) GrabOriginTransform();

            if (rotating && !Utilities.IsInVR) characterController.enabled = !NoClipFlying;
            else if (!characterController.enabled)
                characterController.enabled = true;

            if (Utilities.IsInVR)
                Utilities.GetLocalVRCPlayer()?.prop_VRCPlayerApi_0.Immobilize(rotating);

            alignTrackingToPlayer = Utilities.GetAlignTrackingToPlayerDelegate;
        }

        internal void OnUpdate()
        {
            try
            {
                if (!rotating) return;

                holdingShift = Input.GetKey(KeyCode.LeftShift);
                if (!BarrelRolling
                    && CurrentControlScheme.HandleInput(playerTransform, cameraTransform))
                {
                    // How to stop being able to move diagonally being faster
                    float speed = Mathf.Clamp01(currentFlyingDirection.magnitude) * RotatorComponent._RotationFlightSpeed * Time.deltaTime * (holdingShift ? 2f : 1f);
                    playerTransform.position += currentFlyingDirection.normalized * speed;
                    alignTrackingToPlayer();
                }

                currentFlyingDirection = Vector3.zero;
            }
            catch { };
        }


        internal void OnLeftWorld()
        {
            rotating = false;
            playerTransform = null;
            alignTrackingToPlayer = null;
        }

        /// <summary>
        ///     Do 4 rolls within 2 seconds
        /// </summary>
        /// <returns></returns>
        private IEnumerator BarrelRollCoroutine()
        {
            BarrelRolling = true;
            bool originalRotated = rotating;

            if (!originalRotated) Toggle(true);

            var degreesCompleted = 0f;
            while (degreesCompleted < 720f)
            {
                yield return null;
                float currentRoll = 720 * Time.deltaTime;
                degreesCompleted += currentRoll;
                playerTransform.RotateAround(originTransform.position, usePlayerAxis ? -playerTransform.forward : -originTransform.forward, currentRoll);
                alignTrackingToPlayer?.Invoke();
            }

            yield return null;

            if (!originalRotated) Toggle(false);
            BarrelRolling = false;
        }

        public void BarrelRoll()
        {
            if (!Utilities.GetStreamerMode)
                MelonCoroutines.Start(BarrelRollCoroutine());
        }

    }
}
