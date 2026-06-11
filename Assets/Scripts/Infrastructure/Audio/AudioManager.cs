using UnityEngine;

// シーンをまたいで残る、音関連の常駐エントリーポイント。
public class AudioManager : MonoBehaviour
{
	// シングルトン参照を保持する。
	public static AudioManager _audioManager { get; private set; }

	// シーン読み込み前に必要なら自動生成する。
	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		if (_audioManager != null)
		{
			return;
		}

		new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
	}

	// シングルトンを確立し、永続化する。
	private void Awake()
	{
		if (_audioManager != null && _audioManager != this)
		{
			Destroy(gameObject);
			return;
		}

		_audioManager = this;
		DontDestroyOnLoad(gameObject);
	}
}
