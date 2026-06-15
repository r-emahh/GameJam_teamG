using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

public sealed class MatchSessionTests
{
	[Test]
	public void Begin_InitializesFirstRoundState()
	{
		MatchConfiguration configuration = CreateConfiguration(
			drawDuration: 12f,
			goalRunnerLaunches: 4,
			blockerLaunches: 3,
			minGoalRunnerShapes: 5,
			maxGoalRunnerShapes: 5,
			minBlockerShapes: 2,
			maxBlockerShapes: 2);
		MatchSession session = CreateSession(configuration);

		session.Begin();

		AssertRoundStartState(session, configuration, 1, 5, 2);
		Assert.That(session.IsRunning, Is.True);
	}

	[Test]
	public void Tick_TransitionsDrawToPlaceToRaceToResultUsingPhaseDurations()
	{
		MatchConfiguration configuration = CreateConfiguration(
			drawDuration: 2f,
			placeDuration: 3f,
			raceDuration: 4f,
			resultDuration: 5f);
		MatchSession session = CreateSession(configuration);
		session.Begin();

		session.Tick(1f);
		AssertPhase(session, MatchPhase.Draw, 1f, 2f);
		session.Tick(1f);
		AssertPhase(session, MatchPhase.Draw, 0f, 2f);
		session.Tick(0f);
		AssertPhase(session, MatchPhase.Place, 3f, 3f);

		ExpireCurrentPhase(session);
		AssertPhase(session, MatchPhase.Race, 4f, 4f);

		ExpireCurrentPhase(session);
		AssertPhase(session, MatchPhase.Result, 5f, 5f);
		Assert.That(session.Result, Is.EqualTo(MatchResult.TimeUp));
	}

	[Test]
	public void TryConsumeLaunch_ConsumesBothBudgetsAndRejectsFurtherLaunches()
	{
		MatchConfiguration configuration = CreateConfiguration(
			goalRunnerLaunches: 2,
			blockerLaunches: 2);
		MatchSession session = CreateSession(configuration);
		session.Begin();
		ExpireCurrentPhase(session);

		Assert.That(session.TryConsumeLaunch(MatchSide.GoalRunner), Is.True);
		Assert.That(session.GoalRunnerLaunches, Is.EqualTo(1));
		Assert.That(session.BlockerLaunches, Is.EqualTo(2));
		Assert.That(session.TryConsumeLaunch(MatchSide.GoalRunner), Is.True);
		Assert.That(session.TryConsumeLaunch(MatchSide.GoalRunner), Is.False);
		Assert.That(session.GoalRunnerLaunches, Is.Zero);

		Assert.That(session.TryConsumeLaunch(MatchSide.Blocker), Is.True);
		Assert.That(session.BlockerLaunches, Is.EqualTo(1));
		Assert.That(session.TryConsumeLaunch(MatchSide.Blocker), Is.True);
		Assert.That(session.TryConsumeLaunch(MatchSide.Blocker), Is.False);
		Assert.That(session.BlockerLaunches, Is.Zero);
	}

	[Test]
	public void TryConsumeLaunch_WhenBudgetReachesZero_ChangesTurnAndStartsRace()
	{
		MatchConfiguration configuration = CreateConfiguration(
			goalRunnerLaunches: 1,
			blockerLaunches: 1);
		MatchSession session = CreateSession(configuration);
		session.Begin();
		ExpireCurrentPhase(session);

		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Place));
		Assert.That(session.Side, Is.EqualTo(MatchSide.GoalRunner));

		Assert.That(session.TryConsumeLaunch(MatchSide.GoalRunner), Is.True);
		Assert.That(session.GoalRunnerLaunches, Is.Zero);
		Assert.That(session.Side, Is.EqualTo(MatchSide.Blocker));
		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Place));

		Assert.That(session.TryConsumeLaunch(MatchSide.Blocker), Is.True);
		Assert.That(session.BlockerLaunches, Is.Zero);
		Assert.That(session.Side, Is.EqualTo(MatchSide.GoalRunner));
		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Race));
	}

	[Test]
	public void TryMarkGoalReached_InRaceSetsGoalRunnerWin()
	{
		MatchSession session = CreateSession();
		session.Begin();
		ExpireCurrentPhase(session);
		ExpireCurrentPhase(session);

		Assert.That(session.TryMarkGoalReached(MatchSide.GoalRunner), Is.True);
		Assert.That(session.Result, Is.EqualTo(MatchResult.GoalRunnerWin));
		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Result));
		Assert.That(session.TryMarkGoalReached(MatchSide.GoalRunner), Is.False);
	}

	[Test]
	public void Tick_WhenRaceTimeExpiresSetsTimeUpResult()
	{
		MatchSession session = CreateSession(CreateConfiguration(raceDuration: 2f));
		session.Begin();
		ExpireCurrentPhase(session);
		ExpireCurrentPhase(session);

		ExpireCurrentPhase(session);

		Assert.That(session.Result, Is.EqualTo(MatchResult.TimeUp));
		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Result));
	}

	[Test]
	public void ResultExpiry_StartsNextRoundWithFreshState()
	{
		MatchConfiguration configuration = CreateConfiguration(
			goalRunnerLaunches: 2,
			blockerLaunches: 1,
			minGoalRunnerShapes: 3,
			maxGoalRunnerShapes: 3,
			minBlockerShapes: 2,
			maxBlockerShapes: 2);
		MatchSession session = CreateSession(configuration);
		session.Begin();
		Assert.That(session.TryConsumeShape(MatchSide.GoalRunner), Is.True);
		Assert.That(session.TryConsumeShape(MatchSide.Blocker), Is.True);
		Assert.That(session.TryConsumeLaunch(MatchSide.GoalRunner), Is.True);
		AdvanceToRace(session);
		Assert.That(session.TryMarkGoalReached(MatchSide.GoalRunner), Is.True);

		ExpireCurrentPhase(session);

		AssertRoundStartState(session, configuration, 2, 3, 2);
	}

	[Test]
	public void MultipleRounds_ReinitializeBudgetsAndRoundState()
	{
		Queue<int> shapeRolls = new Queue<int>(new[]
		{
			3, 2,
			6, 5,
			4, 3
		});
		MatchConfiguration configuration = CreateConfiguration();
		MatchSession session = new MatchSession(configuration, (min, max) =>
		{
			int value = shapeRolls.Dequeue();
			Assert.That(value, Is.InRange(min, max - 1));
			return value;
		});
		List<(int goalRunner, int blocker)> shapeNotifications = new List<(int, int)>();
		session.ShapeBudgetChanged += (goalRunner, blocker) => shapeNotifications.Add((goalRunner, blocker));

		session.Begin();
		AssertRoundStartState(session, configuration, 1, 3, 2);

		Assert.That(session.TryConsumeShape(MatchSide.GoalRunner), Is.True);
		Assert.That(session.TryConsumeShape(MatchSide.Blocker), Is.True);
		AdvanceToRace(session);
		Assert.That(session.TryMarkGoalReached(MatchSide.GoalRunner), Is.True);
		ExpireCurrentPhase(session);

		AssertRoundStartState(session, configuration, 2, 6, 5);
		Assert.That(session.TryConsumeShape(MatchSide.GoalRunner), Is.True);
		Assert.That(session.TryConsumeLaunch(MatchSide.GoalRunner), Is.True);
		session.MarkTimeUp();
		ExpireCurrentPhase(session);

		AssertRoundStartState(session, configuration, 3, 4, 3);
		Assert.That(shapeRolls, Is.Empty);
		Assert.That(shapeNotifications, Is.EqualTo(new[]
		{
			(3, 2),
			(2, 2),
			(2, 1),
			(6, 5),
			(5, 5),
			(4, 3)
		}));
	}

	[Test, Timeout(1000)]
	public void ZeroSecondDurations_AdvanceWithoutInfiniteLoop()
	{
		MatchConfiguration configuration = CreateConfiguration(
			drawDuration: 0f,
			placeDuration: 0f,
			raceDuration: 0f,
			resultDuration: 0f);
		MatchSession session = CreateSession(configuration);
		session.Begin();

		for (int expectedRound = 1; expectedRound <= 100; expectedRound++)
		{
			Assert.That(session.Round, Is.EqualTo(expectedRound));
			AssertPhase(session, MatchPhase.Draw, 0f, 0f);
			session.Tick(0f);
			AssertPhase(session, MatchPhase.Place, 0f, 0f);
			session.Tick(0f);
			AssertPhase(session, MatchPhase.Race, 0f, 0f);
			session.Tick(0f);
			AssertPhase(session, MatchPhase.Result, 0f, 0f);
			Assert.That(session.Result, Is.EqualTo(MatchResult.TimeUp));
			session.Tick(0f);
		}

		AssertRoundStartState(
			session,
			configuration,
			101,
			configuration.MinDrawingStampCount,
			configuration.MinBlockerStampCount);
	}

	[Test]
	public void TryMarkGoalReached_RejectsNonRacePhasesAndBlocker()
	{
		MatchSession session = CreateSession();
		session.Begin();

		Assert.That(session.TryMarkGoalReached(MatchSide.GoalRunner), Is.False);
		ExpireCurrentPhase(session);
		Assert.That(session.TryMarkGoalReached(MatchSide.GoalRunner), Is.False);
		ExpireCurrentPhase(session);

		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Race));
		Assert.That(session.TryMarkGoalReached(MatchSide.Blocker), Is.False);
		Assert.That(session.Result, Is.EqualTo(MatchResult.None));
		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Race));
	}

	[Test]
	public void RoundStartNotifications_AreSentAfterStateIsComplete()
	{
		Queue<int> shapeRolls = new Queue<int>(new[] { 3, 2, 6, 5 });
		MatchConfiguration configuration = CreateConfiguration();
		MatchSession session = new MatchSession(configuration, (_, _) => shapeRolls.Dequeue());
		List<string> notifications = new List<string>();
		bool captureNotifications = true;

		session.RoundAdvanced += _ =>
		{
			captureNotifications = true;
			notifications.Add(nameof(session.RoundAdvanced));
			AssertRoundStartState(session, configuration, session.Round, 6, 5);
		};
		session.RoundChanged += _ => Capture(nameof(session.RoundChanged));
		session.ResultChanged += _ => Capture(nameof(session.ResultChanged));
		session.SideChanged += _ => Capture(nameof(session.SideChanged));
		session.LaunchBudgetChanged += (_, _) => Capture(nameof(session.LaunchBudgetChanged));
		session.ShapeBudgetChanged += (_, _) => Capture(nameof(session.ShapeBudgetChanged));
		session.PhaseChanged += _ => Capture(nameof(session.PhaseChanged));
		session.TimerChanged += (_, _) =>
		{
			Capture(nameof(session.TimerChanged));
			captureNotifications = false;
		};

		session.Begin();
		Assert.That(notifications, Is.EqualTo(new[]
		{
			nameof(session.RoundChanged),
			nameof(session.ResultChanged),
			nameof(session.SideChanged),
			nameof(session.LaunchBudgetChanged),
			nameof(session.ShapeBudgetChanged),
			nameof(session.PhaseChanged),
			nameof(session.TimerChanged)
		}));

		notifications.Clear();
		session.MarkTimeUp();
		ExpireCurrentPhase(session);

		Assert.That(notifications, Is.EqualTo(new[]
		{
			nameof(session.RoundAdvanced),
			nameof(session.RoundChanged),
			nameof(session.ResultChanged),
			nameof(session.SideChanged),
			nameof(session.LaunchBudgetChanged),
			nameof(session.ShapeBudgetChanged),
			nameof(session.PhaseChanged),
			nameof(session.TimerChanged)
		}));

		void Capture(string notification)
		{
			if (!captureNotifications)
			{
				return;
			}

			notifications.Add(notification);
			int expectedGoalRunnerShapes = session.Round == 1 ? 3 : 6;
			int expectedBlockerShapes = session.Round == 1 ? 2 : 5;
			AssertRoundStartState(
				session,
				configuration,
				session.Round,
				expectedGoalRunnerShapes,
				expectedBlockerShapes);
		}
	}

	private static void AssertRoundStartState(
		MatchSession session,
		MatchConfiguration configuration,
		int expectedRound,
		int expectedGoalRunnerShapes,
		int expectedBlockerShapes)
	{
		Assert.That(session.Round, Is.EqualTo(expectedRound));
		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Draw));
		Assert.That(session.Result, Is.EqualTo(MatchResult.None));
		Assert.That(session.Side, Is.EqualTo(MatchSide.GoalRunner));
		Assert.That(session.GoalRunnerLaunches, Is.EqualTo(configuration.GoalRunnerLaunchCount));
		Assert.That(session.BlockerLaunches, Is.EqualTo(configuration.BlockerLaunchCount));
		Assert.That(session.GoalRunnerShapes, Is.EqualTo(expectedGoalRunnerShapes));
		Assert.That(session.BlockerShapes, Is.EqualTo(expectedBlockerShapes));
		Assert.That(session.PhaseDuration, Is.EqualTo(configuration.GetDuration(MatchPhase.Draw)));
		Assert.That(session.TimeRemaining, Is.EqualTo(configuration.GetDuration(MatchPhase.Draw)));
	}

	private static void AssertPhase(
		MatchSession session,
		MatchPhase expectedPhase,
		float expectedTimeRemaining,
		float expectedDuration)
	{
		Assert.That(session.Phase, Is.EqualTo(expectedPhase));
		Assert.That(session.TimeRemaining, Is.EqualTo(expectedTimeRemaining));
		Assert.That(session.PhaseDuration, Is.EqualTo(expectedDuration));
	}

	private static void AdvanceToRace(MatchSession session)
	{
		while (session.Phase == MatchPhase.Draw || session.Phase == MatchPhase.Place)
		{
			ExpireCurrentPhase(session);
		}

		Assert.That(session.Phase, Is.EqualTo(MatchPhase.Race));
	}

	private static MatchSession CreateSession(MatchConfiguration configuration = null)
	{
		configuration ??= CreateConfiguration();
		return new MatchSession(configuration, (min, _) => min);
	}

	private static MatchConfiguration CreateConfiguration(
		float drawDuration = 20f,
		float placeDuration = 20f,
		float raceDuration = 60f,
		float resultDuration = 6f,
		int goalRunnerLaunches = 3,
		int blockerLaunches = 2,
		int minGoalRunnerShapes = 3,
		int maxGoalRunnerShapes = 6,
		int minBlockerShapes = 2,
		int maxBlockerShapes = 5)
	{
		MatchConfiguration configuration = new MatchConfiguration();
		SetPrivateField(configuration, "drawPhaseDuration", drawDuration);
		SetPrivateField(configuration, "placePhaseDuration", placeDuration);
		SetPrivateField(configuration, "racePhaseDuration", raceDuration);
		SetPrivateField(configuration, "resultPhaseDuration", resultDuration);
		SetPrivateField(configuration, "goalRunnerLaunchCount", goalRunnerLaunches);
		SetPrivateField(configuration, "blockerLaunchCount", blockerLaunches);
		SetPrivateField(configuration, "minDrawingStampCount", minGoalRunnerShapes);
		SetPrivateField(configuration, "maxDrawingStampCount", maxGoalRunnerShapes);
		SetPrivateField(configuration, "minBlockerStampCount", minBlockerShapes);
		SetPrivateField(configuration, "maxBlockerStampCount", maxBlockerShapes);
		return configuration;
	}

	private static void SetPrivateField<T>(MatchConfiguration configuration, string fieldName, T value)
	{
		FieldInfo field = typeof(MatchConfiguration).GetField(
			fieldName,
			BindingFlags.Instance | BindingFlags.NonPublic);
		Assert.That(field, Is.Not.Null, $"MatchConfiguration.{fieldName} was not found.");
		field.SetValue(configuration, value);
	}

	private static void ExpireCurrentPhase(MatchSession session)
	{
		if (session.TimeRemaining > 0f)
		{
			session.Tick(session.TimeRemaining);
		}

		session.Tick(0f);
	}
}
