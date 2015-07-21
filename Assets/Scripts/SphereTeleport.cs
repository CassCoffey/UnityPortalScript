using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// A special version of the teleport script made for spheres.
/// </summary>
public class SphereTeleport : MonoBehaviour
{
    // The other portal that you will be teleporting to.
    public SphereTeleport otherPortal;

    // Lists used for keeping track of potential objects to teleport.
    private List<GameObject> interiorObjects = new List<GameObject>();
    private List<GameObject> notableObjects = new List<GameObject>();

    // List of tags that will not teleport.
    public List<string> noTeleport;

    // If a new object has entered the trigger, add it to the notable objects list.
    void OnTriggerEnter(Collider collider)
    {
        if (!noTeleport.Contains(collider.tag))
        {
            if (!notableObjects.Contains(collider.gameObject))
            {
                notableObjects.Add(collider.gameObject);
            }
        }
    }

    // If an object has left the trigger, remove it from the lists.
    void OnTriggerExit(Collider collider)
    {
        if (!noTeleport.Contains(collider.tag))
        {
            if (notableObjects.Contains(collider.gameObject))
            {
                notableObjects.Remove(collider.gameObject);
            }
        }
    }

    // Every update, check to see if we should teleport any objects.
    void Update()
    {
        // First check the objects that are inside the portal's teleport area.
        // If they have left the teleport area, they should be teleported.
        for (int i = 0; i < interiorObjects.Count; i++)
        {
            notableObjects.Remove(interiorObjects[i]);
            Vector3 interiorLocation = interiorObjects[i].transform.position;
            if (interiorObjects[i].GetComponentInChildren<Camera>() != null)
            {
                interiorLocation = interiorObjects[i].GetComponentInChildren<Camera>().transform.position;
            }
            if (Vector3.Distance(transform.position, interiorLocation) >= (Mathf.Min(transform.localScale.x, transform.localScale.y, transform.localScale.z) * GetComponent<SphereCollider>().radius) - 0.05f)
            {
                TeleportObject(interiorObjects[i]);
                interiorObjects.RemoveAt(i);
            }
        }
        // Next, check to see if any notable objects have entered the teleport area.
        // If so, add them to the list to be checked next update.
        for (int i = 0; i < notableObjects.Count; i++)
        {
            Vector3 notableLocation = notableObjects[i].transform.position;
            if (notableObjects[i].GetComponentInChildren<Camera>() != null)
            {
                notableLocation = notableObjects[i].GetComponentInChildren<Camera>().transform.position;
            }
            if (Vector3.Distance(transform.position, notableLocation) < (Mathf.Min(transform.localScale.x, transform.localScale.y, transform.localScale.z) * GetComponent<SphereCollider>().radius) - 0.05f)
            {
                interiorObjects.Add(notableObjects[i]);
                notableObjects.RemoveAt(i);
            }
        }
    }

    // Teleport an object to the other portal
    private void TeleportObject(GameObject teleportee)
    {
        // Calculate the object's current position. If it has a camera, its position is set relative to that, to avoid visual errors.
        Vector3 currentLocation = teleportee.transform.position;
        if (teleportee.GetComponentInChildren<Camera>() != null)
        {
            currentLocation = teleportee.GetComponentInChildren<Camera>().transform.position;
        }
        // Find a new position in the same relative location to the other portal.
        Vector3 oldPos = transform.InverseTransformPoint(currentLocation);
        Vector3 newPos = otherPortal.transform.TransformPoint(oldPos);
        newPos += (currentLocation - transform.position).normalized * 0.2f;
        // If we had used the camera, make sure that the new position is offset to account for that.
        if (teleportee.GetComponentInChildren<Camera>() != null)
        {
            newPos += (teleportee.transform.position - teleportee.GetComponentInChildren<Camera>().transform.position);
        }
        if (teleportee.GetComponentInChildren<RigidbodyFirstPersonController>())
        {
            RigidbodyFirstPersonController person = teleportee.GetComponentInChildren<RigidbodyFirstPersonController>();
            person.dimension = !person.dimension;
        }
        // Now we just set the object's location.
        teleportee.transform.position = newPos;
    }
}