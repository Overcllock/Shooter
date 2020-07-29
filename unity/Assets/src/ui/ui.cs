using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using System.Collections.Generic;
using UniRx.Async;

namespace game 
{

public class UI 
{
  public static GameObject root;
  public static Camera camera;

  [HideInInspector]
  public static List<UIWindow> windows = new List<UIWindow>();

  const int EVENT_SYSTEM_DRAG_THRESHOLD = 5;

  static GameObject windows_container;
  static GameObject loading_go;
  static EventSystem event_system;

  public static void Init()
  {
    root = Assets.Load("UI/ui_root");
    Error.Verify(root != null);

    loading_go = root.GetChild("loading");
    loading_go.SetActive(false);

    windows_container = root.GetChild("windows");

    camera = root.GetChild("camera").GetComponent<Camera>();
    Error.Verify(camera != null);
    camera.GetComponent<AudioListener>().enabled = false;
    
    event_system = root.GetComponent<EventSystem>();
    Error.Verify(event_system != null);
    event_system.pixelDragThreshold = EVENT_SYSTEM_DRAG_THRESHOLD;
  }

  public static T Open<T>() where T : UIWindow
  {
    string prefab = GetPrefab(typeof(T));
    var ui_window_go = Assets.Load(prefab, windows_container.transform);
    var window = ui_window_go.AddComponentOnce<T>();
    if(window != null)
      windows.Add(window);

    return window as T;
  }

  public static async UniTask<T> OpenAsync<T>() where T : UIWindow
  {
    string prefab = GetPrefab(typeof(T));
    var ui_window_go = await Assets.LoadAsync(prefab, windows_container.transform);
    var window = ui_window_go.AddComponentOnce<T>();
    if(window != null)
      windows.Add(window);

    return window as T;
  }

  public static T Find<T>() where T : UIWindow
  {
    foreach(var window in windows)
    {
      if(window is T)
        return window as T;
    }

    return null;
  }

  public static void CloseAllWindows()
  {
    foreach(var window in windows)
      GameObject.Destroy(window.gameObject);
    
    windows.Clear();
  }

  public static void ShowLoading()
  {
    loading_go.SetActive(true);
  }

  public static void HideLoading()
  {
    loading_go.SetActive(false);
  }

  static string GetPrefab(System.Type type)
  {
    var field = type.GetField("PREFAB");
    if(field == null)
    {
      Debug.LogError("Field \"PREFAB\" not found in type " + type.Name);
      return string.Empty;
    }

    object val = field.GetValue(null);
    string prefab = val as string;

    if(prefab == null)
      return string.Empty;
    return prefab;
  }
}

public abstract class UIWindow : MonoBehaviour
{
  public abstract void Init();

  public void CloseSelf()
  {
    UI.windows.Remove(this);
    Destroy(gameObject);
  }

  protected void MakeButton(string path, UnityAction func, bool set_active = true)
  {
    var btn_go = transform.FindRecursive(path);
    var button = btn_go.GetComponent<Button>();
    if(button != null)
    {
      button.onClick.AddListener(func);
      button.gameObject.SetActive(set_active);
    }
  }

  protected T GetUIComponent<T>(string name) where T : Component
  {
    return transform.FindRecursive(name).GetComponent<T>();
  }
}

} //namespace game