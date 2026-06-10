using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum GameState
{
	Title,
	Game,
	Pause,
	GameOver
}

public enum MatchPhase
{
	Idle,
	Draw,
	Place,
	Race,
	Result
}

public enum MatchSide
{
	GoalRunner,
	Blocker
}

public enum MatchResult
{
	None,
	GoalRunnerWin,
	BlockerWin,
	TimeUp
}

public class GameManager : MonoBehaviour
{
	public static GameManager _gameManager { get; private set; }
	public static GameManager Instance => _gameManager;
	public static GameState currentState { get; private set; } = GameState.Title;

	[SerializeField]
	private float drawPhaseDuration = 20f;

	[SerializeField]
	private float placePhaseDuration = 20f;

	[SerializeField]
	private float racePhaseDuration = 60f;

	[SerializeField]
	private float resultPhaseDuration = 6f;

	[SerializeField]
	private int goalRunnerLaunchCount = 3;

	[SerializeField]
	private int blockerLaunchCount = 2;

	public MatchPhase CurrentPhase { get; private set; } = MatchPhase.Idle;
	public MatchSide CurrentSide { get; private set; } = MatchSide.GoalRunner;
	public MatchResult CurrentResult { get; private set; } = MatchResult.None;
	public int CurrentRound { get; private set; } = 0;
	public float CurrentPhaseTimeRemaining { get; private set; }
	public float CurrentPhaseDuration { get; private set; }
	public int GoalRunnerLaunchesRemaining { get; private set; }
	public int BlockerLaunchesRemaining { get; private set; }
	public bool IsMatchRunning => CurrentPhase != MatchPhase.Idle;

	public event Action<GameState> OnGameStateChanged;
	public event Action<MatchPhase> OnMatchPhaseChanged;
	public event Action<MatchSide> OnMatchSideChanged;
	public event Action<int> OnRoundChanged;
	public event Action<float, float> OnPhaseTimerChanged;
	public event Action<MatchResult> OnMatchResultChanged;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
	private static void Bootstrap()
	{
		if (_gameManager != null)
		{
			return;
		}

		new GameObject(nameof(GameManager)).AddComponent<GameManager>();
	}

	private void Awake()
	{
		if (_gameManager != null && _gameManager != this)
		{
			Destroy(gameObject);
			return;
		}

		_gameManager = this;
		DontDestroyOnLoad(gameObject);
		SceneManager.sceneLoaded += HandleSceneLoaded;
	}

	private void OnDestroy()
	{
		if (_gameManager == this)
		{
			SceneManager.sceneLoaded -= HandleSceneLoaded;
			_gameManager = null;
		}
	}

	private void Update()
	{
		if (!IsMatchRunning || currentState != GameState.Game)
		{
			return;
		}

		if (CurrentPhaseTimeRemaining <= 0f)
		{
			AdvancePhase();
			return;
		}

		CurrentPhaseTimeRemaining = Mathf.Max(0f, CurrentPhaseTimeRemaining - Time.unscaledDeltaTime);
		OnPhaseTimerChanged?.Invoke(CurrentPhaseTimeRemaining, GetCurrentPhaseDuration());
	}

	private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if ((scene.name == "Game" || scene.name == "Rema") && currentState == GameState.Game && CurrentPhase == MatchPhase.Idle)
		{
			BeginMatch();
		}
	}

	public void ChangeState(GameState nextState)
	{
		if (currentState == nextState)
		{
			return;
		}

		currentState = nextState;
		OnGameStateChanged?.Invoke(currentState);

		if (currentState == GameState.Title)
		{
			ResetMatch();
		}
	}

	public void BeginMatch()
	{
		ChangeState(GameState.Game);
		CurrentRound = 1;
		CurrentSide = MatchSide.GoalRunner;
		CurrentResult = MatchResult.None;
		SetPhase(MatchPhase.Draw);
		OnRoundChanged?.Invoke(CurrentRound);
		OnMatchSideChanged?.Invoke(CurrentSide);
	}

	public void PauseMatch()
	{
		if (currentState != GameState.Game)
		{
			return;
		}

		ChangeState(GameState.Pause);
	}

	public void ResumeMatch()
	{
		if (currentState != GameState.Pause)
		{
			return;
		}

		ChangeState(GameState.Game);
	}

	public void MarkGoalReached()
	{
		CurrentResult = CurrentSide == MatchSide.GoalRunner ? MatchResult.GoalRunnerWin : MatchResult.BlockerWin;
		OnMatchResultChanged?.Invoke(CurrentResult);
		SetPhase(MatchPhase.Result);
	}

	public void MarkTimeUp()
	{
		CurrentResult = MatchResult.TimeUp;
		OnMatchResultChanged?.Invoke(CurrentResult);
		SetPhase(MatchPhase.Result);
	}

	public bool TryConsumeLaunch(MatchSide side)
	{
		if (side == MatchSide.GoalRunner)
		{
			if (GoalRunnerLaunchesRemaining <= 0)
			{
				return false;
			}

			GoalRunnerLaunchesRemaining--;
			if (GoalRunnerLaunchesRemaining <= 0)
			{
				CurrentSide = MatchSide.Blocker;
				OnMatchSideChanged?.Invoke(CurrentSide);
			}

			return true;
		}

		if (BlockerLaunchesRemaining <= 0)
		{
			return false;
		}

		BlockerLaunchesRemaining--;
		if (BlockerLaunchesRemaining <= 0)
		{
			SetPhase(MatchPhase.Race);
		}

		return true;
	}

	public void ResetMatch()
	{
		CurrentPhase = MatchPhase.Idle;
		CurrentSide = MatchSide.GoalRunner;
		CurrentResult = MatchResult.None;
		CurrentRound = 0;
		CurrentPhaseTimeRemaining = 0f;
		CurrentPhaseDuration = 0f;
		GoalRunnerLaunchesRemaining = 0;
		BlockerLaunchesRemaining = 0;
	}

	private void AdvancePhase()
	{
		switch (CurrentPhase)
		{
			case MatchPhase.Draw:
				SetPhase(MatchPhase.Place);
				break;
			case MatchPhase.Place:
				SetPhase(MatchPhase.Race);
				break;
			case MatchPhase.Race:
				MarkTimeUp();
				break;
			case MatchPhase.Result:
				AdvanceRound();
				break;
			default:
				break;
		}
	}

	private void AdvanceRound()
	{
		CurrentRound++;
		CurrentSide = CurrentSide == MatchSide.GoalRunner ? MatchSide.Blocker : MatchSide.GoalRunner;
		CurrentResult = MatchResult.None;
		OnRoundChanged?.Invoke(CurrentRound);
		OnMatchSideChanged?.Invoke(CurrentSide);
		SetPhase(MatchPhase.Draw);
	}

	private void SetPhase(MatchPhase nextPhase)
	{
		CurrentPhase = nextPhase;

		if (CurrentPhase == MatchPhase.Place)
		{
			GoalRunnerLaunchesRemaining = goalRunnerLaunchCount;
			BlockerLaunchesRemaining = blockerLaunchCount;
		}

		CurrentPhaseDuration = GetCurrentPhaseDuration();
		CurrentPhaseTimeRemaining = CurrentPhaseDuration;
		OnMatchPhaseChanged?.Invoke(CurrentPhase);
		OnPhaseTimerChanged?.Invoke(CurrentPhaseTimeRemaining, CurrentPhaseDuration);
	}

	private float GetCurrentPhaseDuration()
	{
		return CurrentPhase switch
		{
			MatchPhase.Draw => drawPhaseDuration,
			MatchPhase.Place => placePhaseDuration,
			MatchPhase.Race => racePhaseDuration,
			MatchPhase.Result => resultPhaseDuration,
			_ => 0f
		};
	}
}
