using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
public class PlayerController : MonoBehaviour
{
	[SerializeField]
	private float moveSpeedX = 3f;

	[SerializeField]
	private float jumpPower = 10f;

	[SerializeField]
	private float wallJumpPower = 20f;

	[SerializeField]
	private float acceleration = 10f;

	[SerializeField]
	private float inputLockDuration = 0.2f;

	[SerializeField]
	private float dashSpeed = 18f;

	[SerializeField]
	private float dashDuration = 0.15f;

	[SerializeField]
	private float dashCooldown = 0.5f;

	[SerializeField]
	private LayerMask groundLayer;

	[SerializeField]
	private Rigidbody2D rb2D;

	[SerializeField]
	private BoxCollider2D boxCollider2D;

	[SerializeField]
	private PlayerInput playerInput;

	private Vector2 moveInput;
	private bool isGrounded;
	private bool isTouchingWallRight;
	private bool isTouchingWallLeft;
	private float inputLockTimer;
	private float dashTimer;
	private float dashCooldownTimer;
	private Vector2 dashDirection = Vector2.right;
	private bool dashAvailable = true;
	private bool isDashing;
	private float startingGravityScale;
	private int selectedCannonIndex;
	private CannonProjectile activeProjectile;
	private bool isDrawHeld;
	private Vector3 drawCursorPosition;
	private float drawStampCooldown;
	public MatchSide ControlledSide { get; private set; } = MatchSide.GoalRunner;

	public bool IsGrounded => isGrounded;
	public bool IsDashing => isDashing;

	private void Awake()
	{
		rb2D ??= GetComponent<Rigidbody2D>();
		boxCollider2D ??= GetComponent<BoxCollider2D>();
		playerInput ??= GetComponent<PlayerInput>();
		startingGravityScale = rb2D.gravityScale;
		drawCursorPosition = transform.position;
		RegisterWithInputManager();
	}

	private void Start()
	{
		RegisterWithInputManager();
	}

	private void OnDestroy()
	{
		if (InputManager.Instance != null)
		{
			InputManager.Instance.UnregisterPlayer(this);
		}
	}

	private void RegisterWithInputManager()
	{
		if (InputManager.Instance == null)
		{
			return;
		}

		InputManager.Instance.RegisterPlayer(this);
	}

	private void FixedUpdate()
	{
		UpdateContactState();
		UpdateDashState();
		UpdateDrawing();

		if (isDashing)
		{
			rb2D.linearVelocity = new Vector2(dashDirection.x * dashSpeed, rb2D.linearVelocity.y);
			return;
		}

		if (inputLockTimer > 0f)
		{
			inputLockTimer -= Time.fixedDeltaTime;
			return;
		}

		float targetSpeedX = moveInput.x * moveSpeedX;
		float currentSpeedX = rb2D.linearVelocity.x;
		float nextSpeedX = Mathf.MoveTowards(currentSpeedX, targetSpeedX, acceleration * Time.fixedDeltaTime);
		rb2D.linearVelocity = new Vector2(nextSpeedX, rb2D.linearVelocity.y);
	}

	private void UpdateContactState()
	{
		isGrounded = Physics2D.OverlapCircle(
			new Vector2(boxCollider2D.bounds.center.x, boxCollider2D.bounds.min.y),
			0.1f,
			groundLayer
		);

		isTouchingWallRight = Physics2D.OverlapBox(
			new Vector2(boxCollider2D.bounds.max.x, boxCollider2D.bounds.center.y),
			new Vector2(0.3f, boxCollider2D.bounds.size.y - 0.2f),
			0f,
			groundLayer
		);

		isTouchingWallLeft = Physics2D.OverlapBox(
			new Vector2(boxCollider2D.bounds.min.x, boxCollider2D.bounds.center.y),
			new Vector2(0.3f, boxCollider2D.bounds.size.y - 0.2f),
			0f,
			groundLayer
		);

		if (isGrounded)
		{
			dashAvailable = true;
		}
	}

	private void UpdateDashState()
	{
		if (dashCooldownTimer > 0f)
		{
			dashCooldownTimer -= Time.fixedDeltaTime;
		}

		if (!isDashing)
		{
			return;
		}

		dashTimer -= Time.fixedDeltaTime;
		if (dashTimer > 0f)
		{
			return;
		}

		isDashing = false;
		rb2D.gravityScale = startingGravityScale;
	}

	public void OnMove(InputValue inputValue)
	{
		moveInput = inputValue.Get<Vector2>();

		if (Mathf.Abs(moveInput.x) > 0.01f)
		{
			dashDirection = new Vector2(Mathf.Sign(moveInput.x), 0f);
		}
	}

	public void OnJump(InputValue inputValue)
	{
		if (!inputValue.isPressed)
		{
			return;
		}

		if (isGrounded)
		{
			rb2D.linearVelocity = new Vector2(rb2D.linearVelocity.x, jumpPower);
			return;
		}

		if (isTouchingWallRight)
		{
			rb2D.linearVelocity = new Vector2(-wallJumpPower, wallJumpPower);
			inputLockTimer = inputLockDuration;
			dashDirection = Vector2.left;
			return;
		}

		if (isTouchingWallLeft)
		{
			rb2D.linearVelocity = new Vector2(wallJumpPower, wallJumpPower);
			inputLockTimer = inputLockDuration;
			dashDirection = Vector2.right;
		}
	}

	public void OnSprint(InputValue inputValue)
	{
		if (!inputValue.isPressed)
		{
			return;
		}

		TryDash();
	}

	public void OnPrevious(InputValue inputValue)
	{
		if (!inputValue.isPressed)
		{
			return;
		}

		if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != MatchPhase.Place)
		{
			return;
		}

		selectedCannonIndex = (selectedCannonIndex + 3) % 4;
	}

	public void OnNext(InputValue inputValue)
	{
		if (!inputValue.isPressed)
		{
			return;
		}

		if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != MatchPhase.Place)
		{
			return;
		}

		selectedCannonIndex = (selectedCannonIndex + 1) % 4;
	}

	public void OnAttack(InputValue inputValue)
	{
		isDrawHeld = inputValue.isPressed;

		if (!inputValue.isPressed)
		{
			return;
		}

		if (GameManager.Instance == null || GameManager.currentState != GameState.Game)
		{
			return;
		}

		if (GameManager.Instance.CurrentPhase == MatchPhase.Place)
		{
			if (GameManager.Instance.CurrentSide != ControlledSide)
			{
				return;
			}

			TryFireCannon(false);
			return;
		}

		if (GameManager.Instance.CurrentPhase == MatchPhase.Race && ControlledSide == MatchSide.Blocker)
		{
			TryFireCannon(true);
		}
	}

	public void OnCrouch(InputValue inputValue)
	{
		if (!inputValue.isPressed)
		{
			return;
		}

		if (activeProjectile != null)
		{
			activeProjectile.StopProjectile();
		}
	}

	public bool TryDash()
	{
		if (!dashAvailable || dashCooldownTimer > 0f || isDashing)
		{
			return false;
		}

		Vector2 direction = dashDirection;
		if (Mathf.Abs(moveInput.x) > 0.01f)
		{
			direction = new Vector2(Mathf.Sign(moveInput.x), 0f);
		}

		if (direction == Vector2.zero)
		{
			direction = Vector2.right;
		}

		isDashing = true;
		dashAvailable = false;
		dashTimer = dashDuration;
		dashCooldownTimer = dashCooldown;
		rb2D.gravityScale = 0f;
		rb2D.linearVelocity = new Vector2(direction.x * dashSpeed, 0f);
		return true;
	}

	public void AssignControlledSide(MatchSide side)
	{
		ControlledSide = side;
	}

	public void ConfigureLocalInput(MatchSide side, bool useGamepad)
	{
		ControlledSide = side;

		if (playerInput == null)
		{
			playerInput = GetComponent<PlayerInput>();
		}

		if (playerInput == null)
		{
			return;
		}

		playerInput.neverAutoSwitchControlSchemes = true;

		if (useGamepad && Gamepad.current != null)
		{
			playerInput.SwitchCurrentControlScheme("Gamepad", Gamepad.current);
			return;
		}

		if (Keyboard.current != null && Mouse.current != null)
		{
			playerInput.SwitchCurrentControlScheme("Keyboard&Mouse", Keyboard.current, Mouse.current);
		}
	}

	private void TryFireCannon(bool isStunProjectile)
	{
		if (GameManager.Instance == null)
		{
			return;
		}

		if (GameManager.Instance.CurrentPhase == MatchPhase.Place && !GameManager.Instance.TryConsumeLaunch(ControlledSide))
		{
			return;
		}

		Transform cannonTransform = GetSelectedCannonTransform();
		if (cannonTransform == null)
		{
			return;
		}

		GameObject projectileObject = new GameObject(isStunProjectile ? "StunProjectile" : "DrawingProjectile");
		projectileObject.transform.position = cannonTransform.position;
		projectileObject.transform.rotation = cannonTransform.rotation;

		SpriteRenderer renderer = projectileObject.AddComponent<SpriteRenderer>();
		renderer.sprite = MatchSceneBootstrap.GetRuntimeSprite();
		renderer.color = isStunProjectile ? new Color(1f, 0.35f, 0.35f, 1f) : new Color(0.95f, 0.95f, 0.95f, 1f);
		renderer.sortingOrder = 2;

		Rigidbody2D projectileRb = projectileObject.AddComponent<Rigidbody2D>();
		projectileRb.gravityScale = 1.4f;
		projectileRb.angularDamping = 0.05f;
		projectileRb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

		CircleCollider2D collider2D = projectileObject.AddComponent<CircleCollider2D>();
		collider2D.radius = 0.18f;

		CannonProjectile projectile = projectileObject.AddComponent<CannonProjectile>();
		projectile.Initialize(this, projectileRb, isStunProjectile);
		activeProjectile = projectile;
	}

	private Transform GetSelectedCannonTransform()
	{
		string[] cannonNames = { "Cannon_TopLeft", "Cannon_TopRight", "Cannon_BottomLeft", "Cannon_BottomRight" };
		selectedCannonIndex = Mathf.Clamp(selectedCannonIndex, 0, cannonNames.Length - 1);
		GameObject cannonObject = GameObject.Find(cannonNames[selectedCannonIndex]);
		return cannonObject != null ? cannonObject.transform : null;
	}

	public void ClearProjectileReference(CannonProjectile projectile)
	{
		if (activeProjectile == projectile)
		{
			activeProjectile = null;
		}
	}

	public void LockInput(float duration)
	{
		inputLockTimer = Mathf.Max(inputLockTimer, duration);
	}

	public void ResetDashAvailability()
	{
		dashAvailable = true;
		dashCooldownTimer = 0f;
		isDashing = false;
		dashTimer = 0f;
		rb2D.gravityScale = startingGravityScale;
	}

	public void ApplyStun(float duration)
	{
		LockInput(duration);
		rb2D.linearVelocity = Vector2.zero;
	}

	private void UpdateDrawing()
	{
		if (GameManager.Instance == null || GameManager.Instance.CurrentPhase != MatchPhase.Draw)
		{
			drawStampCooldown = 0f;
			return;
		}

		Vector2 movement = moveInput;
		drawCursorPosition += new Vector3(movement.x, movement.y, 0f) * 3.2f * Time.fixedDeltaTime;
		drawCursorPosition = ClampToDrawArea(drawCursorPosition);

		if (!isDrawHeld)
		{
			return;
		}

		drawStampCooldown -= Time.fixedDeltaTime;
		if (drawStampCooldown > 0f)
		{
			return;
		}

		drawStampCooldown = 0.12f;
		CreateDrawStamp(drawCursorPosition);
	}

	private Vector3 ClampToDrawArea(Vector3 position)
	{
		Rect area = MatchSceneBootstrap.DrawArea;
		float x = Mathf.Clamp(position.x, area.xMin, area.xMax);
		float y = Mathf.Clamp(position.y, area.yMin, area.yMax);
		return new Vector3(x, y, 0f);
	}

	private void CreateDrawStamp(Vector3 position)
	{
		GameObject stampObject = new GameObject("DrawStamp");
		stampObject.transform.position = position;
		stampObject.transform.localScale = new Vector3(0.28f, 0.28f, 1f);

		SpriteRenderer renderer = stampObject.AddComponent<SpriteRenderer>();
		renderer.sprite = MatchSceneBootstrap.GetRuntimeSprite();
		renderer.color = new Color(0.93f, 0.93f, 0.93f, 1f);
		renderer.sortingOrder = 1;

		BoxCollider2D collider2D = stampObject.AddComponent<BoxCollider2D>();
		collider2D.size = Vector2.one;
	}
}
