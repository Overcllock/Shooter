using UnityEngine;

namespace game 
{

public enum GameMode
{
  None,
  Loading,
  MainMenu,
  PreparingToBattle,
  AfterBattle
}

public class GameLoop
{
  StateMachine<GameMode> fsm;

  public GameMode current_mode => fsm.CurrentState();

  abstract class State
  {
    public abstract GameMode GetMode();
    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
    
    public StateMachine<GameMode> fsm;
  }

  void AddState(State state)
  {
    state.fsm = fsm;
    fsm.Add(state.GetMode(), state.OnEnter, state.OnUpdate, state.OnExit);
  }
  
  public void Init()
  {
    fsm = new StateMachine<GameMode>();
    
    AddState(new StateLoading());
    AddState(new StateMainMenu());
    AddState(new StatePreparingToBattle());
    AddState(new StateAfterBattle());
    
    fsm.SwitchTo(GameMode.Loading);
  }

  public void Tick()
  {
    fsm.Update();
  }

  public bool TrySwitchTo(GameMode state)
  {
    if(fsm.CurrentState() == state)
      return false;
    SwitchTo(state);
    return true;
  }

  void SwitchTo(GameMode state)
  {
    fsm.SwitchTo(state);
  }

  class StateLoading : State
  {
    public override GameMode GetMode() { return GameMode.Loading; }

    public override void OnEnter()
    {
      UI.ShowLoading();
      LoadLocation();
    }

    public override void OnExit()
    {
      UI.HideLoading();
    }

    async void LoadLocation()
    {
      await Game.self.LoadLocation();
      fsm.SwitchTo(GameMode.MainMenu);
    }
  }

  class StateMainMenu : State
  {
    public override GameMode GetMode() { return GameMode.MainMenu; }

    UIMainMenu ui;

    public override void OnEnter()
    {
      Assets.UnloadUnused();
      UI.CloseAllWindows();

      ui = UI.Open<UIMainMenu>();
      ui.Init();
    }

    public override void OnExit()
    {
      ui.CloseSelf();
    }
  }

  class StatePreparingToBattle : State
  {
    public override GameMode GetMode() { return GameMode.PreparingToBattle; }

    public override void OnEnter()
    {
      UI.CloseAllWindows();

      Cursor.lockState = CursorLockMode.Locked;
      Cursor.visible = false;

      var combat = Game.self.combat;
      combat.SpawnPlayer();
      combat.NextWave();
    }
  }

  class StateAfterBattle : State
  {
    public override GameMode GetMode() { return GameMode.AfterBattle; }

    public override void OnEnter()
    {
      var combat = Game.self.combat;
      Cursor.lockState = CursorLockMode.None;
      Cursor.visible = true;

      UIWindow ui;
      if(combat.player.is_alive)
        ui = UI.Open<UIWin>();
      else
        ui = UI.Open<UILose>();
      
      ui.Init();

      combat.Release();
    }
  }
}

} //namespace game
