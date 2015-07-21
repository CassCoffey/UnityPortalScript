using UnityEngine;
using System.Collections;

public class MoveWithOffset : MonoBehaviour {

    public GameObject parent;
    public Vector3 offset;
	
	// Keeps two objects at identical locations and rotations with an offset.
    // Works well with "interdimensional" portals where they need to be in the same relative location.
	void Update () {
        transform.position = parent.transform.position + offset;
        transform.rotation = parent.transform.rotation;
	}
}
