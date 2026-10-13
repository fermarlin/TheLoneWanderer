using System;
using UnityEngine;
using UnityEngine.InputSystem;

//Este script se encarga de gestionar el movimiento como tal delplayer

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerInput))]
public class PlayerController : MonoBehaviour
{
    [Header("Configuracion")]
    [SerializeField] private PlayerConfigSO config;

    [Header("Eventos compartidos (opcional si hay GameOrchestrator)")]
    [SerializeField] private GameEventsSO gameEvents;

    private Transform groundPoint;
    private Transform rightWallPoint;
    private Transform leftWallPoint;

    private Transform visualObject;

    public event Action OnJump;
    public event Action OnWallJump;
    public event Action OnDash;


    private Rigidbody2D rb;
    private PlayerInput playerInput;
    private InputAction moveAction;
    private InputAction jumpAction;
    private InputAction dashAction;
    private bool actionsInitialized;
    private GameEventsSO subscribedEvents;
    private Vector3 originalVisualScale;

    private bool isGrounded;
    private bool isTouchingWall;
    private bool isWallSliding;
    private bool isForcedWallFall;
    private bool isJumping;
    private bool isPaused;
    private bool isShieldInFlight;
    private RigidbodyConstraints2D previousShieldConstraints;

    private bool isDoubleJumpUnlocked;
    private bool isWallJumpUnlocked;
    private bool isDashUnlocked;
    private bool isDashing;
    private bool isAirDashAvailable = true;
    private float dashTimeRemaining;
    private float dashCooldownRemaining;
    private int dashDirection;

    private int wallDirection;
    private int facingDirection = 1;
    private int extraJumpsRemaining;
    private float horizontalMovement;
    private float wallJumpTimer;

    private bool pendingJump;
    private bool pendingJumpCut;
    private bool pendingDash;

    private float authorizedVerticalVelocity;
    private bool verticalVelocityInitialized;

    public bool IsForcedWallFall => isForcedWallFall;
    public bool IsGrounded => isGrounded;
    public bool IsTouchingWall => isTouchingWall;
    public bool IsWallSliding => isWallSliding;
    public bool IsJumping => isJumping;
    public int ExtraJumpsRemaining => extraJumpsRemaining;
    public bool IsDoubleJumpUnlocked => isDoubleJumpUnlocked;
    public bool IsWallJumpUnlocked => isWallJumpUnlocked;
    public bool IsDashUnlocked => isDashUnlocked;
    public bool IsDashing => isDashing;
    public bool IsShieldInFlight => isShieldInFlight;
    public float DashCooldownRemaining => dashCooldownRemaining;
    public bool IsFalling =>
        !isDashing && !isGrounded && rb != null &&
        (isForcedWallFall || (!isJumping && rb.linearVelocity.y <= 0.05f));
    public int FacingDirection => facingDirection;
    public float HorizontalMovement => horizontalMovement;
    public Vector2 InputDirection => moveAction != null && moveAction.enabled
        ? moveAction.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 Velocity => rb != null ? rb.linearVelocity : Vector2.zero;

    private GameEventsSO Events
    {
        get
        {
            GameOrchestrator orchestrator = GameOrchestrator.Instance;
            return orchestrator != null ? orchestrator.Events : gameEvents;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        if (playerInput == null)
        {
            Debug.LogError("[PlayerController] Necesitas el componente Player Input oficial de Unity.", this);
            enabled = false;
            return;
        }

        if (visualObject == null)
            visualObject = transform;
        originalVisualScale = visualObject.localScale;

        if (config == null)
        {
            Debug.LogError("[PlayerController] Asigna un PlayerConfigSO en el Inspector.", this);
            enabled = false;
            return;
        }

        isDoubleJumpUnlocked = config.dobleSaltoDesbloqueadoInicialmente;
        isWallJumpUnlocked = config.saltoParedDesbloqueadoInicialmente;
        isDashUnlocked = config.dashDesbloqueadoInicialmente;
        ResetExtraJumps();
        AuthorizeCurrentVerticalVelocity();
    }


    private void Start()
    {
        actionsInitialized = true;
        SubscribeToActions();
    }

    private void SubscribeToActions()
    {
        if (!isActiveAndEnabled)
            return;
        if (playerInput == null)
            playerInput = GetComponent<PlayerInput>();
        if (playerInput == null || playerInput.actions == null)
            return;

        InputActionMap actionMap = playerInput.actions.FindActionMap("Player", true);
        moveAction = actionMap.FindAction("Move", true);
        jumpAction = actionMap.FindAction("Jump", true);
        dashAction = actionMap.FindAction("Sprint", true);

        jumpAction.performed += RegisterJump;
        jumpAction.canceled += RegisterJumpRelease;
        dashAction.performed += RegisterDash;
    }

    private void OnEnable()
    {
        if (rb == null || config == null)
            return;

        ClearPendingInput();
        if (actionsInitialized)
            SubscribeToActions();

        subscribedEvents = Events;
        if (subscribedEvents != null)
        {
            subscribedEvents.OnPauseChanged += HandlePauseChanged;
            subscribedEvents.OnDoubleJumpUnlockRequested += UnlockDoubleJump;
            subscribedEvents.OnWallJumpUnlockRequested += UnlockWallJump;
            subscribedEvents.OnDashUnlockRequested += UnlockDash;
        }

        GameOrchestrator orchestrator = GameOrchestrator.Instance;
        HandlePauseChanged(orchestrator != null && orchestrator.GamePaused);
    }

    private void OnDisable()
    {
        if (jumpAction != null)
        {
            jumpAction.performed -= RegisterJump;
            jumpAction.canceled -= RegisterJumpRelease;
        }
        if (dashAction != null)
            dashAction.performed -= RegisterDash;

        moveAction = null;
        jumpAction = null;
        dashAction = null;
        if (subscribedEvents != null)
        {
            subscribedEvents.OnPauseChanged -= HandlePauseChanged;
            subscribedEvents.OnDoubleJumpUnlockRequested -= UnlockDoubleJump;
            subscribedEvents.OnWallJumpUnlockRequested -= UnlockWallJump;
            subscribedEvents.OnDashUnlockRequested -= UnlockDash;
        }
        subscribedEvents = null;
        ClearPendingInput();
    }


    public void SetShieldInFlight(bool block)
    {
        if (rb == null || isShieldInFlight == block)
            return;

        isShieldInFlight = block;
        ClearPendingInput();

        if (block)
        {
            previousShieldConstraints = rb.constraints;
            rb.linearVelocity = Vector2.zero;
            rb.constraints = previousShieldConstraints | RigidbodyConstraints2D.FreezePosition;
            AuthorizeCurrentVerticalVelocity();
        }
        else
        {
            rb.constraints = previousShieldConstraints;
            rb.linearVelocity = Vector2.zero;
            AuthorizeCurrentVerticalVelocity();
        }
    }

    private void Update()
    {
        if (config == null || isPaused || isShieldInFlight)
            return;

        horizontalMovement = Mathf.Clamp(InputDirection.x, -1f, 1f);
    }

    private void FixedUpdate()
    {
        if (config == null || isPaused || isShieldInFlight)
            return;

        CorrectVerticalImpulse();

        if (dashCooldownRemaining > 0f)
            dashCooldownRemaining = Mathf.Max(0f, dashCooldownRemaining - Time.fixedDeltaTime);

        bool wasOnWall = isTouchingWall;
        DetectGround();
        DetectWall();

        if (isGrounded)
        {
            isAirDashAvailable = true;
            isForcedWallFall = false;
            isJumping = false;
            ResetExtraJumps();
        }
        else if (wasOnWall && !isTouchingWall &&
                 wallJumpTimer <= 0f && !isJumping)
        {
            isForcedWallFall = true;
        }

        if (isJumping && rb.linearVelocity.y <= 0f)
            isJumping = false;

        if (wallJumpTimer > 0f && !isDashing)
            wallJumpTimer = Mathf.Max(0f, wallJumpTimer - Time.fixedDeltaTime);

        if (pendingDash)
        {
            pendingDash = false;
            TryDash();
        }

        if (isDashing)
        {
            UpdateDash();
            return;
        }

        if (pendingJump)
        {
            pendingJump = false;

            if (isGrounded)
                Jump();
            else if (isWallJumpUnlocked && isTouchingWall && wallJumpTimer <= 0f)
                WallJump();
            else if (isDoubleJumpUnlocked && extraJumpsRemaining > 0 && wallJumpTimer <= 0f)
                DoubleJump();
        }

        if (pendingJumpCut)
        {
            pendingJumpCut = false;
            if (rb.linearVelocity.y > 0f)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    rb.linearVelocity.y * config.multiplicadorJumpCut);
                AuthorizeCurrentVerticalVelocity();
            }
        }

        UpdateMovement();

        if (wallJumpTimer <= 0f)
        {
            rb.linearVelocity = new Vector2(
                horizontalMovement * config.velocidad,
                rb.linearVelocity.y);
        }

        rb.gravityScale = !isGrounded && rb.linearVelocity.y < 0f
            ? config.gravedadCaida
            : config.gravedadSubida;

        isWallSliding = !isGrounded && isTouchingWall &&
                         rb.linearVelocity.y < 0f && wallJumpTimer <= 0f;

        if (isWallSliding && rb.linearVelocity.y < -config.velocidadDeslizamiento)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -config.velocidadDeslizamiento);
        }

        if (rb.linearVelocity.y < -config.velocidadMaximaCaida)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -config.velocidadMaximaCaida);
        }

        authorizedVerticalVelocity = rb.linearVelocity.y;
        verticalVelocityInitialized = true;
    }

    private void RegisterJump(InputAction.CallbackContext context)
    {
        if (!isPaused && !isShieldInFlight)
            pendingJump = true;
    }

    private void RegisterJumpRelease(InputAction.CallbackContext context)
    {
        if (!isPaused && !isShieldInFlight)
            pendingJumpCut = true;
    }

    private void RegisterDash(InputAction.CallbackContext context)
    {
        if (!isPaused && !isShieldInFlight)
            pendingDash = true;
    }

    private void DetectGround()
    {
        Vector2 origin = groundPoint != null ? (Vector2)groundPoint.position : (Vector2)transform.position;
        RaycastHit2D hit = Physics2D.Raycast(
            origin, Vector2.down, config.longitudRaycast, config.capaSuelo);

        if (isJumping && rb.linearVelocity.y > 0.05f)
        {
            isGrounded = false;
            return;
        }

        isGrounded = hit.collider != null;
    }

    private void DetectWall()
    {
        Vector2 rightOrigin = rightWallPoint != null
            ? (Vector2)rightWallPoint.position : (Vector2)transform.position;
        Vector2 leftOrigin = leftWallPoint != null
            ? (Vector2)leftWallPoint.position : (Vector2)transform.position;

        RaycastHit2D rightHit = Physics2D.Raycast(
            rightOrigin, Vector2.right, config.longitudRaycastPared, config.capaSuelo);
        RaycastHit2D leftHit = Physics2D.Raycast(
            leftOrigin, Vector2.left, config.longitudRaycastPared, config.capaSuelo);

        if (rightHit.collider != null)
        {
            isTouchingWall = true;
            wallDirection = 1;
        }
        else if (leftHit.collider != null)
        {
            isTouchingWall = true;
            wallDirection = -1;
        }
        else
        {
            isTouchingWall = false;
            wallDirection = 0;
        }
    }

    private void PrepareJump()
    {
        isGrounded = false;
        isWallSliding = false;
        isForcedWallFall = false;
        isJumping = true;
    }

    private void Jump()
    {
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            config.fuerzaSalto / Mathf.Max(0.0001f, rb.mass));
        AuthorizeCurrentVerticalVelocity();
        PrepareJump();


        OnJump?.Invoke();
        Events?.NotifyPlayerJumped();
    }

    private void DoubleJump()
    {
        if (!isDoubleJumpUnlocked || extraJumpsRemaining <= 0)
            return;

        extraJumpsRemaining--;
        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            (config.fuerzaSalto * config.multiplicadorDobleSalto) /
            Mathf.Max(0.0001f, rb.mass));
        AuthorizeCurrentVerticalVelocity();
        PrepareJump();

        OnJump?.Invoke();
        Events?.NotifyPlayerJumped();
    }

    private void WallJump()
    {
        if (!isWallJumpUnlocked || wallDirection == 0)
            return;

        int jumpDirection = -wallDirection;
        rb.linearVelocity = new Vector2(
            jumpDirection * config.fuerzaWallJumpX,
            config.fuerzaWallJumpY);
        AuthorizeCurrentVerticalVelocity();

        wallJumpTimer = config.tiempoBloqueoMovimiento;
        facingDirection = jumpDirection;
        ApplyVisualDirection();
        PrepareJump();
        ResetExtraJumps();

        OnWallJump?.Invoke();
        Events?.NotifyPlayerWallJumped();
    }

// Esto es para hacer el pogo jump del ataque en el aire hacia abajo
    public void Bounce()
    {
        if (config != null)
            Bounce(config.fuerzaRebote);
    }


    public void Bounce(float force)
    {
        if (config == null || rb == null || isPaused || isDashing || force <= 0f)
            return;

        pendingJumpCut = false;

        rb.linearVelocity = new Vector2(
            rb.linearVelocity.x,
            force / Mathf.Max(0.0001f, rb.mass));
        AuthorizeCurrentVerticalVelocity();
        PrepareJump();

        OnJump?.Invoke();
        Events?.NotifyPlayerJumped();
    }

 
    private void TryDash()
    {
        if (!isDashUnlocked || isDashing || dashCooldownRemaining > 0f)
            return;

        if (!isGrounded && config.limitarDashAereo && !isAirDashAvailable)
            return;

        if (!isGrounded)
            isAirDashAvailable = false;

        dashDirection = horizontalMovement > 0.1f ? 1 : horizontalMovement < -0.1f ? -1 : facingDirection;
        facingDirection = dashDirection;
        ApplyVisualDirection();

        isDashing = true;
        dashTimeRemaining = config.duracionDash;
        dashCooldownRemaining = config.tiempoRecargaDash;
        wallJumpTimer = 0f;
        isJumping = false;
        isWallSliding = false;
        isForcedWallFall = false;
        pendingJump = false;
        pendingJumpCut = false;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDirection * config.velocidadDash, 0f);
        AuthorizeCurrentVerticalVelocity();

        OnDash?.Invoke();
        Events?.NotifyPlayerDashed();
    }

    private void UpdateDash()
    {
        dashTimeRemaining -= Time.fixedDeltaTime;

        rb.gravityScale = 0f;
        rb.linearVelocity = new Vector2(dashDirection * config.velocidadDash, 0f);
        AuthorizeCurrentVerticalVelocity();

        if (dashTimeRemaining <= 0f)
        {
            isDashing = false;
            dashTimeRemaining = 0f;
            rb.gravityScale = isGrounded || rb.linearVelocity.y >= 0f
                ? config.gravedadSubida : config.gravedadCaida;
            rb.linearVelocity = new Vector2(horizontalMovement * config.velocidad, rb.linearVelocity.y);
            AuthorizeCurrentVerticalVelocity();
        }
    }

    public void AuthorizeCurrentVerticalVelocity()
    {
        if (rb == null)
            return;

        authorizedVerticalVelocity = rb.linearVelocity.y;
        verticalVelocityInitialized = true;
    }

    public void UpdateMovement()
    {
        if (isPaused || isShieldInFlight || wallJumpTimer > 0f)
            return;

        if (isTouchingWall && wallDirection != 0)
            facingDirection = wallDirection;
        else if (horizontalMovement < 0f)
            facingDirection = -1;
        else if (horizontalMovement > 0f)
            facingDirection = 1;

        ApplyVisualDirection();
    }

    private void ApplyVisualDirection()
    {
        if (visualObject == null)
            return;

        Vector3 scale = originalVisualScale;
        scale.x = Mathf.Abs(originalVisualScale.x) * facingDirection;
        visualObject.localScale = scale;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        HandleGroundCollision(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        HandleGroundCollision(collision);
    }

    private void HandleGroundCollision(Collision2D collision)
    {
        if (config == null)
            return;

        if ((config.capaSuelo.value & (1 << collision.gameObject.layer)) == 0)
            return;

        CorrectVerticalImpulse();
    }

    private void CorrectVerticalImpulse()
    {
        if (!verticalVelocityInitialized || config == null)
            return;

        float maximumAllowed = Mathf.Max(0f, authorizedVerticalVelocity);
        if (rb.linearVelocity.y > maximumAllowed + config.toleranciaImpulsoVertical)
        {
            rb.linearVelocity = new Vector2(
                rb.linearVelocity.x, maximumAllowed);
        }
    }

    private void ResetExtraJumps()
    {
        extraJumpsRemaining = isDoubleJumpUnlocked
            ? Mathf.Max(0, config.saltosExtra)
            : 0;
    }

  
    public void UnlockDoubleJump()
    {
        if (config == null || isDoubleJumpUnlocked)
            return;

        isDoubleJumpUnlocked = true;
        ResetExtraJumps();
        Events?.NotifyDoubleJumpUnlocked();
    }

    public void UnlockDash()
    {
        if (config == null || isDashUnlocked)
            return;

        isDashUnlocked = true;
        Events?.NotifyDashUnlocked();
    }

    public void UnlockWallJump()
    {
        if (config == null || isWallJumpUnlocked)
            return;

        isWallJumpUnlocked = true;
        Events?.NotifyWallJumpUnlocked();
    }

    private void HandlePauseChanged(bool paused)
    {
        isPaused = paused;
        if (isPaused)
            ClearPendingInput();

    }

    private void ClearPendingInput()
    {
        pendingJump = false;
        pendingJumpCut = false;
        pendingDash = false;
        horizontalMovement = 0f;
    }

}
