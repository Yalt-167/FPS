#define LOG_MOVEMENTS_EVENTS

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(-7)]
public class PlayerMovement : MonoBehaviour, IPlayerFrameMember
{
    #region Debug Things

    public VelocityDebug GlobalVelocityDebug;
    public VelocityDebug LocalVelocityDebug;
    public VelocityDebug HorizontalVelocityBoostDebug;
    public CollisionDebug CollisionDebug;

    [SerializeField] private bool doDebugCollidingDown;
    [SerializeField] private bool doDebugCollidingUp;
    [SerializeField] private bool doDebugCollidinAnySide;

    #endregion

    #region References

    [SerializeField] private MovementInputQuery inputQuery;
    private Rigidbody Rigidbody;
    private FollowRotationCamera followRotationCamera;
    private int ForwardAxisInput => MyInput.GetAxis(inputQuery.Back, inputQuery.Forward);
    private int SidewayAxisInput => MyInput.GetAxis(inputQuery.Left, inputQuery.Right);
    private bool PressingForwardOrStrafeInput => inputQuery.Forward || inputQuery.Left || inputQuery.Right;
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
    private bool IsSprinting => !inputQuery.HoldCrouch && PressingForwardOrStrafeInput;
    private bool IsCrouching { get; set; }
    private bool IsSliding => currentMovementMode == MovementMode.Slide;
    private bool IsDashing => currentMovementMode == MovementMode.Dash;
    private bool IsWallrunning => currentMovementMode == MovementMode.Wallrun;
    private bool IsLedgeClimbing => currentMovementMode == MovementMode.LedgeClimb;
    private bool IsGrappling => currentMovementMode == MovementMode.Grappling;

    #endregion

    #endregion

    #region Movements Setup

    [Header("Movements")]
    private float currentSpeedRatio; // Basically a lerp of the speed that increases overtime when running (basically some sort of acceleration
    [SerializeField] private float timeToReachMaxSprintSpeed;
    private float targetSpeed;
    [SerializeField] private float airFriction;
    private float CroouchingSpeed => RunningSpeed * .25f;
    private float RunningSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.RunningSpeed ?? 8f;
    private float SpeedDifferenceBetweenWalkingAndSprinting => RunningSpeed - CroouchingSpeed;
    //private float StrafingSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.StrafingSpeed ?? 7f;
    private float StrafingSpeedCoefficient => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.StrafingSpeedCoefficient ?? .75f;
    private float BackwardSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.BackwardSpeed ?? 5f;
    private float WallRunSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.WallRunningSpeed ?? 9f;


    [SerializeField] private float sidewayInertiaControlFactor; // when the direction changes apply it to control inertia and prevent the player from going sideway (former forward)

    private Action currentMovementMethod;

    private Vector3 horizontalVelocityBoost;
    [SerializeField] private float horizontalVelocityBoostDecayRate;

    private Vector3 currentExternalVelocityBoost;
    [SerializeField] private float externalVelocityBoostDecayRate;

    [SerializeField] private float minHorizontalVelocityBoostThreshold;

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
    private float JumpForce => PlayerFrame?.ChampionStats.MovementStats.JumpStats.JumpForce ?? 1080f;
    private float timeLeftGround;

    [SerializeField] private float terminalVelocity = -75f;

    private readonly float coyoteTimeThreshold = .1f;
    private readonly float jumpBuffer = .1f;
    private bool coyoteUsable;

    private float lastJumpPressed = float.NegativeInfinity;
    private bool CanUseCoyote => coyoteUsable && !isCollidingDown && timeLeftGround + coyoteTimeThreshold > Time.time;
    private bool HasBufferedJump => isCollidingDown && lastJumpPressed + jumpBuffer > Time.time;

    #endregion

    #region Dash Setup

    [Header("Dash")]
    [SerializeField] private float afterDashMomentumConservationWindowDuration;
    private bool InDashMomentumConservationWindow => timeDashTriggered + DashDuration + afterDashMomentumConservationWindowDuration > Time.time;
    private float DashVelocity => PlayerFrame?.ChampionStats.MovementStats.DashStats.DashVelocity ?? 90f;
    private float DashDuration => PlayerFrame?.ChampionStats.MovementStats.DashStats.DashDuration ?? .1f;
    private float DashCooldown => PlayerFrame?.ChampionStats.MovementStats.DashStats.DashCooldown ?? 1f;
    private bool dashOnCooldown;

    private bool dashReady;
    private bool DashUsable => dashReady && !dashOnCooldown && (PlayerFrame?.ChampionStats.MovementStats.DashStats.HasDash ?? false);
    private float timeDashTriggered = float.NegativeInfinity;
    private bool ShouldReplenishDash => isCollidingDown || isCollidingLeft || isCollidingRight;

    #endregion

    #region Slide Setup

    [Header("Sliding")]
    [SerializeField] private float slideCancelThreshold;
    [SerializeField] private float slideJumpBoostWindow;
    [SerializeField] private float slideSlowdownForce;
    [SerializeField] private float slideDownwardForce;

    private float timeFinishedSliding;
    private bool InSlideJumpBoostWindow => timeFinishedSliding + slideJumpBoostWindow > Time.time;
    [SerializeField] private float fallIntoSlideVelocityBoost;

    [SerializeField] private float slideCameraHeightAdjustmentDuration = .3f;

    private readonly float initialColliderHeight = 2f;
    private readonly float slidingColliderHeight = .5f;

    #endregion

    #region Wallrun && WallJump Setup

    [Header("Wallrun")]
    [SerializeField] private float sideWallJumpForce;
    [SerializeField] private Vector3 upwardWallJumpForce;
    [SerializeField] private float sideWallJumpForceAwayFromWall;
    [SerializeField] private Vector3 upwardWallJumpForceAwayFromWall;
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
            MovementMode.Run => CurrentRunCameraTiltAngle,
            MovementMode.Slide => CurrentSlideCameraTiltAngle,
            MovementMode.Wallrun => CurrentWallRunCameraTilt,
            MovementMode.Dash => 0f,
            MovementMode.LedgeClimb => 0f,
            MovementMode.Grappling => 0f,
            _ => 0f
        };


    private bool slideCameraHandlingCoroutineActive;

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

    public PlayerFrame PlayerFrame { get; set; }

    public void InitPlayerFrame(PlayerFrame playerFrame)
    {
        PlayerFrame = playerFrame;
    }

    #endregion

    #region Updates

    private void Awake()
    {
        inputQuery.Init();

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
        _ = inputQuery.HoldLeftForTime;
        _ = inputQuery.HoldRightForTime;
        _ = inputQuery.QuickReset;
        _ = inputQuery.HoldSlide;
        _ = inputQuery.HoldCrouch;

        currentMovementMethod();
        currentCameraHandlingMethod();
        TryReplenishDash();

        if (inputQuery.SwitchCameraPosition) { SwitchCameraPosition(); }

        LocalVelocityDebug = new(transform.InverseTransformDirection(Rigidbody.velocity));
        GlobalVelocityDebug = new(Rigidbody.velocity);
        CollisionDebug = new(isCollidingUp, isCollidingDown, isCollidingRight, isCollidingLeft, isCollidingOnAnySide);
        HorizontalVelocityBoostDebug = new(horizontalVelocityBoost);

        // do a layer per level instead with a scriptable object LevelInfo holding all relevant y
        if (transform.position.y < -50f)
        {
            transform.position = Vector3.up * 100f;
        }
    }

    private void FixedUpdate()
    {
        RunCollisionChecks();

        // get rid of that eventually
        horizontalVelocityBoost = Vector3.Lerp(Vector3.zero, horizontalVelocityBoost, horizontalVelocityBoostDecayRate);
        if (horizontalVelocityBoost.magnitude < minHorizontalVelocityBoostThreshold) { horizontalVelocityBoost = Vector3.zero; }

        currentExternalVelocityBoost = Vector3.Lerp(Vector3.zero, currentExternalVelocityBoost, externalVelocityBoostDecayRate);
        if (currentExternalVelocityBoost.magnitude < minHorizontalVelocityBoostThreshold) { currentExternalVelocityBoost = Vector3.zero; }
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
                currentCameraHandlingMethod = () => { };
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
    }

    private void ApplySlowdown(float slowdownForce)
    {
        ApplyBoost(-slowdownForce);
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
        Rigidbody.AddForce(boostForce * Time.deltaTime * Rigidbody.velocity.Mask(1f, 0f, 1f).normalized, ForceMode.Impulse);
    }

    #endregion

    #region Jump

    /// <summary>
    /// Returns wether a jump occured
    /// </summary>
    /// <param name="coyoteThresholdAllowed"></param>
    /// <param name="afterSlide"></param>
    /// <param name="afterDash"></param>
    /// <returns></returns>
    private bool HandleJump(bool coyoteThresholdAllowed)
    {
        if (IsJumping) { return false; }

        if (inputQuery.InitiateJump)
        {
            lastJumpPressed = Time.time; // for jump buffer

            if (CanUseCoyote && coyoteThresholdAllowed || isCollidingDown)
            {
                Jump(true);
                return true;
            }
        }
        else if (HasBufferedJump)
        {
            Jump(inputQuery.HoldJump);
            return true;
        }

        return false;
    }

    private void CommonJumpStart()
    {
#if LOG_MOVEMENTS_EVENTS
        print("Jumped");
#endif

        UnCrouch();
        coyoteUsable = false;
        IsJumping = true;

        ResetYVelocity();

        if (inputQuery.Forward)
        {
            Rigidbody.AddForce(initialJumpSpeedBoost * transform.forward, ForceMode.Impulse);
        }
    }

    private void Jump(bool fullJump)
    {
        CommonJumpStart();

        Rigidbody.AddForce((fullJump ? JumpForce : JumpForce / 2) * Vector3.up, ForceMode.Impulse);

        StartCoroutine(ResetJumping());
    }

    private void WallJump(bool towardRight, bool awayFromWall)
    {
        CommonJumpStart();

        horizontalVelocityBoost += (towardRight ? transform.right : -transform.right) * (awayFromWall ? sideWallJumpForceAwayFromWall : sideWallJumpForce);

        horizontalVelocityBoost.y = 0f; // just in case (bc of that: Rigidbody.velocity.normalized) (even though it s reset in CommonJumpStart() I had issues with it so better safe than sorry

        Rigidbody.AddForce(awayFromWall ? upwardWallJumpForceAwayFromWall : upwardWallJumpForce, ForceMode.Impulse);

        StartCoroutine(ResetJumping());
    }

    private IEnumerator ResetJumping()
    {
        yield return new WaitUntil(
                () => Rigidbody.velocity.y < 0f || currentMovementMode != MovementMode.Run
            );

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

        if (HandleJump(true))
        {
            return;
        }

        if (DashUsable && inputQuery.Dash)
        {
            StartCoroutine(Dash());
            return;
        }

        if (inputQuery.HoldSlide && CurrentSpeed != 0f) // HoldSlide or InitiateSlide ? -> run some tests
        {
            StartCoroutine(Slide()); // add some kind of coyote threshold where the velocity is conserved even tho the player walked a bit (which should kill his momentum)
            return;
        }
        
        if (!isCollidingDown)
        {
            var sideToWallRunOn = MyInput.GetAxis(inputQuery.Left && isCollidingLeft, inputQuery.Right && isCollidingRight);
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

        Crouch(inputQuery.HoldCrouch && isCollidingDown);
    }

    private void Run()
    {
        var wantedMoveVec = new Vector2(SidewayAxisInput, ForwardAxisInput).normalized;

        var targetSpeed = CalculateTargetSpeed(wantedMoveVec) - (isCollidingDown ? 0f : .5f);

        var velocityY = Rigidbody.velocity.y;

        var localVelocity = transform.InverseTransformDirection(Rigidbody.velocity);
        var localVelocityBoost = transform.InverseTransformDirection(horizontalVelocityBoost);

        if (wantedMoveVec.x == 0f)
        {
            localVelocity = localVelocity.Mask(sidewayInertiaControlFactor, 0f, 1f);
            localVelocityBoost = localVelocityBoost.Mask(sidewayInertiaControlFactor, 0f, 1f);
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

        if (Mathf.Sign(wantedMoveVec.x) != Mathf.Sign(localVelocityBoost.x))
        {
            localVelocityBoost = localVelocityBoost.Mask(0f, 0f, 1f);
        }

        if (Mathf.Sign(wantedMoveVec.y) != Mathf.Sign(localVelocityBoost.z))
        {
            localVelocityBoost = localVelocityBoost.Mask(1f, 0f, 0f);
        }

        horizontalVelocityBoost = transform.TransformDirection(localVelocityBoost);

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


                float t = Mathf.Clamp01(timeElapsedSinceStartedSprinting / timeToReachMaxSprintSpeed); // if failing timeToReachMaxSprintSpeed probably reset to 0 in th einspector
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
            Rigidbody.velocity = Mathf.Lerp(CurrentSpeed, 0, airFriction) * direction;
        }

        ResetYVelocity(velocityY < terminalVelocity ? terminalVelocity : velocityY);

        //Rigidbody.AddForce(horizontalVelocityBoost + currentExternalVelocityBoost, ForceMode.Impulse);
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

    /// <summary>
    /// Returns wether the HandleRun() method should return too
    /// </summary>
    /// <returns></returns>
    private bool HandleCrouchActionFromRun()
    {
        if (inputQuery.HoldSlide && isCollidingDown)
        {
            if (IsSprinting)
            {
                StartCoroutine(Slide());
                return true;
            }
            else
            {
                Crouch();
                return false;
            }
        }
        else
        {
            UnCrouch();
            return false;
        }
    }


    private void Crouch(bool doIt)
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

        print("Crouch");
        IsCrouching = true;
        transform.localScale = transform.localScale.Mask(1f, .5f, 1f);
        transform.position -= Vector3.up * .5f;
    }

    private void UnCrouch()
    {
        if (!IsCrouching) { return; }

        print("Uncrouch");
        IsCrouching = false;
        transform.position += Vector3.up * .5f;
        transform.localScale = transform.localScale.Mask(1f, 2f, 1f);
    }

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

    #region Slide

    private void HandleSlide()
    {
        ApplyGravity();
    }

    private IEnumerator Slide(/*bool chainedFromDash*/)
    {
        var shouldAwardVelocityBoostForFalling = !isCollidingDown;

        yield return new WaitUntil(() => isCollidingDown || !inputQuery.HoldSlide); // await the landing to initiate the slide

        if (!inputQuery.HoldSlide) { yield break; } // if changed his mind

        if (shouldAwardVelocityBoostForFalling)
        {
            ApplyBoost(fallIntoSlideVelocityBoost);
        }
        
        SetMovementMode(MovementMode.Slide);
        transform.localScale = transform.localScale.Mask(1f, .5f, 1f);
        transform.position -= Vector3.up * .5f;


        //if (chainedFromDash)
        //{
        //  var dir = new Vector3(SidewayAxisInput, 0f, ForwardAxisInput);
        //  horizontalVelocityBoost +=
        //      (chainedFromDash ? dashIntoSlideVelocityBoost : 1f) * initialSlideBoost * (dir == Vector3.zero ?
        //          Rigidbody.velocity.Mask(1f, 0, 1f).normalized
        //          :
        //          transform.TransformDirection(dir)).normalized;
        //}


        var dashed = false;
        var jumped = false;

        yield return new WaitUntil(
            () =>
                {
                    if (isCollidingDown)
                    {
                        // like some extra gravity so the player can slide along steeper slopes
                        Rigidbody.AddForce(slideDownwardForce * Time.deltaTime * -transform.up, ForceMode.Force);
                    }

                    ApplySlowdown(slideSlowdownForce);

                    if (DashUsable && inputQuery.Dash)
                    {
                        CommonSlideExit(MovementMode.Dash);
                        StartCoroutine(Dash());
                        dashed = true;
                        return true;
                    }

                    jumped = HandleJump(true);

                    return
                        Rigidbody.velocity.magnitude < slideCancelThreshold ||
                        !inputQuery.HoldSlide ||
                        jumped
                        ;
                }
        );

        if (dashed) { yield break; }

        CommonSlideExit(MovementMode.Run);
        
    }

    private void CommonSlideExit(MovementMode newMovementMode)
    {
        transform.position += Vector3.up * .5f;
        transform.localScale = transform.localScale.Mask(1f, 2f, 1f);
        timeStoppedSlide = Time.time;
        SetMovementMode(newMovementMode);
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
        SetMovementMode(MovementMode.Dash);
        dashReady = false;
        ResetVelocityBoost();

        var slid = false;
        var jumped = false;
        timeDashTriggered = Time.time;
        followRotationCamera.enabled = false;
        var dir = cameraTransform.TransformDirection(SidewayAxisInput, 0f, ForwardAxisInput);

        dir = dir == Vector3.zero ? cameraTransform.forward : dir;
        
        yield return new WaitUntil(
            () =>
                {
                    //Rigidbody.velocity = dashVelocity * cameraTransform.forward; // perhaps do sth less brutal with gradual velocity loss
                    Rigidbody.velocity = DashVelocity * dir; // perhaps do sth less brutal with gradual velocity loss

                    if (inputQuery.InitiateSlide && isCollidingDown) // if slide during the dash then the boost is applied // here it s most likely in the dash (at most 1 frame off so take it as a lil gift :) )
                    {
                        slid = true;
                        CommonDashExit(MovementMode.Slide);
                        StartCoroutine(Slide(/*true*/));    
                        return true;
                    }

                    if (HandleJump(false))
                    {
                        jumped = true;
                        CommonDashExit(MovementMode.Run);
                        return true;
                    }

                    return timeDashTriggered + DashDuration < Time.time;
                }
        );

        if (slid || jumped) { yield break; }
        
        var shouldWallrunLeftRight = MyInput.GetAxis(inputQuery.Left && isCollidingLeft, inputQuery.Right && isCollidingRight);
        if (shouldWallrunLeftRight != 0f && !isCollidingDown)
        {
            CommonDashExit(MovementMode.Wallrun);
            StartCoroutine(Wallrun(shouldWallrunLeftRight));
        }
        else
        {
            CommonDashExit(MovementMode.Run);
        }
    }

    private void CommonDashExit(MovementMode newMovementMode)
    {
        followRotationCamera.enabled = true;
        SetMovementMode(newMovementMode);
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
        if (!CanLedgeClimb || inputQuery.Back || !inputQuery.Forward && !inputQuery.InitiateJump)
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
        ResetVelocityBoost();

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
        if (!inputQuery.Forward || inputQuery.Back || !CanWallRunAfterDash) { yield break; }

        SetMovementMode(MovementMode.Wallrun);

        onRight = side > 0;
        var forceDirection = onRight ? transform.right : -transform.right;

        timeStartedWallRunning = Time.time;
        var startedTiltingCamera = false;
        inputQuery.HoldLeftForTime.ResetState(); // avoid insta quitting the wallrun due to this triggering while you held it to jump
        inputQuery.HoldRightForTime.ResetState(); // same but other side :)

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

                if (inputQuery.InitiateJump)
                {
                    CommonWallRunExit(MovementMode.Run, onRight);
                    WallJump(!onRight, onRight ? inputQuery.Left : inputQuery.Right);
                    return leftEarly = true;
                }

                if (dashReady && inputQuery.Dash)
                {   
                    CommonWallRunExit(MovementMode.Dash, onRight);
                    StartCoroutine(Dash());
                    return leftEarly = true;
                }

                
                if (CheckLedgeClimb(out var ledgesEncountered))
                {
                    CommonWallRunExit(MovementMode.LedgeClimb, onRight);
                    LedgeClimb(ledgesEncountered);
                    return leftEarly = true;
                }

                if (inputQuery.InitiateSlide)
                {
                    CommonWallRunExit(MovementMode.Slide, onRight);
                    StartCoroutine(Slide(/*false*/));
                    return leftEarly = true;
                }

                if (!isCollidingOnAnySide) { return true; } // out of the final return bc didn t work for reasons that are beyond me

                return
                    inputQuery.Back ||
                    onRight ? inputQuery.HoldLeftForTime : inputQuery.HoldRightForTime
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

    #region Velocity

    private void ResetVelocityBoost()
    {
        horizontalVelocityBoost = Vector3.zero;
    }

    public void ResetXVelocity()
    {
        Rigidbody.velocity = new(0f, Rigidbody.velocity.y, Rigidbody.velocity.z);
    }

    public void ResetYVelocity()
    {
        Rigidbody.velocity = new(Rigidbody.velocity.x, 0f, Rigidbody.velocity.z);
    }

    public void ResetYVelocity(float newY)
    {
        Rigidbody.velocity = new(Rigidbody.velocity.x, newY, Rigidbody.velocity.z);
    }

    public void ResetZVelocity()
    {
        Rigidbody.velocity = new(Rigidbody.velocity.x, Rigidbody.velocity.y, 0f);
    }

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

    #region Run Camera Tilt

    [Header("Run Camera Tilt")]
    [SerializeField] private float maxRunCameraTiltAngle;
    [SerializeField] private float maxRunCameraTiltSpeed;
    [SerializeField] private float runCameraTiltLeniencyToExtent;
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
            if (Mathf.Abs(value - maxRunCameraTiltAngle) < runCameraTiltLeniencyToExtent && TargetRunCameraTiltAngle != 0)
            {
                currentRunCameraTiltAngle = maxRunCameraTiltAngle * Mathf.Sign(value);
                return;
            }

            if (Mathf.Abs(value) < runCameraTiltLeniencyToExtent && TargetRunCameraTiltAngle == 0)
            {
                currentRunCameraTiltAngle = 0;
                return;
            }

            currentRunCameraTiltAngle = value;
        }
    }

    //private float TargetRunCameraTiltAngle => MyInput.GetAxis(inputQuery.Right, inputQuery.Left) * maxRunCameraTiltAngle;
    private float TargetRunCameraTiltAngle => MyInput.GetAxis(inputQuery.Right && !isCollidingRight, inputQuery.Left & !isCollidingLeft) * maxRunCameraTiltAngle;
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
        HandleRunCameraTiltInternal(TargetRunCameraTiltAngle); 
    }

    private void HandleRunCameraTiltInternal(float targetAngle)
    {
        CurrentRunCameraTiltAngle = Mathf.Lerp(CurrentRunCameraTiltAngle, targetAngle, (targetAngle == 0 ? runCameraTiltRegulationSpeed : maxRunCameraTiltSpeed) * Time.deltaTime);
    }

    #endregion

    #region Slide Camera Tilt

    [Header("Slide Camera Tilt")]
    [SerializeField] private bool slideTiltOnRight;
    [SerializeField] private float maxSlideCameraTiltAngle;
    [SerializeField] private float slideCameraTiltSpeed;
    [SerializeField] private float slideCameraTiltLeniencyToExtent;
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
            if (Mathf.Abs(value - maxSlideCameraTiltAngle) < slideCameraTiltLeniencyToExtent && TargetSlideCameraTiltAngle != 0)
            {
                currentSlideCameraTiltAngle = maxSlideCameraTiltAngle * Mathf.Sign(value);
                return;
            }

            if (Mathf.Abs(value) < slideCameraTiltLeniencyToExtent && TargetSlideCameraTiltAngle == 0)
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

        while(slideCameraHandlingCoroutineActive == false)
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
        
        if(doDebugCollidingUp)
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

public enum MovementMode
{
    Run,
    Slide,
    Wallrun,
    Dash,
    LedgeClimb,
    Grappling
}

// run on an angle

// make it so the wall run gains one velocity for each chained jump (reset when touching the ground)

// grapple -> impulse (using the same logic as Mist&Shadow) (+ raycast and actually having to aim)

// box knockable with the dash

// implement the pause

// simple ennemies (aim at you (shoot on you but bullet with travel time)) && hitscan but shoot after seeing u for X seconds

// fix the bug when jumping left/right forever untils it keeps one or the other direction even though I press the
// other

# region Debug

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
//    GRAPPLING -> let you gather speed the further you are from the target you are at the beginning of tyhe grapple action
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
// fix the glitch of phasing through floor when spamming slide