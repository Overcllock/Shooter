using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UniRx.Async;

namespace game 
{

public class Combat
{
  static public uint MAX_WAVES = 3;
  static public int WAVE_DELAY = 3000;
  static public uint ENEMIES_PER_WAVE = 5;
  static public float SPAWN_DELAY_MIN = 1f;
  static public float SPAWN_DELAY_MAX = 3f;

  public uint current_wave = 0;
  public CombatUnit player;

  List<CombatUnit> enemies = new List<CombatUnit>();
  uint current_enemies_count;
  uint all_enemies_count;

  UIHud hud;
  GameObject cam_go;

  Coroutine spawner = null;

  public async void NextWave()
  {
    if(!player.is_alive)
      return;

    current_wave++;
    if(current_wave > MAX_WAVES)
    {
      Game.self.game_loop.TrySwitchTo(GameMode.AfterBattle);
      return;
    }

    hud.ShowWaveText(current_wave);

    await UniTask.Delay(WAVE_DELAY);
    
    if(spawner != null)
    {
      player.StopCoroutine(spawner);
      spawner = null;
    }

    spawner = player.StartCoroutine(SpawnEnemies());
  }

  public void SpawnPlayer()
  {
    var point = Game.self.location.GetChild("PlayerSpawnPoint");
    Error.Verify(point != null);

    cam_go = Assets.TryReuse("Cameras/PlayerCamera");

    var player_go = Assets.TryReuse(prefab: "Characters/Player", activate: false);
    player_go.transform.position = new Vector3(point.transform.position.x, point.transform.position.y, point.transform.position.z);
    player_go.transform.rotation = new Quaternion(point.transform.rotation.x, point.transform.rotation.y, point.transform.rotation.z, point.transform.rotation.w);

    var unit = player_go.GetComponent<CombatUnit>();
    unit.Reset();
    unit.OnDamaged.AddListener(OnPlayerDamaged);

    player = unit;
    player_go.SetActive(true);

    hud = UI.Open<UIHud>();
    hud.Init();
  }

  IEnumerator SpawnEnemies()
  {
    enemies.Clear();

    uint count = current_wave * ENEMIES_PER_WAVE;
    current_enemies_count = all_enemies_count = count;

    hud.UpdateEnemiesCountText(0, all_enemies_count);

    while(count > 0)
    {
      SpawnEnemy();
      yield return new WaitForSeconds(Random.Range(SPAWN_DELAY_MIN, SPAWN_DELAY_MAX));
      count--;
    }
  }

  void SpawnEnemy()
  {
    var enemy = Assets.TryReuse(prefab: "Characters/Zombie", activate: false);
    enemy.transform.position = new Vector3(-50, -10, 150); //invisible point for pre-spawn
    enemy.SetActive(true);

    var unit = enemy.GetComponent<CombatUnit>();
    unit.Reset();
    unit.OnDie.AddListener(OnEnemyDie);

    enemies.Add(unit);

    var ai = enemy.GetComponent<EnemyAI>();
    ai.Init();
  }

  void OnEnemyDie()
  {
    current_enemies_count--;

    hud.UpdateEnemiesCountText(all_enemies_count - current_enemies_count, all_enemies_count);

    if(current_enemies_count <= 0)
      player.StartCoroutine(NextWaveDelayed());
  }

  IEnumerator NextWaveDelayed()
  {
    yield return new WaitForSeconds(1.4f);
    NextWave();
  }

  void OnPlayerDamaged()
  {
    float value = player.hp / player.max_hp;
    hud.UpdateHPBar(value);
  }

  public void Release()
  {
    current_wave = 0;
    player?.Release();

    foreach(var enemy in enemies)
    {
      if(enemy.is_alive)
      {
        var ai = enemy.gameObject.GetComponent<EnemyAI>();
        ai?.Release();
      }

      enemy.Release();
    }
    
    enemies.Clear();

    hud.CloseSelf();
    hud = null;

    Assets.Release(cam_go);
  }
}

} //namespace game