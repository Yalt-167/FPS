using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[DefaultExecutionOrder(-7)]
public class PlayerMovement : MonoBehaviour, IPlayerFrameMember
{
    public VelocityDebug GlobalVelocityDebug;
    public VelocityDebug LocalVelocityDebug;
    public VelocityDebug HorizontalVelocityBoostDebug;
    public CollisionDebug CollisionDebug;

    #region References

    [SerializeField] private MovementInputQuery inputQuery;
    private Rigidbody Rigidbody;
    private CapsuleCollider capsuleCollider;
    private FollowRotationCamera followRotationCamera;
    public Vector3 Position => transform.position;
    public float CurrentSpeed => new Vector3(Rigidbody.velocity.x, 0f, Rigidbody.velocity.z).magnitude;
    public bool IsJumping { get; private set; } = false;
    public bool IsGrounded => isCollidingDown;



    [Space(10)]
    [SerializeField] private MovementMode currentMovementMode;

    #endregion

    #region Movements Setup

    [Header("Movements")]
    [SerializeField] private float targetSpeed;
    // in m/s
    private float RunningSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.RunningSpeed ?? 8f;
    private float StrafingSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.StrafingSpeed ?? 7f;
    private float BackwardSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.BackwardSpeed ?? 5f;
    private float WallRunSpeed => PlayerFrame?.ChampionStats.MovementStats.SpeedStats.WallRunningSpeed ?? 9f;

    [SerializeField] private float acceleration;
    [SerializeField] private float stopDecceleration; // deceleration when no key held
    [SerializeField] private float baseDecceleration; // decceleration when speed above limit
    [SerializeField] private float sidewayInertiaControlFactor; // when the direction changes apply it to control inertia and prevent the player from going sideway (former forward)

    private Action currentMovementMethod;
    private Vector3 horizontalVelocityBoost;
    [SerializeField] private float horizontalVelocityBoostDecayRate;
    [SerializeField] private float minHorizontalVelocityBoostThreshold;

    private Vector3 currentExternalVelocityBoost;
    [SerializeField] private float externalVelocityBoostDecayRate;
    [SerializeField] private float velocityBoostCoefficient;
    [SerializeField] private Vector3 horizontalVelocityBoostCoefficient;
    [SerializeField] private float velocityBoostCancelThreshold;

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

    [SerializeField] private BoxCaster leftCheck;
    [SerializeField] private BoxCaster rightCheck;
    [SerializeField] private BoxCaster upperLedgeClimbCheck;
    [SerializeField] private BoxCaster lowerLedgeClimbCheck;

    #endregion

    #region Jump Setup

    [Header("Jump")]
    [SerializeField] private float initialJumpSpeedBoost;
    [SerializeField] private float slideIntoJumpVelocityBoost;
    private float JumpForce => PlayerFrame?.ChampionStats.MovementStats.JumpStats.JumpForce ?? 1280f;
    private float timeLeftGround;

    [SerializeField] private float terminalVelocity = -75f;

    private readonly float coyoteTimeThreshold = .1f;
    private readonly float jumpBuffer = .1f;
    private bool coyoteUsable;

    private float lastJumpPressed = -.2f;
    private bool CanUseCoyote => coyoteUsable && !isCollidingDown && timeLeftGround + coyoteTimeThreshold > Time.time;
    private bool HasBufferedJump => isCollidingDown && lastJumpPressed + jumpBuffer > Time.time;

    #endregion

    #region Dash Setup

    [Header("Dash")]
    [SerializeField] private float dashIntoJumpVelocityBoost;
    [SerializeField] private float afterDashVelocityBoostWindowDuration;
    private float DashVelocity => PlayerFrame?.ChampionStats.MovementStats.DashStats.DashVelocity ?? 90f;
    private float DashDuration => PlayerFrame?.ChampionStats.MovementStats.DashStats.DashDuration ?? .1f;
    private float DashCooldown => PlayerFrame?.ChampionStats.MovementStats.DashStats.DashCooldown ?? 1f;
    private bool dashOnCooldown;
    private bool InDashVelocityBoostWindow => timeDashTriggered + DashDuration + afterDashVelocityBoostWindowDuration > Time.time;

    private bool dashReady;
    private bool DashUsable => dashReady && !dashOnCooldown && (PlayerFrame?.ChampionStats.MovementStats.DashStats.HasDash ?? false);
    private float timeDashTriggered = float.NegativeInfinity;
    private bool ShouldReplenishDash => isCollidingDown || isCollidingLeft || isCollidingRight;

    #endregion

    #region Slide Setup

    [Header("Sliding")]
    [SerializeField] private float slideDecelerationRate;
    [SerializeField] private float slideCancelThreshold;
    [SerializeField] private float initialSlideBoost = 3f;
    //[SerializeField] private float initialSlideDownBoost = 10f;
    [SerializeField] private float slideJumpBoostWindow;
    [SerializeField] private float slideSlowdownForce;
    [SerializeField] private float slideDownwardForce = 1000f;

    private float timeFinishedSliding;
    [SerializeField] private float dashIntoSlideVelocityBoost;
    private bool InSlideJumpBoostWindow => timeFinishedSliding + slideJumpBoostWindow > Time.time;

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

    //private void Duration


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
    [field: SerializeField] public float CurrentWallRunCameraTilt { get; private set; } = 0f;
    [SerializeField] private float timeOnWallBeforeTiltingCamera;
    private float timeStartedWallRunning;
    private bool ShouldStartTiltingCamera => timeStartedWallRunning + timeOnWallBeforeTiltingCamera < Time.time;

    private Transform cameraTransform;
    private Vector3[] cameraTransformPositions = new Vector3[2] { new(0f, .6f, 0f), new(0f, 2f, -5f) };
    private int currentCameraTransformPositionIndex = 0;

    private readonly float cameraTransformBaseHeight = .6f;
    private readonly float cameraTransformSlidingHeight = .3f;

    private Action currentCameraHandlingMethod;

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

        // The rest
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
        //Instance = this;

        inputQuery.Init();

        Rigidbody = GetComponent<Rigidbody>();
        capsuleCollider = GetComponent<CapsuleCollider>();
        cameraTransform = transform.GetChild(0).GetComponent<Transform>();
        followRotationCamera = transform.GetChild(0).GetComponent<FollowRotationCamera>();
        cameraTransform = transform.GetChild(0);
        cameraOriginalPosition = cameraTransform.localPosition;

        cameraTransform.localPosition = cameraTransformPositions[0];
        SetMovementMode(MovementMode.RUN);
    }

    private void Update()
    {
        // kinda nasty but these need to be called each frame for them to work as expected (otherwise the CheckKeyHeld() might never be called and the key might just be seen as pressed forever;
        _ = inputQuery.HoldLeftForTime;
        _ = inputQuery.HoldRightForTime;
        _ = inputQuery.QuickReset;

        currentMovementMethod();
        currentCameraHandlingMethod();
        TryReplenishDash();
        if (inputQuery.SwitchCameraPosition) SwitchCameraPosition();

        LocalVelocityDebug = new(transform.TransformDirection(Rigidbody.velocity));
        GlobalVelocityDebug = new(Rigidbody.velocity);
        CollisionDebug = new(isCollidingUp, isCollidingDown, isCollidingRight, isCollidingLeft, isCollidingOnAnySide);
        HorizontalVelocityBoostDebug = new(horizontalVelocityBoost);

        if (Physics.Raycast(transform.position, Vector3.down, 2f * 0.5f + 0.2f, limits)) transform.position = Vector3.up * 100;
        // do a layer per level instead with a scriptable object LevelInfo holding all relevant y
    }

    private void FixedUpdate()
    {
        RunCollisionChecks();

        horizontalVelocityBoost = Vector3.Lerp(Vector3.zero, horizontalVelocityBoost, horizontalVelocityBoostDecayRate);
        if (horizontalVelocityBoost.magnitude < minHorizontalVelocityBoostThreshold)
        {
            horizontalVelocityBoost = Vector3.zero;
        }

        currentExternalVelocityBoost = Vector3.Lerp(Vector3.zero, currentExternalVelocityBoost, externalVelocityBoostDecayRate);
        if (currentExternalVelocityBoost.magnitude < minHorizontalVelocityBoostThreshold)
        {
            currentExternalVelocityBoost = Vector3.zero;
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
            case MovementMode.RUN:
                currentGravityForce = baseGravity;
                currentMovementMethod = HandleRun;
                currentCameraHandlingMethod = () => { };
                break;

            case MovementMode.SLIDE:
                currentGravityForce = baseGravity;
                currentMovementMethod = HandleSlide;
                currentCameraHandlingMethod = () => { };
                break;

            case MovementMode.WALLRUN:
                currentGravityForce = noGravity;
                currentMovementMethod = HandleWallrun;
                currentCameraHandlingMethod = () => { };
                break;

            case MovementMode.DASH:
                currentGravityForce = noGravity;
                currentMovementMethod = HandleDash;
                currentCameraHandlingMethod = () => { };
                break;

            case MovementMode.LEDGE_CLIMB:
                currentGravityForce = noGravity;
                currentMovementMethod = HandleLedgeClimb;
                currentCameraHandlingMethod = () => { };
                break;

            case MovementMode.GRAPPLING:
                currentGravityForce = noGravity;
                currentMovementMethod = HandleGrappling;
                currentCameraHandlingMethod = () => { };
                break;
        }
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
    private bool HandleJump(bool coyoteThresholdAllowed, bool afterSlide, bool afterDash)
    {
        if (IsJumping) { return false; }

        if (inputQuery.InitiateJump)
        {
            lastJumpPressed = Time.time; // for jump buffer

            if (CanUseCoyote && coyoteThresholdAllowed || isCollidingDown)
            {
                Jump(true, afterSlide, afterDash);
                return true;
            }
        }
        else if (HasBufferedJump)
        {
            Jump(inputQuery.HoldJump, afterSlide, afterDash);
            return true;
        }

        return false;
    }

    private void CommonJumpStart()
    {
        coyoteUsable = false;
        IsJumping = true;

        ResetYVelocity();
    }

    private void Jump(bool fullJump, bool afterSlide, bool afterDash)
    {
        print("jumped");
        CommonJumpStart();
        horizontalVelocityBoost +=
            (afterSlide ? slideIntoJumpVelocityBoost : 1f) *
            (afterDash ? dashIntoJumpVelocityBoost : 1f) *
            initialJumpSpeedBoost *
            transform.TransformDirection(new Vector3(MyInput.GetAxis(inputQuery.Left, inputQuery.Right), 0f, MyInput.GetAxis(inputQuery.Back, inputQuery.Forward))).normalized;

        Rigidbody.AddForce((fullJump ? JumpForce : JumpForce / 2) * Vector3.up, ForceMode.Impulse);

        StartCoroutine(ResetJumping());
    }

    private void WallJump(bool towardRight, bool awayFromWall)
    {
        CommonJumpStart();

        horizontalVelocityBoost += initialJumpSpeedBoost * Rigidbody.velocity.normalized;
        horizontalVelocityBoost += (towardRight ? transform.right : -transform.right) * (awayFromWall ? sideWallJumpForceAwayFromWall : sideWallJumpForce);
            
        horizontalVelocityBoost.y = 0f; // just in case (bc of that: Rigidbody.velocity.normalized) (even though it s reset in CommonJumpStart() I had issues with it so better safe than sorry

        Rigidbody.AddForce(awayFromWall ? upwardWallJumpForceAwayFromWall : upwardWallJumpForce, ForceMode.Impulse);

        StartCoroutine(ResetJumping());
    }

    private IEnumerator ResetJumping()
    {
        yield return new WaitUntil(
                () => Rigidbody.velocity.y < 0f/* || (int) currentMovementMode > 1*/
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
        
        Run(isCollidingDown);
        

        if (HandleJump(true, InSlideJumpBoostWindow, InDashVelocityBoostWindow))
        {
            return;
        }
        
        if (DashUsable && inputQuery.Dash)
        {
            StartCoroutine(Dash());
            return;
        }
        else if (inputQuery.InitiateSlide && !(Rigidbody.velocity == Vector3.zero))
        {
            StartCoroutine(Slide(InDashVelocityBoostWindow));
            return;
        }
        else
        {
            var shouldWallrunLeftRight = MyInput.GetAxis(inputQuery.Left && isCollidingLeft, inputQuery.Right && isCollidingRight);
            if (shouldWallrunLeftRight != 0f && !isCollidingDown)
            {
                StartCoroutine(Wallrun(shouldWallrunLeftRight));
                return;
            }
        }

        
        if (CheckLedgeClimb(out var ledges))
        {
            LedgeClimb(ledges);
        }
    }

    private void Run(bool grounded)
    {
        var wantedMoveVec = new Vector2(MyInput.GetAxis(inputQuery.Left, inputQuery.Right), MyInput.GetAxis(inputQuery.Back, inputQuery.Forward)).normalized;

        var targetSpeed = CalculateTargetSpeed(wantedMoveVec) - (grounded ? 0f : .5f);

        var velocityY = Rigidbody.velocity.y;
        var localVelocity = transform.InverseTransformDirection(Rigidbody.velocity);
        var localVelocityBoost = transform.InverseTransformDirection(horizontalVelocityBoost);

        if (wantedMoveVec.x == 0f)
        {
            localVelocity = localVelocity.Mask(sidewayInertiaControlFactor, 0, 1);
            localVelocityBoost = localVelocityBoost.Mask(sidewayInertiaControlFactor, 0, 1);
        }
        else if (Mathf.Sign(wantedMoveVec.x) != Mathf.Sign(localVelocity.x))
        {
           localVelocity = localVelocity.Mask(0, 0, 1);
        }

        if (Mathf.Sign(wantedMoveVec.y) != Mathf.Sign(localVelocity.z))
        {
            localVelocity = localVelocity.Mask(1, 0, 0);
        }

        Rigidbody.velocity = transform.TransformDirection(localVelocity);

        if (Mathf.Sign(wantedMoveVec.x) != Mathf.Sign(localVelocityBoost.x))
        {
            localVelocityBoost = localVelocityBoost.Mask(0, 0, 1);
        }

        if (Mathf.Sign(wantedMoveVec.y) != Mathf.Sign(localVelocityBoost.z))
        {
            localVelocityBoost = localVelocityBoost.Mask(1, 0, 0);
        }

        horizontalVelocityBoost = transform.TransformDirection(localVelocityBoost);

        // dumb shit
        //{
        //Rigidbody.velocity = transform.TransformDirection(transform.InverseTransformDirection(Rigidbody.velocity).Mask(wantedMoveVec.x == 0f ? sidewayInertiaControlFactor : (Mathf.Sign(wantedMoveVec.x) != Mathf.Sign(transform.InverseTransformDirection(Rigidbody.velocity).x)) ? 0f : 1f, 0f, Mathf.Sign(wantedMoveVec.y) != Mathf.Sign(transform.InverseTransformDirection(Rigidbody.velocity).z) ? 0f : 1f));
        //}


        Rigidbody.AddForce(acceleration * Time.deltaTime * (transform.forward * wantedMoveVec.y + transform.right * wantedMoveVec.x), ForceMode.Force);
        Rigidbody.velocity = Vector3.ClampMagnitude(Rigidbody.velocity.Mask(1f, 0f, 1f), targetSpeed);

        ResetYVelocity(Mathf.Clamp(velocityY, terminalVelocity, 13f));

        Rigidbody.AddForce((horizontalVelocityBoost + currentExternalVelocityBoost), ForceMode.Impulse); // 120 * Time.deltaTime * 
    }

    private float CalculateTargetSpeed(Vector2 wantedMoveVec)
    {
        return wantedMoveVec.x == 0f ? wantedMoveVec.y == 0f ? 0f : wantedMoveVec.y < 0f ? BackwardSpeed : RunningSpeed : wantedMoveVec.y < 0f ? BackwardSpeed : StrafingSpeed;
    }

    #endregion

    #region Slide

    private void HandleSlide()
    {
        ApplyGravity();
    }

    private IEnumerator Slide(bool chainedFromDash)
    {
        yield return new WaitUntil(() => isCollidingDown || !inputQuery.HoldSlide); // await the landing to initiate the slide

        if (!inputQuery.HoldSlide) { yield break; } // if changed his mind
        
        SetMovementMode(MovementMode.SLIDE);
        transform.localScale = transform.localScale.Mask(1f, .5f, 1f);
        transform.position = transform.position - Vector3.up * .5f;
        //cameraTransform.localPosition = cameraTransform.localPosition.Mask(1f, .25f, 1f);


        if (chainedFromDash)
        {
        //var dir = new Vector3(MyInput.GetAxis(inputQuery.Left, inputQuery.Right), 0f, MyInput.GetAxis(inputQuery.Back, inputQuery.Forward));
        //horizontalVelocityBoost +=
        //    (chainedFromDash ? dashIntoSlideVelocityBoost : 1f) * initialSlideBoost * (dir == Vector3.zero ?
        //        Rigidbody.velocity.Mask(1f, 0, 1f).normalized
        //        :
        //        transform.TransformDirection(dir)).normalized;
        }


        var dashed = false;
        var jumped = false;

        yield return new WaitUntil(
            () =>
                {
                    if (isCollidingDown)
                    {
                        Rigidbody.AddForce(slideDownwardForce * Time.deltaTime * -transform.up, ForceMode.Force);
                    }

                    /* -Rigidbody.velocity.Mask(1f, 0f, 1f).normalized -> gets the opposite of the velocity while ignoring verticality */
                    Rigidbody.AddForce(slideSlowdownForce * Time.deltaTime * -Rigidbody.velocity.Mask(1f, 0f, 1f).normalized, ForceMode.Force);

                    if (DashUsable && inputQuery.Dash)
                    {
                        CommonSlideExit(MovementMode.DASH);
                        StartCoroutine(Dash());
                        dashed = true;
                        return true;
                    }

                    jumped = HandleJump(true, true, InDashVelocityBoostWindow);

                    return
                        Rigidbody.velocity.magnitude < slideCancelThreshold ||
                        !inputQuery.HoldSlide ||
                        jumped
                        ;
                }
        );

        if (dashed) { yield break; }

        CommonSlideExit(MovementMode.RUN);
        
    }

    private void CommonSlideExit(MovementMode newMovementMode)
    {
        //cameraTransform.localPosition = cameraTransform.localPosition.Mask(1f, 2f, 1f);
        transform.localScale = transform.localScale.Mask(1f, 2f, 1f);
        transform.position = transform.position - Vector3.up * .5f;
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
        SetMovementMode(MovementMode.DASH);
        dashReady = false;
        ResetVelocityBoost();

        var slid = false;
        var jumped = false;
        timeDashTriggered = Time.time;
        followRotationCamera.enabled = false;
        var dir = cameraTransform.TransformDirection(new(MyInput.GetAxis(inputQuery.Left, inputQuery.Right), 0f, MyInput.GetAxis(inputQuery.Back, inputQuery.Forward)));

        dir = dir == Vector3.zero ? cameraTransform.forward : dir;
        
        yield return new WaitUntil(
            () =>
                {
                    //Rigidbody.velocity = dashVelocity * cameraTransform.forward; // perhaps do sth less brutal with gradual velocity loss
                    Rigidbody.velocity = DashVelocity * dir; // perhaps do sth less brutal with gradual velocity loss

                    if (inputQuery.InitiateSlide && isCollidingDown) // if slide during the dash then the boost is applied // here it s most likely in the dash (at most 1 frame off so take it as a lil gift :) )
                    {
                        slid = true;
                        CommonDashExit(MovementMode.SLIDE);
                        StartCoroutine(Slide(true));    
                        return true;
                    }

                    if (HandleJump(false, InSlideJumpBoostWindow, true))
                    {
                        jumped = true;
                        CommonDashExit(MovementMode.RUN);
                        return true;
                    }

                    return timeDashTriggered + DashDuration < Time.time;
                }
        );

        if (slid || jumped) { yield break; }
        
        var shouldWallrunLeftRight = MyInput.GetAxis(inputQuery.Left && isCollidingLeft, inputQuery.Right && isCollidingRight);
        if (shouldWallrunLeftRight != 0f && !isCollidingDown)
        {
            CommonDashExit(MovementMode.WALLRUN);
            StartCoroutine(Wallrun(shouldWallrunLeftRight));
        }
        else
        {
            CommonDashExit(MovementMode.RUN);
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

    private IEnumerator ClimbLedge(Vector3 ledgePosition)
    {
        SetMovementMode(MovementMode.LEDGE_CLIMB);

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

        SetMovementMode(MovementMode.RUN);
    }

    private Vector3 CalculateLedgePosition(Collider[] colliders)
    {
        var ledgeY = float.MinValue;
        Collider relevantCollider = null;
        foreach (var collider in colliders)
        {
            var halfColSize = collider.transform.lossyScale.y * .5f;
            var curPos = collider.transform.position.y + halfColSize;
            if (ledgeY < curPos) // get the highest scalable surface's height
            {
                ledgeY = curPos; 
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

        SetMovementMode(MovementMode.WALLRUN);

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
                    CommonWallRunExit(MovementMode.RUN, onRight);
                    WallJump(!onRight, onRight ? inputQuery.Left : inputQuery.Right);
                    return leftEarly = true;
                }

                if (dashReady && inputQuery.Dash)
                {   
                    CommonWallRunExit(MovementMode.DASH, onRight);
                    StartCoroutine(Dash());
                    return leftEarly = true;
                }

                
                if (CheckLedgeClimb(out var ledgesEncountered))
                {
                    CommonWallRunExit(MovementMode.LEDGE_CLIMB, onRight);
                    LedgeClimb(ledgesEncountered);
                    return leftEarly = true;
                }

                if (inputQuery.InitiateSlide)
                {
                    CommonWallRunExit(MovementMode.SLIDE, onRight);
                    StartCoroutine(Slide(false));
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

        CommonWallRunExit(MovementMode.RUN, onRight);
        
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

    public void AddExternalForces(Vector3 forceDirection, float force)
    {
        currentExternalVelocityBoost += forceDirection * force;
    }

    public void AddExternalForces(Vector3 force)
    {
        currentExternalVelocityBoost += force;
    }

    #region Camera Handling

    [Header("Bobbing Settings")]
    [SerializeField] private float bobbingSpeed = 0.1f; // Speed of the bobbing motion
    [SerializeField] private float bobbingAmount = 0.1f; // Amount of bobbing motion
    private Vector3 cameraOriginalPosition; // Original camera position

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


    private IEnumerator Bob()
    {
        var timer = 0f;

        for (; ; )
        {
            var verticalOffset = Mathf.Sin(timer) * bobbingAmount;

            cameraTransform.localPosition = cameraOriginalPosition + new Vector3(0f, verticalOffset, 0f);

            timer += BobbingSpeed * Time.deltaTime;

            yield return null;
        }
    }

    #endregion
    private void OnDrawGizmosSelected()
    {

        //Gizmos.color = isCollidingDown ? Color.red : Color.green;
        //Gizmos.DrawWireSphere(
        //    transform.position + ceilingCheckOffset,
        //    groundCeilingCheckRadius
        //    );

        //Gizmos.color = isCollidingUp ? Color.red : Color.green;
        //Gizmos.DrawWireSphere(
        //    transform.position + groundCheckOffset,
        //    groundCeilingCheckRadius
        //    );

        //Gizmos.color = isCollidingOnAnySide ? Color.red : Color.green;
        //Gizmos.DrawWireCube(
        //    transform.position,
        //    bodyCheckSize
        //    );
    }
}

public enum MovementMode
{
    RUN,
    SLIDE,
    WALLRUN,
    DASH,
    LEDGE_CLIMB,
    GRAPPLING
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
// still dynamic with slide/dash etc but no more velocity boost for chaining moves