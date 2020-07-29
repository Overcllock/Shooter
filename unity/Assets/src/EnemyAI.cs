using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace game
{

public class EnemyAI : MonoBehaviour 
{
  public enum AIState
  {
    Spawn,
    Move,
    Attack,
    Die
  }

  StateMachine<AIState> fsm;

  NavMeshAgent agent;
  Animator animator;
  CombatUnit combat_unit;

  GameObject effects_go;
  ParticleSystem[] effects;

  public bool is_moving
  {
    get { 
      return agent != null && agent.velocity.magnitude > 0.1f; 
    }
  }

  abstract class State
  {
    public abstract AIState GetState();
    public virtual void OnEnter() { }
    public virtual void OnUpdate() { }
    public virtual void OnExit() { }
    
    public StateMachine<AIState> fsm;
    public EnemyAI ai;
  }

  void AddState(State state)
  {
    state.fsm = fsm;
    state.ai = this;
    fsm.Add(state.GetState(), state.OnEnter, state.OnUpdate, state.OnExit);
  }

  void Awake()
  {
    Init();
  }

  void Update()
  {
    if(fsm != null)
      fsm.Update();
  }

  void OnDamaged()
  {
    StopEffects();
    PlayEffects();
  }

  void OnDie()
  {
    if(fsm != null)
      fsm.TrySwitchTo(AIState.Die);
  }

  public void Init()
  {
    agent = gameObject.GetComponent<NavMeshAgent>();
    animator = gameObject.GetComponent<Animator>();
    combat_unit = gameObject.GetComponent<CombatUnit>();

    effects_go = gameObject.GetChild("blood_vfx");
    effects = effects_go.GetComponentsInChildren<ParticleSystem>(true);

    effects_go.SetActive(true);
    StopEffects();

    gameObject.tag = "Enemy";

    combat_unit.OnDie.AddListener(OnDie);
    combat_unit.OnDamaged.AddListener(OnDamaged);

    fsm = new StateMachine<AIState>();
    fsm.enable_log = false;

    AddState(new StateSpawn());
    AddState(new StateMove());
    AddState(new StateAttack());
    AddState(new StateDie());

    fsm.SwitchTo(AIState.Spawn);
  }
  
  void PlayEffects()
  {
    foreach(var effect in effects)
      effect.Play(true);
  }

  void StopEffects()
  {
    foreach(var effect in effects)
      effect.Stop(true);
  }

  public void Release()
  {
    combat_unit.OnDie.RemoveAllListeners();
    combat_unit.OnAttack.RemoveAllListeners();
    combat_unit.OnDamaged.RemoveAllListeners();

    if(fsm != null)
    {
      fsm.Shutdown();
      fsm = null;
    }
  }

  class StateSpawn : State
  {
    static public int MAX_ENEMIES_SPAWNS = 2;

    GameObject point;

    public override AIState GetState() { return AIState.Spawn; }

    public override void OnEnter()
    {
      int spawn_id = Random.Range(0, MAX_ENEMIES_SPAWNS);
      var points = Game.self.location.GetChild("ZombieSpawnPoints");
      point = points.GetChild("ZombieSpawnPoint" + spawn_id);
      Error.Verify(point != null);

      ai.animator.SetBool("IsMoving", false);
      ai.animator.Play("Idle");
    }

    public override void OnUpdate()
    {
      ai.transform.position = new Vector3(point.transform.position.x, point.transform.position.y, point.transform.position.z);
      ai.transform.rotation = new Quaternion(point.transform.rotation.x, point.transform.rotation.y, point.transform.rotation.z, point.transform.rotation.w);

      fsm.SwitchTo(AIState.Move);
    }
  }

  class StateMove : State
  {
    CombatUnit player;
    NavMeshAgent agent;
    Animator animator;

    public override AIState GetState() { return AIState.Move; }

    public override void OnEnter()
    {
      player = Game.self.combat.player;
      agent = ai.agent;
      animator = ai.animator;
      agent.isStopped = false;
      
      if(player == null)
      {
        agent.isStopped = true;
        return;
      }

      agent.SetDestination(player.transform.position);
    }

    public override void OnUpdate()
    {
      UpdateAnimator();

      var hits = Physics.OverlapSphere(ai.transform.position + ai.transform.forward, ai.combat_unit.attack_radius);
      foreach(var hit in hits)
      {
        if(hit.gameObject.tag == "Player")
        {
          fsm.SwitchTo(AIState.Attack);
          return;
        }
      }

      agent.SetDestination(player.transform.position);
    }

    public override void OnExit()
    {
      if(!animator.gameObject.activeSelf)
        return;

      agent.isStopped = true;
      animator.SetBool("IsMoving", false);
      animator.Play("Idle");
    }

    void UpdateAnimator()
    {
      animator.SetBool("IsMoving", ai.is_moving);

      var state = animator.GetCurrentAnimatorStateInfo(0);
      if(!ai.is_moving && state.IsName("Move"))
        animator.Play("Idle");
      else if(ai.is_moving && !state.IsName("Move"))
        animator.Play("Move");
    }
  }

  class StateAttack : State
  {
    CombatUnit player;
    Animator animator;

    public override AIState GetState() { return AIState.Attack; }

    public override void OnEnter()
    {
      player = Game.self.combat.player;
      animator = ai.animator;

      animator.SetBool("IsAttacking", true);
      animator.Play("Attacking");

      ai.StartCoroutine(Attack());
    }

    IEnumerator Attack()
    {
      yield return new WaitForSeconds(0.5f);

      var hits = Physics.OverlapSphere(ai.transform.position + ai.transform.forward, ai.combat_unit.attack_radius);
      foreach(var hit in hits)
      {
        if(hit.tag == "Player")
        {
          ai.combat_unit.Attack(player);
          break;
        }
      }

      yield return new WaitForSeconds(1.0f);

      if(fsm.CurrentState() != AIState.Die)
        fsm.SwitchTo(AIState.Move);
    }

    public override void OnExit()
    {
      if(!animator.gameObject.activeSelf)
        return;

      animator.SetBool("IsAttacking", false);
      animator.Play("Idle");
    }
  }

  class StateDie : State
  {
    static public float RELEASE_DELAY = 1.4f;

    NavMeshAgent agent;
    Animator animator;

    public override AIState GetState() { return AIState.Die; }

    public override void OnEnter()
    {
      agent = ai.agent;
      animator = ai.animator;

      agent.isStopped = true;

      animator.SetBool("IsMoving", false);
      animator.SetBool("IsAttacking", false);
      animator.SetBool("IsDeath", true);
      animator.Play("Death");
      
      ai.StartCoroutine(ReleaseDelayed());
    }

    public override void OnExit()
    {
      Assets.Release(ai.gameObject);
    }

    IEnumerator ReleaseDelayed()
    {
      yield return new WaitForSeconds(RELEASE_DELAY);

      ai.Release();
    }
  }
}

} //namespace game