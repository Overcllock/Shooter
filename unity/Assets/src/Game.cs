using UnityEngine;
using UniRx.Async;

namespace game 
{
  public class Game : MonoBehaviour
  {
    public static Game self;

    public GameLoop game_loop { get; private set; }
    public Combat combat { get; private set; }
    public GameObject location { get; private set; }

    void Awake()
    {
      Init();
    }

    void Init()
    {
      self = this;
      game_loop = new GameLoop();
      combat = new Combat();

      Application.targetFrameRate = 60;

#if UNITY_EDITOR
      Assets.InitForEditor();
#endif

      UI.Init();
      game_loop.Init();
    }

    void Update()
    {
      game_loop.Tick();
    }

    public async UniTask LoadLocation()
    {
      location = await Assets.LoadAsync("Location");
      Error.Verify(location != null);

      Debug.Log("Location loaded succesfully");
    }

    public static void Quit()
    {
      Application.Quit();
    }
  }
}