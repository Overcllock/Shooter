namespace game
{
  public class UIWin : UIWindow
  {
    public static readonly string PREFAB = "ui/ui_win";

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