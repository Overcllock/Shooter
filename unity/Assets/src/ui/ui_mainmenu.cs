namespace game
{
  public class UIMainMenu : UIWindow
  {
    public static readonly string PREFAB = "ui/ui_mainmenu";

    public override void Init()
    {
      MakeButton("btn_start", OnStart);
			MakeButton("btn_exit", Game.Quit);
    }

    void OnStart()
    {
      Game.self.game_loop.TrySwitchTo(GameMode.PreparingToBattle);
    }
  }
}