using System.Collections;
using UnityEngine;

// シーンをまたいで残る、音関連の常駐エントリーポイント。
[DisallowMultipleComponent]
public sealed class AudioManager : MonoBehaviour
{
	// シングルトン参照を保持する。
	public static AudioManager _audioManager { get; private set; }

	// Resources 配下の音声フォルダ名を設定する。
	private const string AudioResourceFolder = "BGM";

	// 音楽の音量を設定する。
	[SerializeField, Range(0f, 1f)]
	private float musicVolume = 0.42f;

	// SEの音量を設定する。
	[SerializeField, Range(0f, 1f)]
	private float sfxVolume = 0.85f;

	// 音楽再生用のAudioSourceを保持する。
	private AudioSource musicSource;
	// SE再生用のAudioSourceを保持する。
	private AudioSource sfxSource;
	// 現在購読中のGameManagerを保持する。
	private GameManager subscribedManager;
	// 現在再生している音楽トラックを保持する。
	private MusicTrack currentMusicTrack = MusicTrack.None;
	// タイトルBGMを保持する。
	private AudioClip titleBgm;
	// フェーズ共通BGMを保持する。
	private AudioClip phaseBgm;
	// 大砲発射音を保持する。
	private AudioClip cannonFireSfx;
	// 地面衝突音を保持する。
	private AudioClip groundImpactSfx;

	// 再生中の音楽トラックを表す。
	private enum MusicTrack
	{
		None,
		Title,
		Phase
	}

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

	// シングルトンを確立し、再生基盤を構築する。
	private void Awake()
	{
		if (_audioManager != null && _audioManager != this)
		{
			Destroy(gameObject);
			return;
		}

		_audioManager = this;
		DontDestroyOnLoad(gameObject);
		EnsureSources();
		LoadClips();
		SyncMusicToState(GameManager.currentState);
	}

	// GameManager に接続して、現在状態に応じたBGMを同期する。
	private void Start()
	{
		BindToGameManagerIfNeeded();
		if (subscribedManager == null)
		{
			StartCoroutine(WaitForGameManager());
		}
	}

	// 破棄時に購読を解除し、静的参照を外す。
	private void OnDestroy()
	{
		if (subscribedManager != null)
		{
			subscribedManager.OnGameStateChanged -= HandleGameStateChanged;
			subscribedManager = null;
		}

		if (_audioManager == this)
		{
			_audioManager = null;
		}
	}

	// 大砲の発射音を再生する。
	public static void PlayCannonFire()
	{
		_audioManager?.PlaySfx(_audioManager.cannonFireSfx);
	}

	// 地面にぶつかった時の音を再生する。
	public static void PlayGroundImpact()
	{
		_audioManager?.PlaySfx(_audioManager.groundImpactSfx);
	}

	// GameManager に接続できるまで待機する。
	private IEnumerator WaitForGameManager()
	{
		while (_audioManager == this && subscribedManager == null)
		{
			BindToGameManagerIfNeeded();
			if (subscribedManager != null)
			{
				yield break;
			}

			yield return null;
		}
	}

	// GameManager があれば接続して、現在の状態へ音楽を合わせる。
	private void BindToGameManagerIfNeeded()
	{
		if (subscribedManager != null)
		{
			return;
		}

		GameManager manager = GameManager.Instance;
		if (manager == null)
		{
			return;
		}

		subscribedManager = manager;
		subscribedManager.OnGameStateChanged += HandleGameStateChanged;
		SyncMusicToState(GameManager.currentState);
	}

	// ゲーム状態に応じてBGMを切り替える。
	private void HandleGameStateChanged(GameState state)
	{
		SyncMusicToState(state);
	}

	// ゲーム状態に応じてタイトルBGMかフェーズ共通BGMを流す。
	private void SyncMusicToState(GameState state)
	{
		switch (state)
		{
			case GameState.Title:
				PlayMusic(titleBgm, MusicTrack.Title);
				break;
			default:
				PlayMusic(phaseBgm, MusicTrack.Phase);
				break;
		}
	}

	// 指定BGMをループ再生する。
	private void PlayMusic(AudioClip clip, MusicTrack track)
	{
		if (musicSource == null || clip == null)
		{
			return;
		}

		if (currentMusicTrack == track && musicSource.clip == clip && musicSource.isPlaying)
		{
			return;
		}

		musicSource.Stop();
		musicSource.clip = clip;
		musicSource.loop = true;
		musicSource.volume = musicVolume;
		musicSource.Play();
		currentMusicTrack = track;
	}

	// SEを1回だけ再生する。
	private void PlaySfx(AudioClip clip)
	{
		if (sfxSource == null || clip == null)
		{
			return;
		}

		sfxSource.PlayOneShot(clip);
	}

	// 音声再生用のAudioSourceを2系統用意する。
	private void EnsureSources()
	{
		musicSource = CreateSource("MusicSource", loop: true, volume: musicVolume);
		sfxSource = CreateSource("SfxSource", loop: false, volume: sfxVolume);
	}

	// 個別のAudioSourceを作る。
	private AudioSource CreateSource(string sourceName, bool loop, float volume)
	{
		GameObject child = new GameObject(sourceName);
		child.transform.SetParent(transform, false);

		AudioSource source = child.AddComponent<AudioSource>();
		source.playOnAwake = false;
		source.loop = loop;
		source.volume = volume;
		source.spatialBlend = 0f;
		source.dopplerLevel = 0f;
		source.rolloffMode = AudioRolloffMode.Linear;
		return source;
	}

	// Resources から音源を読み込む。
	private void LoadClips()
	{
		AudioClip[] clips = Resources.LoadAll<AudioClip>(AudioResourceFolder);
		titleBgm = FindClip(clips, "ロビー（タイトル？）", "ロビー", "タイトル");
		phaseBgm = FindClip(clips, "絵描きフェーズ（何なら対戦中の共通BGMにしていいと思う）", "絵描きフェーズ", "対戦中");
		cannonFireSfx = FindClip(clips, "大砲発射音", "発射音", "大砲");
		groundImpactSfx = FindClip(clips, "モノが地面にぶつかる音", "地面", "ぶつかる音")
			?? FindClip(clips, "発射物落下音", "発射物", "落下音");

		if (titleBgm == null || phaseBgm == null || cannonFireSfx == null || groundImpactSfx == null)
		{
			Debug.LogWarning("AudioManager: いくつかの音源が Resources/BGM から見つかりませんでした。");
		}
	}

	// 名前候補に一致するAudioClipを探す。
	private static AudioClip FindClip(AudioClip[] clips, params string[] candidates)
	{
		if (clips == null || candidates == null)
		{
			return null;
		}

		foreach (AudioClip clip in clips)
		{
			if (clip == null || string.IsNullOrEmpty(clip.name))
			{
				continue;
			}

			foreach (string candidate in candidates)
			{
				if (string.IsNullOrEmpty(candidate))
				{
					continue;
				}

				if (clip.name == candidate || clip.name.Contains(candidate))
				{
					return clip;
				}
			}
		}

		return null;
	}
}
