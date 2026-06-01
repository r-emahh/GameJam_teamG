using UnityEngine;
using UnityEngine.InputSystem;

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

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	public static void Initialize()
	{
		if (_gameManager == null)
		{
			// シーンにGameManagerが存在しない場合は、新しく作成する
			GameObject gameManagerObject = new GameObject("GameManager");
			_gameManager = gameManagerObject.AddComponent<GameManager>();
			DontDestroyOnLoad(gameManagerObject);
		}
	}
	// シングルトンの実装
	// ゲッターとセッターを使って、外部からアクセスできるようにする
	// しかし、セッターはprivateにして、外部から変更できないようにする
	public static GameManager _gameManager { get; private set; }
	private GameState gameState;


    void Awake()
	{
		
	}

	// Start is called before the first frame update
	void Start()
	{
		
	}

    // Update is called once per frame
    void Update()
	{
		
	}


}
