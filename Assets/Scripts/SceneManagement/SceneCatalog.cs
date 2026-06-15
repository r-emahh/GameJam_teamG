// 本番で使用するシーン名と判定を一元管理する。
public static class SceneCatalog
{
	public const string Title = "Title";
	public const string Match = "Rema";

	public const string TitlePath = "Assets/Scenes/Title.unity";
	public const string MatchPath = "Assets/Scenes/Rema.unity";

	public static bool IsTitle(string sceneName) => sceneName == Title;

	public static bool IsMatch(string sceneName) => sceneName == Match;

	public static bool IsProduction(string sceneName) => IsTitle(sceneName) || IsMatch(sceneName);
}
