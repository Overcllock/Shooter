namespace game
{
  public class UILose : UIWindow
  {
    public static readonly string PREFAB = "ui/ui_lose";

    public override void Init()
    {
      MakeButton("btn_restart", OnRestart);
			MakeButton("btn_exit", OnExit);
    }

    void OnRestart()
    {
      Game.self.game_loop.TrySwitchTo(GameMode.PreparingToBattle);
    }

    void OnExit()
    {
      Game.self.game_loop.TrySwitchTo(GameMode.MainMenu);
    }
  }
}