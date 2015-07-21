using UnityEngine;
using System.Collections;

public class BallTosser : MonoBehaviour {

    public GameObject ball;
    public float force;
	
	// If the player is pressing right mouse, then shoot a ball.
	void Update () {
	    if (Input.GetMouseButtonDown(1))
        {
            GameObject tossedBall = (GameObject)GameObject.Instantiate(ball, transform.position + (transform.up), transform.rotation);
            tossedBall.GetComponent<Rigidbody>().AddForce(transform.up * force);
            Destroy(tossedBall, 5);
        }
	}
}
