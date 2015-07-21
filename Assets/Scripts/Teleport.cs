using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;
using System.Collections.Generic;

public class Teleport : MonoBehaviour {

    public Teleport otherPortal;

    public bool mirror;

    public List<string> noTeleport;

    private Hashtable objectLocations = new Hashtable();
    private List<GameObject> notableObjects = new List<GameObject>();

    void OnTriggerEnter(Collider collider)
    {
        if (!noTeleport.Contains(collider.tag))
        {
            if (!notableObjects.Contains(collider.gameObject))
            {
                notableObjects.Add(collider.gameObject);
            }
            if (collider.GetComponentInChildren<Camera>() != null)
            {
                objectLocations.Add(collider.gameObject, collider.GetComponentInChildren<Camera>().transform.position);
            }
            else
            {
                objectLocations.Add(collider.gameObject, collider.transform.position);
            }
        }
    }

    void OnTriggerExit(Collider collider)
    {
        if (!noTeleport.Contains(collider.tag))
        {
            if (notableObjects.Contains(collider.gameObject))
            {
                objectLocations.Remove(collider.gameObject);
                notableObjects.Remove(collider.gameObject);
            }
        }
    }

    void FixedUpdate()
    {
        foreach (GameObject notable in notableObjects)
        {
            if (SideFlipped(notable))
            {
                TeleportObject(notable);
            }
        }
    }

    private bool SideFlipped(GameObject target)
    {
        float originalDot;
        Vector3 originalLocation = (Vector3)objectLocations[target];
        float actualDot = Vector3.Dot(transform.forward, originalLocation - transform.position);
        Vector3 offset = Vector3.zero;
        if (actualDot < 0)
        {
            offset = transform.position - (transform.forward * 0.05f);
        }
        else if (actualDot > 0)
        {
            offset = transform.position + (transform.forward * 0.05f);
        }
        else
        {
            offset = transform.position + (transform.forward * 0.05f);
        }
        originalDot = Vector3.Dot(transform.forward, originalLocation - offset);
        if ((actualDot > 0 && originalDot < 0) || (actualDot < 0 && originalDot > 0))
        {
            return true;
        }
        float currentDot;
        if (target.GetComponentInChildren<Camera>() != null)
        {
            currentDot = Vector3.Dot(transform.forward, target.GetComponentInChildren<Camera>().transform.position - offset);
        }
        else
        {
            currentDot = Vector3.Dot(transform.forward, target.transform.position - offset);
        }
        if ((currentDot > 0 && originalDot < 0) || (currentDot < 0 && originalDot > 0))
        {
            return true;
        }
        return false;
    }

    private void TeleportObject(GameObject teleportee)
    {
        Vector3 originalLocation = (Vector3)objectLocations[teleportee];
        float actualDot = Vector3.Dot(transform.forward, originalLocation - transform.position);
        Vector3 offset = Vector3.zero;
        if (actualDot < 0)
        {
            offset = transform.position - (transform.forward * 0.05f);
        }
        else if (actualDot > 0)
        {
            offset = transform.position + (transform.forward * 0.05f);
        }
        else
        {
            offset = transform.position + (transform.forward * 0.05f);
        }
        float currentDot;
        Vector3 currentLocation = Vector3.zero;
        if (teleportee.GetComponentInChildren<Camera>() != null)
        {
            currentLocation = teleportee.GetComponentInChildren<Camera>().transform.position;
        }
        else
        {
            currentLocation = teleportee.transform.position;
        }
        currentDot = Vector3.Dot(transform.forward, currentLocation - offset);
        Vector3 oldPos = transform.InverseTransformPoint(currentLocation);
        Vector3 newPos = otherPortal.transform.TransformPoint(oldPos);
        Vector3 newOffset = otherPortal.transform.forward * 0.1f;
        if (mirror)
        {
            newOffset = -newOffset;
        }
        if (actualDot < 0)
        {
            newPos += newOffset;
        }
        else if (actualDot > 0)
        {
            newPos -= newOffset;
        }
        else if (actualDot == 0)
        {
            if (currentDot > 0)
            {
                newPos += newOffset;
            }
            else if (currentDot < 0)
            {
                newPos -= newOffset;
            }
        }
        if (teleportee.GetComponentInChildren<Camera>() != null)
        {
            newPos += (teleportee.transform.position - teleportee.GetComponentInChildren<Camera>().transform.position);
        }
        if (mirror)
        {
            teleportee.GetComponent<Rigidbody>().velocity = Vector3.Reflect(teleportee.GetComponent<Rigidbody>().velocity, transform.forward);
            teleportee.transform.rotation = Quaternion.LookRotation(Vector3.Reflect(teleportee.transform.forward, transform.forward), teleportee.transform.up);
        }
        if (teleportee.GetComponentInChildren<RigidbodyFirstPersonController>())
        {
            RigidbodyFirstPersonController person = teleportee.GetComponentInChildren<RigidbodyFirstPersonController>();
            person.dimension = !person.dimension;
            if (mirror)
            {
                person.movementSettings.CurrentTargetSpeed = -person.movementSettings.CurrentTargetSpeed;
            }
        }
        teleportee.transform.position = newPos;
    }
}
