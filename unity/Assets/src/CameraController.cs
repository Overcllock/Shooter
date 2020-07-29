using UnityEngine;

namespace game
{

public class CameraController : MonoBehaviour 
{
	public enum Smooth 
	{
    Disabled, 
    Enabled
  };

	public float sensitivity = 2f;
	public float distance = 5f;
	public float height = 2.3f;

	public Smooth smooth = Smooth.Enabled;
	public float speed = 8;

	Transform player;

	void Start()
	{
		player = GameObject.FindGameObjectWithTag("Player").transform;
	}

	void LateUpdate()
	{
		if(player == null)
      return;
      
    transform.RotateAround(player.position, Vector3.up, Input.GetAxis("Mouse X") * sensitivity);

    var position = player.position - transform.rotation * Vector3.forward * distance;
    position = new Vector3(position.x, player.position.y + height, position.z);

    transform.position = smooth == Smooth.Disabled ? position : Vector3.Lerp(transform.position, position, speed * Time.deltaTime);
	}
}

} //namespace game 