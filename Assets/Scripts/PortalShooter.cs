using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;
using System.Collections;

/// <summary>
/// This is an example class to show one way to spawn portals at runtime.
/// </summary>
public class PortalShooter : MonoBehaviour {

    // This is our portal prefab, it will be created when the player presses left mouse.
    public GameObject portalPrefab;

    // This is the player, its only purpose is to keep track of which dimension we are in.
    public RigidbodyFirstPersonController player;

    // This determines how far away from the player portals are created.
    public float range;

    // This determines the offset between dimensions.
    public float dimensionOffset;

    // This keeps track of the currently opened portals.
    private GameObject[] openedPortals = new GameObject[2];
	
	// If the player pressed the left mouse button, create a portal.
	void Update () {
	    if (Input.GetMouseButtonDown(0))
        {
            FirePortal();
        }
	}

    // Either creates a new portal, or closes an old one.
    private void FirePortal()
    {
        // If we already have portals open, then close them.
        if (openedPortals[0] != null)
        {
            // This uses the waver script to give the portals a closing animation.
            openedPortals[0].GetComponent<Waver>().Close();
            openedPortals[1].GetComponent<Waver>().Close();
            openedPortals[0] = null;
            openedPortals[1] = null;
        }
        // If we don't have any portals open, make a new set.
        else
        {
            // Play the particle system, to look cool.
            GetComponentInChildren<ParticleSystem>().Play();
            // Instantiate a new portal facing the player
            GameObject portalOne = (GameObject)GameObject.Instantiate(portalPrefab, transform.position + (transform.up * range), Quaternion.LookRotation(transform.up, Vector3.up));
            // Depending on which dimension the player is in, the second portal is placed at an offset.
            Vector3 portalTwoLocation = portalOne.transform.position;
            if (player.dimension)
            {
                portalTwoLocation.y -= dimensionOffset;
            }
            else
            {
                portalTwoLocation.y += dimensionOffset;
            }
            GameObject portalTwo = (GameObject)GameObject.Instantiate(portalPrefab, portalTwoLocation, portalOne.transform.rotation);

            // Set up both portal's portal scripts by linking them together.
            portalOne.GetComponent<Portal>().otherPortal = portalTwo;
            portalTwo.GetComponent<Portal>().otherPortal = portalOne;

            // If portal one has a teleport script, link those too.
            if (portalOne.GetComponent<Teleport>() != null)
            {
                portalOne.GetComponent<Teleport>().otherPortal = portalTwo.GetComponent<Teleport>();
                portalTwo.GetComponent<Teleport>().otherPortal = portalOne.GetComponent<Teleport>();
            }
            // Otherwise, if portal one has a sphere teleport script, link that.
            else if (portalOne.GetComponent<SphereTeleport>() != null)
            {
                portalOne.GetComponent<SphereTeleport>().otherPortal = portalTwo.GetComponent<SphereTeleport>();
                portalTwo.GetComponent<SphereTeleport>().otherPortal = portalOne.GetComponent<SphereTeleport>();
            }

            // Link both portal's waver scripts, so that they are the same size at all times.
            portalOne.GetComponent<Waver>().linked = true;
            portalOne.GetComponent<Waver>().linkedObject = portalTwo;
            portalTwo.GetComponent<Waver>().enabled = false;

            // Store both portals for future reference.
            openedPortals[0] = portalOne;
            openedPortals[1] = portalTwo;
        }
    }
}
