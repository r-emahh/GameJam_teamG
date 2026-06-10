using UnityEngine;

public class AudioManager : MonoBehaviour
{
	public static AudioManager _audioManager { get; private set; }

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Initialize()
	{
		if (_audioManager != null)
		{
			return;
		}

		new GameObject(nameof(AudioManager)).AddComponent<AudioManager>();
	}

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
