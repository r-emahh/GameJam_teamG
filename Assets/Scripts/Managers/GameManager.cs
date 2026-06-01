using UnityEngine;

// ゲームの状態を管理するクラス
public enum GameState
{
	Title,
	Game,
	Pause,
	GameOver
}

public class GameManager : MonoBehaviour
{
	// シングルトンインスタンス
	public static GameManager _gameManager { get; private set; }

	// ゲームの状態を管理する変数
	// 初期値はタイトル画面にしておく
	public static GameState currentState { get; private set; } = GameState.Title;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (_gameManager != null)
		{
			return;
		}

		// 生成後の初期化と永続化はAwakeで一元管理する
		new GameObject(nameof(GameManager)).AddComponent<GameManager>();
	}

	private void Awake()
	{
		// すでに存在する場合は重複を破棄する
		if (_gameManager != null && _gameManager != this)
		{
			Destroy(gameObject);
			return;
		}

		_gameManager = this;
		DontDestroyOnLoad(gameObject);
	}

	public void ChangeState(GameState nextState)
	{
		if (currentState == nextState)
		{
			return;
		}

		currentState = nextState;
	}
}
