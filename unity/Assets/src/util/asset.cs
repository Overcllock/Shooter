using UnityEngine;
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;
using UniRx.Async;

#if UNITY_EDITOR
using UnityEngine.SceneManagement;
#endif

namespace game 
{

public static class Assets
{
  public const string NO_PREFAB = "";
  static List<AssetsPoolItem> pool = new List<AssetsPoolItem>();

  static public int pool_miss;
  static public int pool_hit;
  
#if UNITY_EDITOR
  static Scene main_scene;
  static Scene inactive_scene;

  public static void InitForEditor()
  {
    main_scene = SceneManager.GetActiveScene();
    inactive_scene = SceneManager.CreateScene("Assets Pool"); 
  }
#endif

  class AssetsPoolItem
  {
    public GameObject go;
    public string prefab;
    public bool used;
    public int release_frame;

    public AssetsPoolItem(string prefab, GameObject go, bool used)
    {
      this.prefab = prefab;
      this.go = go;
      Acquire(used);
      this.release_frame = 0;
    }

    public void Acquire(bool use)
    {
      this.used = use;
      if(!use)
        release_frame = Time.frameCount;
      
#if UNITY_EDITOR
      if(inactive_scene.IsValid() && main_scene.IsValid())
        SceneManager.MoveGameObjectToScene(go, use ? main_scene : inactive_scene);
#endif
    }
  }

  public static GameObject Load(string prefab, Transform parent = null)
  {
    if(prefab == NO_PREFAB)
      return new GameObject();
      
    var tpl = ResourceLoad(prefab);
    var go = parent == null
      ? Object.Instantiate(tpl)
      : Object.Instantiate(tpl, parent);

    if(go != null)
      return go;

    var error_message = prefab != string.Empty
      ? "Can't load prefab, path: " + prefab
      : "Can't load prefab, path string is empty.";
    throw new Exception(error_message);
  }
  
  public static async UniTask<GameObject> LoadAsync(string prefab, Transform parent = null)
  {
    if(prefab == NO_PREFAB)
      return new GameObject();
      
    var tpl = await ResourceLoadAsync(prefab);
    var go = parent == null
      ? Object.Instantiate(tpl)
      : Object.Instantiate(tpl, parent);

    if(go != null)
      return go;

    var error_message = prefab != string.Empty
      ? "Can't load prefab, path: " + prefab
      : "Can't load prefab, path string is empty.";
    throw new Exception(error_message);
  }

  public static GameObject TryReuse(string prefab, bool activate = true, Transform parent = null, bool world_position_stays = true)
  {
    var game_object = TryToGetFromPool(prefab);
    if(game_object == null)
    {
      ++pool_miss;
      game_object = Load(prefab);
      AddToPool(prefab, game_object, true);
    }
    else
      ++pool_hit;

    Activate(game_object, activate, parent, world_position_stays);
    return game_object;
  }

  static void AddToPool(string prefab, GameObject go, bool used)
  {
    var pool_item = new AssetsPoolItem(prefab, go, used);
    pool.Add(pool_item);
  }

  public static GameObject TryToGetFromPool(string prefab)
  {
    for(var i = pool.Count; i-- > 0;)
    {
      var item = pool[i];
      if(!MatchesAndFree(item, prefab, check_null: false, check_frame: false))
        continue;

      item.Acquire(true);
      return item.go;
    }

    return null;
  }

  static bool MatchesAndFree(AssetsPoolItem item, string prefab, bool check_null = true, bool check_frame = false)
  {
    return (!check_null || item.go != null) &&
          !item.used &&
          item.prefab == prefab &&
          (!check_frame || item.release_frame < Time.frameCount);
  }

  static void Activate(GameObject go, bool active, Transform parent = null, bool world_position_stays = true)
  {
    go.SetActive(active);
    if(!active)
      go.transform.SetParent(null, world_position_stays);
    else if(parent != null)
      go.transform.SetParent(parent, world_position_stays);
  }

  public static void Release(GameObject go)
  {
    for(var index = 0; index < pool.Count; index++)
    {
      var pool_item = pool[index];
      if(pool_item.go != go)
        continue;

      Activate(go, false);

      var item = pool[index];
      item.Acquire(false);
    }
  }

  public static GameObject ResourceLoad(string prefab)
  {
    return ResourceLoad<GameObject>(prefab);
  }

  public static async UniTask<GameObject> ResourceLoadAsync(string prefab)
  {
    var go = await ResourceLoadAsync<GameObject>(prefab);
    return go;
  }

  public static T ResourceLoad<T>(string prefab) where T : UnityEngine.Object
  {
    var o = Resources.Load<T>(prefab);
    if(o != null)
      return o;

    Error.Verify(false, "Can't load resource: " + prefab);
    return null;
  }

  public static async UniTask<T> ResourceLoadAsync<T>(string prefab) where T : UnityEngine.Object
  {
    var o = await Resources.LoadAsync<T>(prefab);
    if(o != null)
      return o as T;

    Error.Verify(false, "Can't load resource: " + prefab);
    return null;
  }

  public static void UnloadUnused()
  {
    int removed = 0;
    for(var i = pool.Count; i-- > 0;)
    {
      var item = pool[i];

      if(!item.used || item.go == null)
      {
        DestroyAt(i);
        ++removed;
      }
    }
    
    Debug.Log("Assets removed from pool: " + removed);
    Resources.UnloadUnusedAssets();
  }

  static void DestroyAt(int idx)
  {
    var item = pool[idx];
    pool.RemoveAt(idx);
#if UNITY_EDITOR
    if(Application.isPlaying)
#endif
      Object.Destroy(item.go);
  }
}

} //namespace game