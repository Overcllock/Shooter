using UnityEngine;
using System.Collections;

namespace game
{

public class PlayerController : MonoBehaviour 
{
	public float move_speed = 7.0f;
  public float rotate_speed = 10.0f;
  public float shoot_delay = 0.25f;

	CharacterController cctl;
  CombatUnit player;
  Animator animator;
  Camera cam;

  GameObject effects_go;
  ParticleSystem[] effects;

  float shoot_cooldown = 0;

  public bool is_moving
  {
    get { return cctl != null && cctl.velocity.magnitude > 0.1f; }
  }
	
	void Awake()
	{
		cctl = GetComponent<CharacterController>();
    player = GetComponent<CombatUnit>();
    animator = GetComponent<Animator>();
    cam = Camera.main;

    effects_go = gameObject.GetChild("shot_vfx");
    effects = effects_go.GetComponentsInChildren<ParticleSystem>(true);

    effects_go.SetActive(true);
    StopEffects();

    gameObject.tag = "Player";
	}

  void OnEnable()
  {
    player.OnDie.AddListener(OnDie);
  }

	void Update()
	{
    if(!player.is_alive)
      return;

    ProcessMovement();
    ProcessAnimate();
    ProcessShooting();
	}

  void ProcessMovement()
  {
    if(Input.GetMouseButton(1))
    {
      var mouse_pos = Input.mousePosition;

      var point = cam.ScreenToWorldPoint(new Vector3(mouse_pos.x, mouse_pos.y, cam.farClipPlane));
      point.y = 0;

      transform.LookAt(point);
      return;
    }

    var input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));

		var direction = new Vector3(input.x, 0, input.y);
		direction = cam.transform.TransformDirection(direction);
		direction = new Vector3(direction.x, 0, direction.z);

		if(Mathf.Abs(input.y) > 0 || Mathf.Abs(input.x) > 0)
			transform.rotation = Quaternion.Lerp(transform.rotation, Quaternion.LookRotation(direction), rotate_speed * Time.deltaTime);

    float speed = move_speed * input.normalized.magnitude;
    cctl.Move(transform.forward * speed * Time.deltaTime);
  }

  void ProcessAnimate()
  {
    animator.SetBool("IsMoving", is_moving);

    var state = animator.GetCurrentAnimatorStateInfo(0);
    if(!is_moving && state.IsName("Move"))
      animator.Play("Idle");
    else if(is_moving && !state.IsName("Move"))
      animator.Play("Move");
  }

  void ProcessShooting()
  {
    shoot_cooldown = Mathf.Clamp(shoot_cooldown - Time.deltaTime, 0, shoot_delay);

    if(!Input.GetMouseButton(0) || shoot_cooldown > 0)
    {
      StopEffects();
      return;
    }

    shoot_cooldown = shoot_delay;

    PlayEffects();

    RaycastHit hit;
    var ray = new Ray(transform.position + Vector3.up, transform.forward * player.attack_radius);
    if(Physics.Raycast(ray, out hit))
    {
      if(hit.transform.tag == "Enemy")
      {
        var damaged = hit.transform.GetComponent<CombatUnit>();
        if(damaged != null)
          player.Attack(damaged);
      }
    }
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

  void OnDie()
  {
    StartCoroutine(Die());
  }

  IEnumerator Die()
  {
    animator.SetBool("IsMoving", false);
    animator.SetBool("IsShooting", false);
    animator.SetBool("IsDeath", true);
    animator.Play("Death");

    yield return new WaitForSeconds(1.4f);

    Game.self.game_loop.TrySwitchTo(GameMode.AfterBattle);
  }
}

} //namespace game