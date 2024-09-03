//#define LOG_MOVEMENTS_EVENTS
#define BUFFER_ACTIONS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

using GameManagement;
using Inputs;

namespace Controller
{

    [DefaultExecutionOrder(-7)]
    public sealed class PlayerMovement : MonoBehaviour
        //, IPlayerFrameMember
    {
        #region Debug Things

#pragma warning disable
        [SerializeField] private bool Tilting;
        [SerializeField] private bool Regulating;
        [SerializeField] private bool Idle;
#pragma warning enable

        public VelocityDebug GlobalVelocityDebug;
        public VelocityDebug LocalVelocityDebug;
        public CollisionDebug CollisionDebug;

        [SerializeField] private bool doDebugCollidingDown;
        [SerializeField] private bool doDebugCollidingUp;
        [SerializeField] private bool doDebugCollidinAnySide;

        #endregion

        #region References

        private InputManager inputManager;
        private MovementInputQuery InputQuery => inputManager.MovementInputs;
        private Rigidbody Rigidbody;
        private FollowRotationCamera followRotationCamera;
        private int ForwardAxisInput => MyInput.GetAxis(InputQuery.Back[InputType.OnKeyHeld], InputQuery.Forward[InputType.OnKeyHeld]);
        private int SidewayAxisInput => MyInput.GetAxis(InputQuery.Left[InputType.OnKeyHeld], InputQuery.Right[InputType.OnKeyHeld]);
        private bool PressingForwardOrStrafeInput => InputQuery.Forward[InputType.OnKeyHeld] || InputQuery.Left[InputType.OnKeyHeld] || InputQuery.Right[InputType.OnKeyHeld];
        public Vector3 Position => transform.position;
        public Vector3 FeetPosition => transform.position + Vector3.down;
        public float CurrentSpeed => Rigidbody.velocity.Mask(1f, 0f, 1f).magnitude;
        public float CurrentForwardSpeed => Vector3.Dot(Rigidbody.velocity, transform.forward);
        public float CurrentStrafeSpeed => Mathf.Abs(Vector3.Dot(Rigidbody.velocity, transform.right));
        public float CurrentVerticalSpeed => Rigidbody.velocity.y;

        [Space(10)]
        [SerializeField] private MovementMode currentMovementMode;

        #region State

        private bool IsJumping { get; set; }
        private bool IsRunning => currentMovementMode == MovementMode.Run;
        private bool IsSprinting => !InputQuery.HoldCrouch && PressingForwardOrStrafeInput;
        private bool IsCrouching { get; set; }
        private bool IsSliding => currentMovementMode == MovementMode.Slide;
        private bool IsDashing => currentMovementMode == MovementMode.Dash;
        private bool IsWallrunning => currentMovementMode == MovementMode.Wallrun;
        private bool IsLedgeClimbing => currentMovementMode == MovementMode.LedgeClimb;
        private bool IsGrappling => currentMovementMode == MovementMode.Grappling;

        #endregion

        #region Action Buffering Setup

        private List<MovementActionData> bufferedActions = new();

        [SerializeField] private float validatedActionBufferDuration;

        #endregion

        #endregion

        #region Movements Setup

        [Header("Movements")]
        private float currentSpeedRatio; // Basically a lerp of the speed that increases overtime when running (basically some sort of acceleration
        [SerializeField] private float timeToReachMaxSprintSpeed;
        private float targetSpeed;
        [SerializeField] private float airFriction;
        private float CroouchingSpeed => RunningSpeed * .25f;
        private float RunningSpeed => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.SpeedStats.RunningSpeed ?? 8f;
        private float SpeedDifferenceBetweenWalkingAndSprinting => RunningSpeed - CroouchingSpeed;
        //private float StrafingSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.StrafingSpeed ?? 7f;
        private float StrafingSpeedCoefficient => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.SpeedStats.StrafingSpeedCoefficient ?? .75f;
        private float BackwardSpeed => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.SpeedStats.BackwardSpeed ?? 5f;
        private float WallRunSpeed => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.SpeedStats.WallRunningSpeed ?? 9f;


        [SerializeField] private float sidewayInertiaControlFactor; // when the direction changes apply it to control inertia and prevent the player from going sideway (former forward)

        private Action currentMovementMethod;

        [SerializeField] private float maxStepHeight;
        [SerializeField][Range(0, 90)] private float maxScalableAngle;



        private float timeStartedSprinting = float.NegativeInfinity;
        private bool wasSprinting;

        #endregion

        #region Gravity Setup

        [Header("Gravity")]
        [SerializeField] private float baseGravity;
        private readonly float noGravity = 0f;
        private float currentGravityForce;

        #endregion

        #region Collision Setup

        [Header("Collisions")]
        [SerializeField] private LayerMask groundLayers;
        private float groundCeilingCheckRadius = .4f;
        private Vector3 ceilingCheckOffset = new(0f, .55f, 0f);
        private Vector3 groundCheckOffset = new(0f, -.7f, 0f);


        public LayerMask limits;


        private Vector3 bodyCheckSize = new(1.1f, 1f, 1.1f);


        // already colliding (while the functions are real time checks
        private bool isCollidingDown = false;
        private bool isCollidingUp = false;
        private bool isCollidingRight = false;
        private bool isCollidingLeft = false;
        private bool isCollidingOnAnySide = false;

        private BoxCaster leftCheck;
        private BoxCaster rightCheck;
        private BoxCaster upperLedgeClimbCheck;
        private BoxCaster lowerLedgeClimbCheck;
        private BoxCaster frontStepCheck;
        private BoxCaster backStepCheck;
        private BoxCaster rightStepCheck;
        private BoxCaster leftStepCheck;

        #endregion

        #region Jump Setup

        [Header("Jump")]
        [SerializeField] private float initialJumpSpeedBoost;
        private float JumpForce => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.JumpStats.JumpForce ?? 1080f;
        private float timeLeftGround;
        [SerializeField] private Vector3 longJumpForce;

        [SerializeField] private float terminalVelocity = -75f;

        private readonly float coyoteTimeThreshold = .1f;
        private readonly float jumpBuffer = .1f;
        private bool coyoteUsable;

        private float lastJumpPressed = float.NegativeInfinity;
        private bool CanUseCoyote => coyoteUsable && !isCollidingDown && timeLeftGround + coyoteTimeThreshold > Time.time;
        private bool HasBufferedJump => isCollidingDown && lastJumpPressed + jumpBuffer > Time.time;

        private bool ShouldLongJump => DashUsable && InputQuery.Dash && InputQuery.Forward[InputType.OnKeyHeld];
        private bool forceResetJumping;
        #endregion

        #region Dash Setup

        [Header("Dash")]
        [SerializeField] private float afterDashMomentumConservationWindowDuration;
        private bool InDashMomentumConservationWindow => timeDashEnded + afterDashMomentumConservationWindowDuration > Time.time;
        private float DashVelocity => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.DashStats.DashVelocity ?? 90f;
        private float DashDuration => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.DashStats.DashDuration ?? .1f;
        private float DashCooldown => PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.DashStats.DashCooldown ?? 1f;
        private bool dashOnCooldown;

        private bool dashReady;
        private bool DashUsable => dashReady && !dashOnCooldown && (PlayerFrame.LocalPlayer?.ChampionStats.MovementStats.DashStats.HasDash ?? false);
        private float timeDashTriggered = float.NegativeInfinity;
        private bool ShouldReplenishDash => isCollidingDown || isCollidingLeft || isCollidingRight;
        private float timeDashEnded = float.NegativeInfinity;
        private int dashSide; // -1 left 0 not a side 1 right

        #endregion

        #region Slide Setup

        [Header("Sliding")]
        [SerializeField] private float slideCancelThreshold;
        [SerializeField] private float slideJumpBoostWindow;
        [SerializeField] private float slideSlowdownForce;
        [SerializeField] private float slideSlowdownForceWhenHasDashMomentum;

        [SerializeField] private float slideDownwardForce;
        [SerializeField] private float fallIntoSlideVelocityBoost;

        private float timeFinishedSliding;
        private bool InSlideJumpBoostWindow => timeFinishedSliding + slideJumpBoostWindow > Time.time;

        [SerializeField] private float slideCameraHeightAdjustmentDuration = .3f;

        private readonly float initialColliderHeight = 2f;
        private readonly float slidingColliderHeight = .5f;

        #endregion

        #region Wallrun && WallJump Setup

        [Header("Wallrun")]
        [SerializeField] private float sideWallJumpForce;
        [SerializeField] private float upwardWallJumpForce = 1080f;
        [SerializeField] private float sideWallJumpForceAwayFromWall;
        [SerializeField] private float upwardWallJumpForceAwayFromWall = 900f;
        private bool onRight;
        private float timeInterruptedWallrunWithDash = float.NegativeInfinity;
        [SerializeField] private float wallRunForceCoefficient;
        [SerializeField] private float cantWallRunAfterDashWindow;
        private bool CanWallRunAfterDash => timeInterruptedWallrunWithDash + cantWallRunAfterDashWindow < Time.time;

        #endregion

        #region Ledge Climb Setup

        [Header("Ledge climb")]
        [SerializeField] private float climbDuration = .3f;
        private float timeStoppedSlide = float.NegativeInfinity;
        [SerializeField] private float timeAfterSlidingCanLedgeClimbAgain;
        private bool CanLedgeClimb => timeStoppedSlide + timeAfterSlidingCanLedgeClimbAgain < Time.time;

        #endregion

        #region Camera Handling Setup

        [Header("Camera")]
        [SerializeField] private float maxWallRunCameraTilt = 45f;
        [SerializeField] private float wallRunCameraTiltDuration = .2f;
        public float CurrentWallRunCameraTilt { get; private set; } = 0f;
        [SerializeField] private float timeOnWallBeforeTiltingCamera;
        private float timeStartedWallRunning;
        private bool ShouldStartTiltingCamera => timeStartedWallRunning + timeOnWallBeforeTiltingCamera < Time.time;

        private Transform cameraTransform;
        private Vector3[] cameraTransformPositions = new Vector3[2] { new(0f, .6f, 0f), new(0f, 2f, -5f) };
        private int currentCameraTransformPositionIndex = 0;

        private readonly float cameraTransformBaseHeight = .6f;
        private readonly float cameraTransformSlidingHeight = .3f;

        private Action currentCameraHandlingMethod;

        public float RelevantCameraTiltAngle => currentMovementMode switch
        {
            MovementMode.Run => CurrentDashCameraTiltAngle == 0 ? CurrentRunCameraTiltAngle : CurrentDashCameraTiltAngle,
            MovementMode.Slide => CurrentSlideCameraTiltAngle,
            MovementMode.Wallrun => CurrentWallRunCameraTilt,
            MovementMode.Dash => CurrentDashCameraTiltAngle,
            MovementMode.LedgeClimb => 0f,
            MovementMode.Grappling => 0f,
            _ => 0f
        };


        private bool slideCameraHandlingCoroutineActive;
        private int dashCameraSideHandlingCoroutineActive; // 0 isn t 1 is tilting -1 is regulating

        #endregion

        // actual code 

        #region Collision Handling

        private bool GroundCheck()
        {
            return Physics.CheckSphere(
                transform.position + groundCheckOffset,
                groundCeilingCheckRadius,
                groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private bool CeilingCheck()
        {
            return Physics.CheckSphere(
                transform.position + ceilingCheckOffset,
                groundCeilingCheckRadius,
                groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private bool WallCheckRight()
        {
            return rightCheck.DiscardCast(groundLayers);
        }

        private bool WallCheckLeft()
        {
            return leftCheck.DiscardCast(groundLayers);
        }

        private bool AnySideCheck()
        {
            return Physics.CheckBox(transform.position, bodyCheckSize * .5f, transform.rotation, groundLayers, QueryTriggerInteraction.Ignore);
        }

        private void RunCollisionChecks()
        {
            // Ground
            var grounded = GroundCheck();
            if (isCollidingDown && !grounded) // just jumped
            {
                timeLeftGround = Time.time;
            }
            else if (!isCollidingDown && grounded)
            {
                coyoteUsable = true; // just landed
            }

            // the rest
            isCollidingDown = grounded;
            isCollidingRight = WallCheckRight();
            isCollidingLeft = WallCheckLeft();
            isCollidingUp = CeilingCheck();
            isCollidingOnAnySide = AnySideCheck();
        }

        #endregion

        #region Camera Handling

        private void SwitchCameraPosition()
        {
            cameraTransform.localPosition = cameraTransformPositions[++currentCameraTransformPositionIndex % 2];
        }

        #endregion

        #region PlayerFrameHandling

        //public PlayerFrame PlayerFrame { get; set; }

        //public void InitPlayerFrame(PlayerFrame playerFrame)
        //{
        //    PlayerFrame = playerFrame;
        //}

        #endregion

        #region Updates

        private void Awake()
        {
            inputManager = GetComponent<InputManager>();

            Rigidbody = GetComponent<Rigidbody>();
            cameraTransform = transform.GetChild(0);
            followRotationCamera = cameraTransform.GetComponent<FollowRotationCamera>();
            cameraOriginalPosition = cameraTransform.localPosition;

            #region Assigning Box Casters

            var secondChildTransform = transform.GetChild(1);
            rightCheck = secondChildTransform.GetChild(0).GetComponent<BoxCaster>();
            leftCheck = secondChildTransform.GetChild(1).GetComponent<BoxCaster>();
            upperLedgeClimbCheck = secondChildTransform.GetChild(2).GetComponent<BoxCaster>();
            lowerLedgeClimbCheck = secondChildTransform.GetChild(3).GetComponent<BoxCaster>();
            frontStepCheck = secondChildTransform.GetChild(4).GetComponent<BoxCaster>();
            backStepCheck = secondChildTransform.GetChild(5).GetComponent<BoxCaster>();
            rightStepCheck = secondChildTransform.GetChild(6).GetComponent<BoxCaster>();
            leftStepCheck = secondChildTransform.GetChild(7).GetComponent<BoxCaster>();

            #endregion

            cameraTransform.localPosition = cameraTransformPositions[0];
            SetMovementMode(MovementMode.Run);
        }

        private void Update()
        {
            // kinda nasty but these need to be called each frame for them to work as expected
            // (otherwise the CheckKeyHeld() might never be called and the key might just be seen as pressed forever;
            // this is true for each key with an exoected long lasting effects namely:
            // HoldForTime -> may miss the frame where stopped holding if it was nt checked this frame
            // Toggle -> may misss the frame where toggled it if it was nt checked this frame
            _ = InputQuery.Left[InputType.OnKeyHeldForTime];
            _ = InputQuery.Right[InputType.OnKeyHeldForTime];
            _ = InputQuery.QuickReset;
            _ = InputQuery.Slide[InputType.OnKeyHeld];
            _ = InputQuery.HoldCrouch;

            currentMovementMethod();
            currentCameraHandlingMethod();
            TryReplenishDash();

            if (InputQuery.SwitchCameraPosition) { SwitchCameraPosition(); }

            LocalVelocityDebug = new(transform.InverseTransformDirection(Rigidbody.velocity));
            GlobalVelocityDebug = new(Rigidbody.velocity);
            CollisionDebug = new(isCollidingUp, isCollidingDown, isCollidingRight, isCollidingLeft, isCollidingOnAnySide);

            HandleWhenAtBottomOfMap();
        }

        private void FixedUpdate()
        {
            RunCollisionChecks();
        }

        private void HandleWhenAtBottomOfMap()
        {
            // do a layer per level instead with a scriptable object LevelInfo holding all relevant y
            if (transform.position.y < -50f)
            {
                transform.position = Vector3.up * 100f;
            }
        }

        #endregion

        #region General Movement

        private void ApplyGravity()
        {
            Rigidbody.AddForce(currentGravityForce * Time.deltaTime * -transform.up, ForceMode.Force);
            if (Rigidbody.velocity.y < terminalVelocity)
            {
                Rigidbody.velocity = new(Rigidbody.velocity.x, terminalVelocity, Rigidbody.velocity.y);
            }
        }

        public void SetMovementMode(MovementMode movementMode)
        {

            switch (currentMovementMode = movementMode)
            {
                case MovementMode.Run:
                    currentGravityForce = baseGravity;
                    currentMovementMethod = HandleRun;
                    currentCameraHandlingMethod = HandleRunCamera;
                    break;

                case MovementMode.Slide:
                    currentGravityForce = baseGravity;
                    currentMovementMethod = HandleSlide;
                    currentCameraHandlingMethod = HandleSlideCamera;
                    break;

                case MovementMode.Wallrun:
                    currentGravityForce = noGravity;
                    currentMovementMethod = HandleWallrun;
                    currentCameraHandlingMethod = () => { };
                    break;

                case MovementMode.Dash:
                    currentGravityForce = noGravity;
                    currentMovementMethod = HandleDash;
                    currentCameraHandlingMethod = HandleDashCamera;
                    break;

                case MovementMode.LedgeClimb:
                    currentGravityForce = noGravity;
                    currentMovementMethod = HandleLedgeClimb;
                    currentCameraHandlingMethod = () => { };
                    break;

                case MovementMode.Grappling:
                    currentGravityForce = noGravity;
                    currentMovementMethod = HandleGrappling;
                    currentCameraHandlingMethod = () => { };
                    break;
            }

#if LOG_MOVEMENTS_EVENTS
        print(movementMode);
#endif
        }

        #region Action Buffering 

        private void BufferAction(MovementAction action, MovementActionParam param)
        {
            StartCoroutine(TriggerBufferAction(action, param));
        }

        private IEnumerator TriggerBufferAction(MovementAction action, MovementActionParam param)
        {
            var startTime = Time.time;

            bufferedActions.Add(new(action, param));

            yield return new WaitUntil(() => startTime + validatedActionBufferDuration < Time.time || bufferedActions.Count > 1);

            _ = bufferedActions.Select(selector: (actionData) => PerformActions(actionData.Action, actionData.Param));

            bufferedActions.Clear();
        }

        private object PerformActions(MovementAction action, MovementActionParam param)
        {
            Action<MovementActionParam> relevantMethod = action switch
            {
                MovementAction.Jump => JumpAction,
                MovementAction.Slide => SlideAction,
                MovementAction.Dash => DashAction,
                _ => (_) => { }
            };

            relevantMethod(param);

            return null;
        }

        #region Ended up not using  binary flags

        private bool HasOneBit(int number)
        {
            while (number > 0)
            {
                if (number % 2 == 1)
                {
                    return true;
                }
                number /= 2;
            }

            return false;
        }

        private int CountOneBitsInNumber(int number)
        {
            var cnt = 0;

            while (number > 0)
            {
                cnt += number % 2;
                number /= 2;
            }

            return cnt;
        }

        #endregion

        #endregion

        #region Velocity Alteration

        private void ApplySlowdown(float slowdownForce)
        {
            var velocity = Rigidbody.velocity.normalized;
            ApplyBoost(-slowdownForce);
            if (Rigidbody.velocity.normalized != velocity)
            {
                Rigidbody.velocity = Rigidbody.velocity.Mask(0f, 1f, 0f);
            }
        }

        private void ApplyBoost(float boostForce)
        {
            // Rigidbody.velocity.Mask(1f, 0f, 1f).normalized -> gets the velocity while ignoring verticality
            Rigidbody.AddForce(boostForce * Time.deltaTime * Rigidbody.velocity.Mask(1f, 0f, 1f).normalized, ForceMode.Force);
        }

        private void ApplySlowdownInstant(float slowdownForce)
        {
            ApplyBoostInstant(-slowdownForce);
        }

        private void ApplyBoostInstant(float boostForce)
        {
            // Rigidbody.velocity.Mask(1f, 0f, 1f).normalized -> gets the velocity while ignoring verticality
            Rigidbody.AddForce(boostForce * Rigidbody.velocity.Mask(1f, 0f, 1f).normalized, ForceMode.Impulse);
        }

        public void ResetYVelocity()
        {
            SetYVelocity(0f);
        }

        public void SetYVelocity(float newY)
        {
            Rigidbody.velocity = new(Rigidbody.velocity.x, newY, Rigidbody.velocity.z);
        }

        #endregion

        #endregion

        #region Jump

        /// <summary>
        /// Returns wether a jump should happen and wether it should be a full one
        /// </summary>
        /// <param name="coyoteThresholdAllowed"></param>
        /// <returns></returns>
        private (bool, bool) ShouldJump(bool coyoteThresholdAllowed)
        {
            if (IsJumping) { return (false, false); }

            if (InputQuery.Jump[InputType.OnKeyDown])
            {
                lastJumpPressed = Time.time; // for jump buffer

                if (CanUseCoyote && coyoteThresholdAllowed || isCollidingDown)
                {
                    //Jump(true);
                    return (true, true);
                }
            }
            else if (HasBufferedJump)
            {
                //Jump(inputQuery.HoldJump);
                return (true, InputQuery.Jump[InputType.OnKeyHeld]);
            }

            return (false, false);
        }

        private void CommonJumpStart()
        {
#if LOG_MOVEMENTS_EVENTS
        print("Jumped");
#endif

            UnCrouch();
            coyoteUsable = false;
            IsJumping = true;
            forceResetJumping = false;

            ResetYVelocity();

            if (InputQuery.Forward[InputType.OnKeyHeld])
            {
                Rigidbody.AddForce(initialJumpSpeedBoost * transform.forward, ForceMode.Impulse);
            }
        }

        private void Jump(bool fullJump, bool longJump)
        {
            CommonJumpStart();

            if (longJump)
            {
                Rigidbody.AddForce(longJumpForce, ForceMode.Impulse);
            }
            else
            {
                Rigidbody.AddForce((fullJump ? JumpForce : JumpForce / 2) * Vector3.up, ForceMode.Impulse);
            }


            StartCoroutine(ResetJumping());
        }

        private void JumpAction(MovementActionParam param)
        {
            var jumpActionParam = (JumpActionParam)param;

            Jump(jumpActionParam.FullJump, jumpActionParam.LongJump);
        }

        private void WallJump(bool towardRight, bool awayFromWall)
        {
            CommonJumpStart();

            var rawForce = transform.right * (awayFromWall ? sideWallJumpForceAwayFromWall : sideWallJumpForce);
            var forceWithRelevantSide = towardRight ? rawForce : -rawForce;
            var verticalForce = transform.up * (awayFromWall ? upwardWallJumpForceAwayFromWall : upwardWallJumpForce);
            Rigidbody.AddForce(forceWithRelevantSide + verticalForce, ForceMode.Impulse);

            StartCoroutine(ResetJumping());
        }

        private IEnumerator ResetJumping()
        {
            yield return new WaitUntil(
                    () => Rigidbody.velocity.y < 0f || forceResetJumping
                );

            //print($"Stopped jumping {Rigidbody.velocity.y < 0f} || {forceResetJumping}");

            forceResetJumping = false;
            IsJumping = false;
        }

        #endregion

        #region Run

        private void HandleRun()
        {
            if (!isCollidingDown)
            {
                ApplyGravity();
            }

            Run();
            CheckStep();

            var (shouldJump, shouldBeFullJump) = ShouldJump(true);
            if (shouldJump)
            {
                Jump(shouldBeFullJump, ShouldLongJump);
                return;
            }

            if (DashUsable && InputQuery.Dash)
            {
                StartCoroutine(Dash());
                return;
            }

            if (InputQuery.Slide[InputType.OnKeyDown] && CurrentSpeed != 0f) // HoldSlide or InitiateSlide ? -> run some tests
            {
                StartCoroutine(Slide()); // add some kind of coyote threshold where the velocity is conserved even tho the player walked a bit (which should kill his momentum)
                return;
            }

            if (!isCollidingDown)
            {
                var sideToWallRunOn = MyInput.GetAxis(InputQuery.Left[InputType.OnKeyHeld] && isCollidingLeft, InputQuery.Right[InputType.OnKeyHeld] && isCollidingRight);
                if (sideToWallRunOn != 0f)
                {
                    StartCoroutine(Wallrun(sideToWallRunOn));
                    return;
                }
            }

            if (CheckLedgeClimb(out var ledges))
            {
                LedgeClimb(ledges);
                return;
            }

            HandleCrouch(InputQuery.HoldCrouch && isCollidingDown);
        }

        private void Run()
        {
            var wantedMoveVec = new Vector2(SidewayAxisInput, ForwardAxisInput).normalized;

            var targetSpeed = CalculateTargetSpeed(wantedMoveVec) - (isCollidingDown ? 0f : .5f);

            var velocityY = Rigidbody.velocity.y;

            var localVelocity = transform.InverseTransformDirection(Rigidbody.velocity);

            if (wantedMoveVec.x == 0f)
            {
                localVelocity = localVelocity.Mask(sidewayInertiaControlFactor, 0f, 1f);
            }
            else if (Mathf.Sign(wantedMoveVec.x) != Mathf.Sign(localVelocity.x))
            {
                localVelocity = localVelocity.Mask(0f, 0f, 1f);
            }

            if (Mathf.Sign(wantedMoveVec.y) != Mathf.Sign(localVelocity.z))
            {
                localVelocity = localVelocity.Mask(1f, 0f, 0f);
            }

            Rigidbody.velocity = transform.TransformDirection(localVelocity);

            // DumbShit()
            //{
            //Rigidbody.velocity = transform.TransformDirection(transform.InverseTransformDirection(Rigidbody.velocity).Mask(wantedMoveVec.x == 0f ? sidewayInertiaControlFactor : (Mathf.Sign(wantedMoveVec.x) != Mathf.Sign(transform.InverseTransformDirection(Rigidbody.velocity).x)) ? 0f : 1f, 0f, Mathf.Sign(wantedMoveVec.y) != Mathf.Sign(transform.InverseTransformDirection(Rigidbody.velocity).z) ? 0f : 1f));
            //}


            var direction = transform.forward * wantedMoveVec.y + transform.right * wantedMoveVec.x;
            if (isCollidingDown)
            {
                if (IsSprinting)
                {
                    float timeElapsedSinceStartedSprinting;
                    if (!wasSprinting)
                    {
                        timeStartedSprinting = Time.time;
                        wasSprinting = true;
                        timeElapsedSinceStartedSprinting = 0f;
                    }
                    else
                    {
                        timeElapsedSinceStartedSprinting = Time.time - timeStartedSprinting;
                    }


                    float t = Mathf.Clamp01(timeElapsedSinceStartedSprinting / timeToReachMaxSprintSpeed); // if failing timeToReachMaxSprintSpeed probably reset to 0 in the inspector
                    float currentSpeedRatio = Interpolation.ExponentialLerp(t);

                    Rigidbody.velocity = Vector3.Slerp(direction * CroouchingSpeed, targetSpeed * direction, currentSpeedRatio);
                }
                else
                {
                    wasSprinting = false;
                    currentSpeedRatio = 0f;
                    Rigidbody.velocity = targetSpeed * direction; // else walking so let him go max "speed"
                }
            }
            else
            {
                float reductionAmount = airFriction * CurrentSpeed * CurrentSpeed * Time.deltaTime;
                //var lerpedValue = Mathf.Lerp(CurrentSpeed, 0, airFriction * Time.deltaTime);
                //print(lerpedValue);
                Rigidbody.velocity = Mathf.Max(CurrentSpeed - reductionAmount, targetSpeed) * direction;
            }

            SetYVelocity(velocityY < terminalVelocity ? terminalVelocity : velocityY);
        }

        private float CalculateTargetSpeed(Vector2 wantedMoveVec)
        {
            if (wantedMoveVec.x == 0f)
            {
                if (wantedMoveVec.y == 0f)
                {
                    return 0;
                }
                else
                {
                    return
                            wantedMoveVec.y < 0f ?
                                BackwardSpeed
                                :
                                IsSprinting ?
                                    RunningSpeed
                                    :
                                    CroouchingSpeed;
                }
            }
            else
            {
                return StrafingSpeedCoefficient * (
                    wantedMoveVec.y < 0f ?
                        BackwardSpeed
                        :
                        IsSprinting ?
                            RunningSpeed
                            :
                            CroouchingSpeed
                        );
            }
            //return wantedMoveVec.x == 0f ? wantedMoveVec.y == 0f ? 0f : wantedMoveVec.y < 0f ? BackwardSpeed : RunningSpeed : wantedMoveVec.y < 0f ? BackwardSpeed : StrafingSpeed;
        }

        #region Step

        private void CheckStep()
        {
            var currentDirection = DirectionUtility.GetDirectionFromAxises(ForwardAxisInput, SidewayAxisInput);

            var hasSthToStepOn = false;
            var colliders = new List<Collider>();
            switch (currentDirection)
            {
                case Direction.Forward:
                    hasSthToStepOn = frontStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.ForwardRight:
                    hasSthToStepOn = frontStepCheck.AddCast(groundLayers, ref colliders);
                    hasSthToStepOn |= rightStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.Right:
                    hasSthToStepOn = rightStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.BackwardRight:
                    hasSthToStepOn = backStepCheck.AddCast(groundLayers, ref colliders);
                    hasSthToStepOn |= rightStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.Backward:
                    hasSthToStepOn = backStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.BackwardLeft:
                    hasSthToStepOn = backStepCheck.AddCast(groundLayers, ref colliders);
                    hasSthToStepOn |= leftStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.Left:
                    hasSthToStepOn = leftStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.ForwardLeft:
                    hasSthToStepOn = frontStepCheck.AddCast(groundLayers, ref colliders);
                    hasSthToStepOn |= leftStepCheck.AddCast(groundLayers, ref colliders);
                    break;

                case Direction.None:
                    break;

                default:
                    break;
            }

            if (!hasSthToStepOn) { return; }

            var relevantColliders = colliders.Where(predicate: (collider) => GetHighestPointOffCollider(collider).y <= FeetPosition.y + maxStepHeight).ToList();

            if (relevantColliders.Count == 0) { return; }

            Collider stepToTake = null;
            var highestPointSoFar = float.NegativeInfinity;

            foreach (var collider in relevantColliders)
            {
                var currentHeight = GetHighestPointOffCollider(collider).y;
                if (currentHeight > highestPointSoFar)
                {
                    highestPointSoFar = currentHeight;
                    stepToTake = collider;
                }
            }

            var relevantXZ = stepToTake.ClosestPoint(transform.position).Mask(1f, 0f, 1f);
            transform.position = relevantXZ + Vector3.up * (GetHighestPointOffCollider(stepToTake).y + 1);
        }

        private Vector3 GetHighestPointOffCollider(Collider collider) // update this for it to work with slanted ground too
        {
            if (collider.gameObject.TryGetComponent<MeshFilter>(out var meshFilterComponent))
            {
                var colliderTransform = collider.transform;
                var colliderOrigin = colliderTransform.position;
                var colliderWorldScale = colliderTransform.lossyScale;

                var verticesCount = meshFilterComponent.mesh.vertexCount;

                var vertices = meshFilterComponent.mesh.vertices; // vertices of the mesh in local space
                var verticesInWorldSpace = vertices.Select(selector: (vertice) => colliderOrigin + vertice.Mask(colliderWorldScale)).ToArray();

                var highestPointSoFar = float.NegativeInfinity * Vector3.up;

                for (int vertex = 0; vertex < verticesCount; vertex++)
                {
                    if (verticesInWorldSpace[vertex].y > highestPointSoFar.y)
                    {
                        highestPointSoFar = verticesInWorldSpace[vertex];
                    }
                }

                //print($"Max: {highestPointSoFar.y}");
                return highestPointSoFar;
            }
            else
            {
                // if it s not rendered then it must be some invisible barrier (which I don t plan on having)
                // anyway: should not be climbed
                print("It failed");
                return Vector3.up * float.PositiveInfinity;
            }
        }

        private Vector3 GetHighestPointOffCollider_(Collider collider) // update this for it to work with slanted ground too
        {
            // raycasts from above
            // can determine the slope
            // assume it s flat (as in not curved not just not slanted)
            throw new NotImplementedException();

        }

        private Vector3 GetHighestPointOffNonRotatedCollider(Collider collider)
        {
            var transform_ = collider.transform;
            return transform_.position + .5f * transform_.lossyScale.y * Vector3.up;
        }

        #endregion

        #region Crouch

        private void HandleCrouch(bool doIt)
        {
            if (doIt)
            {
                Crouch();
            }
            else
            {
                UnCrouch();
            }
        }

        private void Crouch()
        {
            if (IsCrouching) { return; }

#if LOG_MOVEMENTS_EVENTS
        print("Crouch");
#endif

            IsCrouching = true;
            transform.localScale = transform.localScale.Mask(1f, .5f, 1f);
            transform.position -= Vector3.up * .5f;
        }

        private void UnCrouch()
        {
            if (!IsCrouching) { return; }

#if LOG_MOVEMENTS_EVENTS
        print("Uncrouch");
#endif

            IsCrouching = false;
            transform.position += Vector3.up * .5f;
            transform.localScale = transform.localScale.Mask(1f, 2f, 1f);
        }


        #endregion

        #endregion

        #region Slide

        private void HandleSlide()
        {
            ApplyGravity();
        }

        private IEnumerator Slide()
        {
            if (IsSliding) { yield break; } // if somehow the player managed to get there when already sliding

            var shouldAwardVelocityBoostForFalling = !isCollidingDown;

            yield return new WaitUntil(() => isCollidingDown || !InputQuery.Slide[InputType.OnKeyHeld]); // await the landing to initiate the slide

            if (!InputQuery.Slide[InputType.OnKeyHeld]) { yield break; } // if changed his mind

            SetMovementMode(MovementMode.Slide);
            transform.localScale = transform.localScale.Mask(1f, .5f, 1f);
            transform.position -= Vector3.up * .5f;

            var benefitedFromDashMomentum = InDashMomentumConservationWindow;
            if (benefitedFromDashMomentum)
            {
                print("Got it");
                var verticalVelocity = Rigidbody.velocity.y;
                Rigidbody.velocity = DashVelocity * .5f * Rigidbody.velocity.Mask(1f, 0f, 1f).normalized + transform.up * verticalVelocity;
            }
            else if (shouldAwardVelocityBoostForFalling)
            {
                ApplyBoost(fallIntoSlideVelocityBoost);
            }

            var dashed = false;
            var triedJumping = false;
            var wouldVeBeenFullJump = false;

            yield return new WaitUntil(
                () =>
                    {
                        if (isCollidingDown)
                        {
                            // like some extra gravity so the player can slide along steeper slopes
                            Rigidbody.AddForce(slideDownwardForce * Time.deltaTime * -transform.up, ForceMode.Force);
                        }

                        ApplySlowdown(benefitedFromDashMomentum && CurrentSpeed > RunningSpeed ? slideSlowdownForceWhenHasDashMomentum : slideSlowdownForce);

                        if (DashUsable && InputQuery.Dash)
                        {
                            dashed = true;
                            return true;
                        }

                        (triedJumping, wouldVeBeenFullJump) = ShouldJump(true);

                        return
                            Rigidbody.velocity.magnitude < slideCancelThreshold ||
                            !InputQuery.Slide[InputType.OnKeyHeld] ||
                            triedJumping
                            ;
                    }
            );

            if (triedJumping)
            {
                Jump(wouldVeBeenFullJump, dashed && InputQuery.Forward[InputType.OnKeyHeld]);
                CommonSlideExit(MovementMode.Run);
                yield break;
            }

            if (dashed)
            {
                CommonSlideExit();
                StartCoroutine(Dash());
                yield break;
            }

            CommonSlideExit(MovementMode.Run);
        }

        private void SlideAction(MovementActionParam __)
        {
            _ = StartCoroutine(Slide());
        }

        private void CommonSlideExit(MovementMode newMovementMode)
        {
            CommonSlideExit();
            SetMovementMode(newMovementMode);
        }

        private void CommonSlideExit()
        {
            transform.position += Vector3.up * .5f;
            transform.localScale = transform.localScale.Mask(1f, 2f, 1f);
            timeStoppedSlide = Time.time;
        }

        private IEnumerator LerpCameraHeight(bool isReset)
        {
            var startingPoint = cameraTransform.localPosition.y;
            var goal = isReset ? cameraTransformBaseHeight : cameraTransformSlidingHeight;
            var (lowerBound, upperBound) = startingPoint < goal ? (startingPoint, goal) : (goal, startingPoint);
            var elapsedTime = 0f;
            while (elapsedTime < slideCameraHeightAdjustmentDuration)
            {
                cameraTransform.localPosition = new(cameraTransform.localPosition.x, Mathf.Lerp(lowerBound, upperBound, elapsedTime / slideCameraHeightAdjustmentDuration), cameraTransform.localPosition.z);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        #endregion

        #region Dash

        private void HandleDash() { }

        private IEnumerator Dash()
        {
            if (IsDashing) { yield break; }

            dashSide = SidewayAxisInput;
            SetMovementMode(MovementMode.Dash);
            dashReady = false;

            forceResetJumping = true;

            var slid = false;
            timeDashTriggered = Time.time;
            followRotationCamera.enabled = false;
            //print($"dashSide: {dashSide}");
            var dir = cameraTransform.TransformDirection(dashSide, 0f, ForwardAxisInput);

            dir = dir == Vector3.zero ? cameraTransform.forward : dir;


            yield return new WaitUntil(
                () =>
                    {
                        //Rigidbody.velocity = dashVelocity * cameraTransform.forward; // perhaps do sth less brutal with gradual velocity loss
                        Rigidbody.velocity = DashVelocity * dir; // perhaps do sth less brutal with gradual velocity loss

                        if (InputQuery.Slide[InputType.OnKeyDown] && isCollidingDown) // if slide during the dash then the boost is applied // here it s most likely in the dash (at most 1 frame off so take it as a lil gift :) )
                        {
                            slid = true;
                            return true;
                        }

                        return timeDashTriggered + DashDuration < Time.time;
                    }
            );

            if (slid)
            {
                CommonDashExit();
                StartCoroutine(Slide());
                yield break;
            }

            if (!isCollidingDown)
            {
                var shouldWallrunLeftRight = MyInput.GetAxis(InputQuery.Left[InputType.OnKeyHeld] && isCollidingLeft, InputQuery.Right[InputType.OnKeyHeld] && isCollidingRight);
                if (shouldWallrunLeftRight != 0f)
                {
                    CommonDashExit(MovementMode.Wallrun);
                    StartCoroutine(Wallrun(shouldWallrunLeftRight));
                }
            }

            CommonDashExit(MovementMode.Run);
        }

        private void DashAction(MovementActionParam __)
        {
            _ = StartCoroutine(Dash());
        }

        private void CommonDashExit(MovementMode newMovementMode)
        {
            followRotationCamera.enabled = true;
            timeDashEnded = Time.time;
            ResetYVelocity();
            SetMovementMode(newMovementMode);
            StartCoroutine(StartDashCooldown());
        }

        private void CommonDashExit()
        {
            followRotationCamera.enabled = true;
            timeDashEnded = Time.time;
            ResetYVelocity();
            StartCoroutine(StartDashCooldown());
        }

        private IEnumerator StartDashCooldown()
        {
            dashOnCooldown = true;

            yield return new WaitForSeconds(DashCooldown);

            dashOnCooldown = false;
        }

        private void TryReplenishDash()
        {
            dashReady |= ShouldReplenishDash;
        }

        #endregion

        #region Ledge Climb

        private void HandleLedgeClimb() { }

        private bool CheckLedgeClimb(out Collider[] ledges)
        {
            if (!CanLedgeClimb || InputQuery.Back[InputType.OnKeyHeld] || !InputQuery.Forward[InputType.OnKeyHeld] && !InputQuery.Jump[InputType.OnKeyDown])
            {
                ledges = new Collider[0];
                return false;
            }

            if (lowerLedgeClimbCheck.ReturnCast(groundLayers, out Collider[] lowerHit) && !upperLedgeClimbCheck.DiscardCast(groundLayers) && Rigidbody.velocity.y < 0f)
            {
                ledges = lowerHit;
                return true;
            }

            ledges = new Collider[0];
            return false;
        }

        private void LedgeClimb(Collider[] ledges)
        {
            StartCoroutine(ClimbLedge(CalculateLedgePosition(ledges)));
        }

        private IEnumerator ClimbLedge(Vector3 ledgePosition) // on slanted ground the lower ledge climb check can collide and not the upper so fix that (test buffing the upper cast width)
        {
            SetMovementMode(MovementMode.LedgeClimb);
            Rigidbody.velocity = Vector3.zero;

            var elapsedTime = 0f;
            Vector3 startPosition = transform.position;

            while (elapsedTime < climbDuration)
            {
                transform.position = Vector3.Lerp(startPosition, ledgePosition, elapsedTime / climbDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            SetMovementMode(MovementMode.Run);
        }

        private Vector3 CalculateLedgePosition(Collider[] colliders)
        {
            var ledgeY = float.MinValue;
            Collider relevantCollider = null;
            foreach (var collider in colliders)
            {
                var highestPointOfCollider = GetHighestPointOffCollider(collider).y;
                if (ledgeY < highestPointOfCollider) // get the highest scalable surface's height
                {
                    ledgeY = highestPointOfCollider;
                    relevantCollider = collider;
                }
            }
            // position so far is the top of the collider but only from its center
            var relevantXZ = relevantCollider.ClosestPoint(transform.position);
            return new Vector3(relevantXZ.x, ledgeY, relevantXZ.z) + transform.up;
            // transform.up being half the player size for it to land on top as it s position is its center

        }

        #endregion

        #region Wall Run

        private void HandleWallrun() { }

        private IEnumerator Wallrun(int side)
        {
            if (!InputQuery.Forward[InputType.OnKeyHeld] || InputQuery.Back[InputType.OnKeyHeld] || !CanWallRunAfterDash) { yield break; }

            SetMovementMode(MovementMode.Wallrun);
            ResetYVelocity();

            onRight = side > 0;
            var forceDirection = onRight ? transform.right : -transform.right;

            timeStartedWallRunning = Time.time;
            var startedTiltingCamera = false;
            InputQuery.Left.ResetHeldSince(); // avoid insta quitting the wallrun due to this triggering while you held it to jump
            InputQuery.Right.ResetHeldSince(); // same but other side :)

            var leftEarly = false;
            var directionAlongWall = (transform.forward - Vector3.Dot(transform.forward, forceDirection) * forceDirection).normalized;
            yield return new WaitUntil(
                () =>
                {
                    if (!startedTiltingCamera && ShouldStartTiltingCamera)
                    {
                        StartCoroutine(SetCameraTilt(onRight, false));
                    }

                    Rigidbody.AddForce(wallRunForceCoefficient * WallRunSpeed * Time.deltaTime * directionAlongWall, ForceMode.Force);
                    Rigidbody.velocity = Vector3.ClampMagnitude(Rigidbody.velocity.Mask(1f, 0f, 1f), WallRunSpeed);

                    if (InputQuery.Jump[InputType.OnKeyDown])
                    {
                        CommonWallRunExit(MovementMode.Run, onRight);
                        WallJump(!onRight, onRight ? InputQuery.Left[InputType.OnKeyHeld] : InputQuery.Right[InputType.OnKeyHeld]);
                        leftEarly = true;
                        return true;
                    }

                    if (dashReady && InputQuery.Dash)
                    {
                        CommonWallRunExit(MovementMode.Dash, onRight);
                        StartCoroutine(Dash());
                        leftEarly = true;
                        return true;
                    }


                    if (CheckLedgeClimb(out var ledgesEncountered))
                    {
                        CommonWallRunExit(MovementMode.LedgeClimb, onRight);
                        LedgeClimb(ledgesEncountered);
                        leftEarly = true;
                        return true;
                    }

                    if (InputQuery.Slide[InputType.OnKeyDown])
                    {
                        CommonWallRunExit(MovementMode.Slide, onRight);
                        StartCoroutine(Slide());
                        leftEarly = true;
                        return true;
                    }

                    if (!isCollidingOnAnySide) { return true; } // out of the final return bc didn t work for reasons that are beyond me

                    return
                        InputQuery.Back[InputType.OnKeyHeld] ||
                        !InputQuery.Forward[InputType.OnKeyHeld] ||
                        onRight ? InputQuery.Left[InputType.OnKeyHeldForTime] : InputQuery.Right[InputType.OnKeyHeldForTime]
                        ;
                }
            );

            if (leftEarly) { yield break; }

            CommonWallRunExit(MovementMode.Run, onRight);

        }

        private void CommonWallRunExit(MovementMode newMovementMode, bool onRight)
        {
            timeStartedWallRunning = float.PositiveInfinity;
            StartCoroutine(SetCameraTilt(onRight, true));
            SetMovementMode(newMovementMode);
        }

        private IEnumerator SetCameraTilt(bool onRightWall, bool isReset)
        {
            if (CurrentWallRunCameraTilt == 0 && isReset) { yield break; }

            var startingPoint = CurrentWallRunCameraTilt;
            var elapsedTime = 0f;
            while (elapsedTime < wallRunCameraTiltDuration)
            {
                CurrentWallRunCameraTilt = isReset ?
                    Mathf.Lerp(startingPoint, 0f, elapsedTime / wallRunCameraTiltDuration)
                    :
                    Mathf.Lerp(startingPoint, onRightWall ? maxWallRunCameraTilt : -maxWallRunCameraTilt, elapsedTime / wallRunCameraTiltDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
        }

        #endregion

        #region Grapplink Hook

        private void HandleGrappling() { }

        #endregion

        #region Camera Handling

        #region View Bobbing

        [Header("Bobbing Settings")]
        [SerializeField] private float maxViewBobbingSpeed; // Speed of the bobbing motion
        [SerializeField] private float maxViewBobbingDepth; // Amount of bobbing motion
        private Vector3 cameraOriginalPosition; // Original camera position

        [SerializeField] private float speedThresholdToTriggerViewBobbing;
        private float SpeedThresholdToTriggerViewBobbing => RunningSpeed - RunningSpeed / 3; // completely & purely arbitrary
        [SerializeField] private float timeToRegulateBobbingOffset;

        [SerializeField] private float speedThresholdToReachMaxViewBobbingIntensity;
        private float RelevantParamForBobbingIntensity => Mathf.Min(CurrentSpeed, speedThresholdToReachMaxViewBobbingIntensity);
        private float BobbingSpeed => Mathf.Lerp(0f, maxViewBobbingSpeed, RelevantParamForBobbingIntensity / speedThresholdToReachMaxViewBobbingIntensity);
        private float BobbingDepth => Mathf.Lerp(0f, maxViewBobbingDepth, RelevantParamForBobbingIntensity / speedThresholdToReachMaxViewBobbingIntensity);
        private bool isBobbing;

        private IEnumerator TriggerViewBobbing()
        {
            var viewBobbingProgress = 0f; // this name kinda sucks but idk what to name it

            isBobbing = true;
            for (; isCollidingDown && currentMovementMode == MovementMode.Run && CurrentSpeed > speedThresholdToTriggerViewBobbing;) // the geneva convention is not safe
            {
                var verticalOffset = Mathf.Sin(viewBobbingProgress) * BobbingDepth;

                cameraTransform.localPosition = cameraOriginalPosition + new Vector3(0f, verticalOffset, 0f);

                viewBobbingProgress += BobbingSpeed * Time.deltaTime;

                yield return null;
            }

            StartCoroutine(ResetBobbingOffset());
        }

        private IEnumerator ResetBobbingOffset()
        {
            isBobbing = false;
            var elapsed = 0f;
            var startingPoint = cameraTransform.localPosition;
            for (; elapsed < timeToRegulateBobbingOffset && !isBobbing;)
            {
                elapsed += Time.deltaTime;
                cameraTransform.localPosition = Vector3.Lerp(startingPoint, cameraOriginalPosition, elapsed / timeToRegulateBobbingOffset);

                yield return null;
            }
        }

        #endregion

        [Header("Camera Tilt")]
        [SerializeField] private float cameraTiltLeniencyToExtent;

        #region Run Camera Tilt

        [Header("Run Camera Tilt")]
        [SerializeField] private float maxRunCameraTiltAngle;
        [SerializeField] private float maxRunCameraTiltSpeed;
        // this name sucks but basically
        // the leniency to the clamp (maxCameraTilt / noCameraTilt) so the lerp doesn t run forever and at some point
        // (when the difference currentCameraTilt -> extent < leniency) the value snaps to the extent
        private float currentRunCameraTiltAngle;
        public float CurrentRunCameraTiltAngle
        {
            get
            {
                return currentRunCameraTiltAngle;
            }
            set
            {
                if (Mathf.Abs(value - maxRunCameraTiltAngle) < cameraTiltLeniencyToExtent && TargetRunCameraTiltAngle != 0)
                {
                    currentRunCameraTiltAngle = maxRunCameraTiltAngle * Mathf.Sign(value);
                    return;
                }

                if (Mathf.Abs(value) < cameraTiltLeniencyToExtent && TargetRunCameraTiltAngle == 0)
                {
                    currentRunCameraTiltAngle = 0;
                    return;
                }

                currentRunCameraTiltAngle = value;
            }
        }

        //private float TargetRunCameraTiltAngle => MyInput.GetAxis(inputQuery.Right, inputQuery.Left) * maxRunCameraTiltAngle;
        private float TargetRunCameraTiltAngle => MyInput.GetAxis(InputQuery.Right[InputType.OnKeyHeld] && !isCollidingRight, InputQuery.Left[InputType.OnKeyHeld] & !isCollidingLeft) * maxRunCameraTiltAngle;
        //
        //private float TargetRunCameraTiltAngle => Rigidbody != null ? CurrentStrafeSpeed / Mathf.Abs(CurrentStrafeSpeed) * maxRunCameraTiltAngle : 0f;
        // as dir in {-1, 0, 1} dir * maxRunCameraTiltAngle in {-maxRunCameraTiltAngle, 0 (regulateCameraTilt), maxRunCameraTiltAngle}

        [SerializeField] private float runCameraTiltRegulationSpeed;


        private void HandleRunCamera()
        {
            if (!isBobbing)
            {
                StartCoroutine(TriggerViewBobbing());
            }

            HandleRunCameraTilt();
        }

        private void HandleRunCameraTilt()
        {
            var targetAngle = TargetRunCameraTiltAngle;
            CurrentRunCameraTiltAngle = Mathf.Lerp(CurrentRunCameraTiltAngle, targetAngle, (targetAngle == 0 ? runCameraTiltRegulationSpeed : maxRunCameraTiltSpeed) * Time.deltaTime);
        }

        #endregion

        #region Slide Camera Tilt

        [Header("Slide Camera Tilt")]
        [SerializeField] private bool slideTiltOnRight;
        [SerializeField] private float maxSlideCameraTiltAngle;
        [SerializeField] private float slideCameraTiltSpeed;
        // this name sucks but basically
        // the leniency to the clamp (maxCameraTilt / noCameraTilt) so the lerp doesn t run forever and at some point
        // (when the difference currentCameraTilt -> extent < leniency) the value snaps to the extent
        private float currentSlideCameraTiltAngle;
        public float CurrentSlideCameraTiltAngle
        {
            get
            {
                return currentSlideCameraTiltAngle;
            }
            set
            {
                if (Mathf.Abs(value - maxSlideCameraTiltAngle) < cameraTiltLeniencyToExtent && TargetSlideCameraTiltAngle != 0)
                {
                    currentSlideCameraTiltAngle = maxSlideCameraTiltAngle * Mathf.Sign(value);
                    return;
                }

                if (Mathf.Abs(value) < cameraTiltLeniencyToExtent && TargetSlideCameraTiltAngle == 0)
                {
                    currentSlideCameraTiltAngle = 0;
                    return;
                }

                currentSlideCameraTiltAngle = value;
            }
        }

        private float TargetSlideCameraTiltAngle => maxSlideCameraTiltAngle * (slideTiltOnRight ? -1f : 1f);
        // as dir in {-1, 0, 1} dir * maxRunCameraTiltAngle in {-maxRunCameraTiltAngle, 0 (regulateCameraTilt), maxRunCameraTiltAngle}

        [SerializeField] private float slideCameraTiltRegulationSpeed;
        [SerializeField] private AnimationCurve dashCameraTiltCurve;


        private void HandleSlideCamera()
        {
            if (!slideCameraHandlingCoroutineActive)
            {
                var localVelocity = transform.InverseTransformDirection(Rigidbody.velocity);
                StartCoroutine(
                    HandleSlideCameraCoroutine(
                        localVelocity.x == 0f ?
                        TargetSlideCameraTiltAngle : // going forward -> choose side according to settings
                        maxSlideCameraTiltAngle * (localVelocity.x < 0f ? -1f : 1f) // at least slightly sideway choose side according to that
                    )
                );
            }
        }


        private IEnumerator HandleSlideCameraCoroutine(float targetAngle)
        {
            slideCameraHandlingCoroutineActive = true;

            while (currentMovementMode == MovementMode.Slide)
            {
                HandleSlideCameraTilt(targetAngle);
                yield return null;
            }

            slideCameraHandlingCoroutineActive = false;

            while (slideCameraHandlingCoroutineActive == false)
            {
                HandleSlideCameraTilt(0f); // regulate until the player slides again
                yield return null;
            }

        }

        private void HandleSlideCameraTilt(float targetAngle)
        {
            CurrentSlideCameraTiltAngle = Mathf.Lerp(CurrentSlideCameraTiltAngle, targetAngle, (targetAngle == 0f ? slideCameraTiltRegulationSpeed : slideCameraTiltSpeed) * Time.deltaTime);
        }

        #endregion

        #region Dash Camera Handling

        [Header("Dash Camera Tilt")]
        [SerializeField] private float maxDashCameraTiltAngle;
        [SerializeField] private float dashCameraTiltDuration;
        // this name sucks but basically
        // the leniency to the clamp (maxCameraTilt / noCameraTilt) so the lerp doesn t run forever and at some point
        // (when the difference currentCameraTilt -> extent < leniency) the value snaps to the extent
        private float currentDashCameraTiltAngle;
        public float CurrentDashCameraTiltAngle
        {
            get
            {
                return currentDashCameraTiltAngle;
            }
            set
            {
                if (Mathf.Abs(value - currentDashCameraTiltAngle) < cameraTiltLeniencyToExtent && TargetDashCameraSideTiltAngle != 0)
                {
                    currentDashCameraTiltAngle = maxDashCameraTiltAngle * Mathf.Sign(value);
                    return;
                }

                if (Mathf.Abs(value) < cameraTiltLeniencyToExtent && TargetDashCameraSideTiltAngle == 0)
                {
                    currentDashCameraTiltAngle = 0;
                    return;
                }

                currentDashCameraTiltAngle = value;
            }
        }

        //private float TargetRunCameraTiltAngle => MyInput.GetAxis(inputQuery.Right, inputQuery.Left) * maxRunCameraTiltAngle;
        private float TargetDashCameraSideTiltAngle => IsDashing ? -dashSide * maxDashCameraTiltAngle : 0f;
        //
        //private float TargetRunCameraTiltAngle => Rigidbody != null ? CurrentStrafeSpeed / Mathf.Abs(CurrentStrafeSpeed) * maxRunCameraTiltAngle : 0f;
        // as dir in {-1, 0, 1} dir * maxRunCameraTiltAngle in {-maxRunCameraTiltAngle, 0 (regulateCameraTilt), maxRunCameraTiltAngle}

        [SerializeField] private float dashCameraTiltRegulationDuration;


        private void HandleDashCamera()
        {
            if (dashCameraSideHandlingCoroutineActive <= 0)
            {
                StartCoroutine(HandleDashSideCameraCoroutine(TargetDashCameraSideTiltAngle));
            }
        }

        private IEnumerator HandleDashSideCameraCoroutine_(float targetAngle)
        {
            if (targetAngle == 0) { yield break; }

            dashCameraSideHandlingCoroutineActive = 1;
            Idle = false;
            Tilting = true;
            while (CurrentDashCameraTiltAngle != targetAngle)
            {
                HandleDashCameraTilt(targetAngle);
                yield return null;
            }

            Tilting = false;
            dashCameraSideHandlingCoroutineActive = -1;

            Regulating = true;
            while (CurrentDashCameraTiltAngle != 0 && dashCameraSideHandlingCoroutineActive == -1)
            {
                HandleDashCameraTilt(0f); // regulate until the player slides again
                yield return null;
            }

            Regulating = false;
            dashCameraSideHandlingCoroutineActive = dashCameraSideHandlingCoroutineActive == 1 ? 1 : 0;
            Idle = true;
        }

        private IEnumerator HandleDashSideCameraCoroutine(float targetAngle)
        {
            if (targetAngle == 0) { yield break; }

            dashCameraSideHandlingCoroutineActive = 1;
            Idle = false;
            Tilting = true;
            var elapsedTime = 0f;
            while (elapsedTime < dashCameraTiltDuration)
            {
                HandleDashCameraTilt(targetAngle, elapsedTime / dashCameraTiltDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Tilting = false;
            dashCameraSideHandlingCoroutineActive = -1;

            Regulating = true;
            elapsedTime = 0f;
            while (elapsedTime < dashCameraTiltRegulationDuration && dashCameraSideHandlingCoroutineActive == -1)
            {
                HandleDashCameraTilt(0f, elapsedTime / dashCameraTiltRegulationDuration); // regulate until the player slides again
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            Regulating = false;
            dashCameraSideHandlingCoroutineActive = dashCameraSideHandlingCoroutineActive == 1 ? 1 : 0;
            Idle = true;
        }

        private void HandleDashCameraTilt(float targetAngle)
        {
            CurrentDashCameraTiltAngle = Mathf.Lerp(CurrentDashCameraTiltAngle, targetAngle, (targetAngle == 0 ? dashCameraTiltRegulationDuration : dashCameraTiltDuration) * Time.deltaTime);
        }

        private void HandleDashCameraTilt(float targetAngle, float t)
        {
            float easedT = dashCameraTiltCurve.Evaluate(t);
            CurrentDashCameraTiltAngle = Mathf.Lerp(CurrentDashCameraTiltAngle, targetAngle, easedT);
        }

        #endregion


        #endregion

        private void OnDrawGizmosSelected()
        {
            if (doDebugCollidingDown)
            {
                Gizmos.color = isCollidingDown ? Color.red : Color.green;
                Gizmos.DrawWireSphere(
                    transform.position + ceilingCheckOffset,
                    groundCeilingCheckRadius
                    );

            }

            if (doDebugCollidingUp)
            {
                Gizmos.color = isCollidingUp ? Color.red : Color.green;
                Gizmos.DrawWireSphere(
                    transform.position + groundCheckOffset,
                    groundCeilingCheckRadius
                    );
            }


            if (doDebugCollidinAnySide)
            {
                Gizmos.color = isCollidingOnAnySide ? Color.red : Color.green;
                Gizmos.DrawWireCube(
                    transform.position,
                    bodyCheckSize
                    );
            }
        }
    }
}
public enum MovementMode
{
    Run,
    Slide,
    Wallrun,
    Dash,
    LedgeClimb,
    Grappling,
}

#region Movement Actions

// ACTIONS ANCHORS

public enum MovementAction
{
    Jump,
    Slide,
    Dash,
}

public struct MovementActionData
{
    public MovementAction Action;
    public MovementActionParam Param;

    public MovementActionData(MovementAction action, MovementActionParam param)
    {
        Action = action;
        Param = param;
    }
}

public interface MovementActionParam { }

public struct JumpActionParam : MovementActionParam
{
    public bool FullJump;
    public bool LongJump;
    public JumpActionParam(bool fullJump, bool longJump)
    {
        FullJump = fullJump;
        LongJump = longJump;
    }
}

//public struct SlideActionParam : MovementActionParam { }

//public struct DashActionParam : MovementActionParam { }

#endregion


// run on an angle

// make it so the wall run gains one velocity for each chained jump (reset when touching the ground)

// grapple -> impulse (using the same logic as Mist&Shadow) (+ raycast and actually having to aim)

// box knockable with the dash

// implement the pause

// simple ennemies (aim at you (shoot on you but bullet with travel time)) && hitscan but shoot after seeing u for X seconds

// fix the bug when jumping left/right forever untils it keeps one or the other direction even though I press the
// other

#region Debug

[Serializable]
public struct VelocityDebug
{
    public float Overall;
    public float X;
    public float Y;
    public float Z;

    public VelocityDebug(Vector3 velocity)
    {
        Overall = velocity.magnitude;
        X = velocity.x;
        Y = velocity.y;
        Z = velocity.z;
    }
}

[Serializable]
public struct CollisionDebug
{
    public bool Up;
    public bool Down;
    public bool Right;
    public bool Left;
    public bool Side;

    public CollisionDebug(bool up, bool down, bool right, bool left, bool side)
    {
        Up = up;
        Down = down;
        Right = right;
        Left = left;
        Side = side;
    }
}

#endregion

// remove boosts and make it a more manageable controller
// still dynamic with slide/dash etc but no more velocity boost for chaining moves (except for dash -> slide | slide -> jump
// dash into slide conserve momentum
// slide into jump too

//public enum MovementMode
//{
//    RUN -> instantly corrects your speed to its max (if grounded)
//    SLIDE -> lets you stay over the threshold but gradually reduce your speed
//    WALLRUN -> instantly corrects your speed to its max
//    DASH -> instantly sets your velocity to dash velocity
//    LEDGE_CLIMB -> kills momentum
//    GRAPPLING -> let you gather speed the further you are from the target you are at the beginning of the grapple action
//}

// crouch slide shenanigans:
// -> differents keys so no more mess like that and no ned for a sprint key
// if crouched then sprint -> should cancel crouch and start sprinting
// if sprint then crouch -> slide


// no more velocity boost mess
// only
// airborne -> airfriction using ApplySlowdown(slowdownForce);
// grounded -> sliding ? groundfriction using ApplySlowdown(slowdownForce) : instant cap;
// wallrunning => instant cap;

// add a feature where the spread is les significant whe crouching
// fix the camera when jumping while sliding and still holding the slide key

// dash && jump simultaneously -> long jump (longer but lower) (only forward)

// make the double dash thingy

// make a validatedActionBuffer for keystrokes combo

//  ? make the slide give a boost anyway just wait till totally on ground to give it


// make a lerping implementation of crouch/slide colllider adjustment