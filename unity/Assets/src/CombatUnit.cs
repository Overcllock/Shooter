using UnityEngine;
using UnityEngine.Events;

namespace game
{

public class CombatUnit : MonoBehaviour
{
  public float attack_radius = 5.0f;
  public float attack_min = 10f;
  public float attack_max = 15f;
  public float max_hp = 200f;

  public float hp;

  [HideInInspector]
  public UnityEvent OnAttack = new UnityEvent();
  [HideInInspector]
  public UnityEvent OnDie = new UnityEvent();
  [HideInInspector]
  public UnityEvent OnDamaged = new UnityEvent();

  public bool is_alive
  {
    get { return hp > 0; }
  }

  void Awake()
  {
    Reset();
  }

  public void Attack(CombatUnit damaged)
  {
    if(!is_alive)
      return;

    var damage = Random.Range(attack_min, attack_max);
    damaged.RecieveDamage(damage);

    OnAttack.Invoke();
  }

  public void RecieveDamage(float damage)
  {
    if(!is_alive)
      return;

    hp = Mathf.Clamp(hp - damage, 0, max_hp);
    OnDamaged.Invoke();

    if(hp == 0)
      OnDie.Invoke();
  }

  public void Reset()
  {
    hp = max_hp;
  }

  public void Release()
  {
    OnAttack.RemoveAllListeners();
    OnDie.RemoveAllListeners();
    OnDamaged.RemoveAllListeners();

    Assets.Release(gameObject);
  }
}

} //namespace game
