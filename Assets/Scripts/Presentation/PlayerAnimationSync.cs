using UnityEngine;

[DisallowMultipleComponent]
// プレイヤーの移動状態を Animator パラメータへ反映する。
public sealed class PlayerAnimationSync : MonoBehaviour
{
	// Animator を直接指定したい場合の差し替え先。
	[SerializeField]
	private Animator animator;

	// ゲーム開始時に差し込む Animator Controller。
	[SerializeField]
	private RuntimeAnimatorController runtimeAnimatorController;

	// 接地情報の参照元。
	[SerializeField]
	private PlayerContactSensor2D contactSensor;

	// 速度参照元。
	[SerializeField]
	private Rigidbody2D body;

	// 速度パラメータ名。
	[SerializeField]
	private string speedParameterName = "Speed";

	// 接地パラメータ名。
	[SerializeField]
	private string groundedParameterName = "Grounded";

	// ジャンプトリガー名。
	[SerializeField]
	private string jumpTriggerName = "Jump";

	private int speedParameterHash;
	private int groundedParameterHash;
	private int jumpTriggerHash;

	private void Awake()
	{
		CacheReferences();
		ApplyRuntimeAnimatorController();
		UpdateParameterHashes();
	}

	private void OnValidate()
	{
		UpdateParameterHashes();
	}

	private void Update()
	{
		RefreshAnimatorParameters();
	}

	// ジャンプ開始時のアニメーションを鳴らす。
	public void TriggerJump()
	{
		CacheReferences();
		ApplyRuntimeAnimatorController();
		UpdateParameterHashes();
		if (animator != null && jumpTriggerHash != 0)
		{
			animator.SetTrigger(jumpTriggerHash);
		}
	}

	// 外部から共通 Animator Controller を割り当てる。
	public void AssignRuntimeAnimatorController(RuntimeAnimatorController controller)
	{
		if (controller == null)
		{
			return;
		}

		runtimeAnimatorController = controller;
		CacheReferences();
		ApplyRuntimeAnimatorController();
		ResetAnimatorState();
	}

	// 生成直後やリスポーン時に Animator を現在状態へ戻す。
	public void ResetAnimatorState()
	{
		CacheReferences();
		ApplyRuntimeAnimatorController();
		UpdateParameterHashes();
		if (animator == null)
		{
			return;
		}

		animator.Rebind();
		RefreshAnimatorParameters();
		animator.Update(0f);
	}

	private void RefreshAnimatorParameters()
	{
		CacheReferences();
		ApplyRuntimeAnimatorController();
		UpdateParameterHashes();
		if (animator == null)
		{
			return;
		}

		float speed = body != null ? Mathf.Abs(body.linearVelocity.x) : 0f;
		bool grounded = contactSensor != null && contactSensor.IsGrounded;

		if (speedParameterHash != 0)
		{
			animator.SetFloat(speedParameterHash, speed);
		}

		if (groundedParameterHash != 0)
		{
			animator.SetBool(groundedParameterHash, grounded);
		}
	}

	private void CacheReferences()
	{
		if (animator == null)
		{
			animator = GetComponent<Animator>();
			if (animator == null)
			{
				animator = GetComponentInChildren<Animator>(true);
			}

			if (animator == null)
			{
				animator = gameObject.AddComponent<Animator>();
			}
		}

		if (contactSensor == null)
		{
			contactSensor = GetComponent<PlayerContactSensor2D>();
		}

		if (body == null)
		{
			body = GetComponent<Rigidbody2D>();
		}
	}

	private void ApplyRuntimeAnimatorController()
	{
		if (animator != null
			&& runtimeAnimatorController != null
			&& animator.runtimeAnimatorController != runtimeAnimatorController)
		{
			animator.runtimeAnimatorController = runtimeAnimatorController;
		}
	}

	private void UpdateParameterHashes()
	{
		speedParameterHash = string.IsNullOrEmpty(speedParameterName) ? 0 : Animator.StringToHash(speedParameterName);
		groundedParameterHash = string.IsNullOrEmpty(groundedParameterName) ? 0 : Animator.StringToHash(groundedParameterName);
		jumpTriggerHash = string.IsNullOrEmpty(jumpTriggerName) ? 0 : Animator.StringToHash(jumpTriggerName);
	}
}
