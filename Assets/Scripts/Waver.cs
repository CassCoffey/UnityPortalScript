using UnityEngine;
using System.Collections;

/// <summary>
/// Class for opening, closing, and idle portal animations.
/// </summary>
public class Waver : MonoBehaviour {

    // The portal this portal is linked with.
    public bool linked;
    public GameObject linkedObject;

    public float minDeltaX;
    public float minDeltaY;
    public float minDeltaZ;
    public float maxDeltaX;
    public float maxDeltaY;
    public float maxDeltaZ;

    public float xMaxTime;
    public float yMaxTime;
    public float zMaxTime;
    public float xMinTime;
    public float yMinTime;
    public float zMinTime;

    private Vector3 originalScale;

    private Vector3 currentScale;
    private Vector3 nextScale;

    private float nextXTime;
    private float nextYTime;
    private float nextZTime;

    private float curXTime = 0;
    private float curYTime = 0;
    private float curZTime = 0;

    private float nextX;
    private float nextY;
    private float nextZ;

    private float curX;
    private float curY;
    private float curZ;

    private float prevX;
    private float prevY;
    private float prevZ;

    private float xPercent = 0;
    private float yPercent = 0;
    private float zPercent = 0;

    private bool closing = false;

	// Use this for initialization
	void Start () {
        // Open the portal.
        originalScale = transform.localScale;
        transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
        curX = transform.localScale.x;
        curY = transform.localScale.y;
        curZ = transform.localScale.z;

        prevX = transform.localScale.x;
        prevY = transform.localScale.y;
        prevZ = transform.localScale.z;

        nextX = Random.Range(originalScale.x + minDeltaX, originalScale.x + maxDeltaX);
        nextY = Random.Range(originalScale.y + minDeltaY, originalScale.y + maxDeltaY);
        nextZ = Random.Range(originalScale.z + minDeltaZ, originalScale.z + maxDeltaZ);

        nextXTime = Random.Range(xMinTime/2, xMaxTime/2);
        nextYTime = Random.Range(yMinTime/2, yMaxTime/2);
        nextZTime = Random.Range(zMinTime/2, zMaxTime/2);
	}
	
	// Update is called once per frame
	void Update () {
        // Make the portal expand/contract to the new size.
        curXTime += Time.deltaTime;
        curYTime += Time.deltaTime;
        curZTime += Time.deltaTime;

        xPercent = curXTime / nextXTime;
        yPercent = curYTime / nextYTime;
        zPercent = curZTime / nextZTime;

        curX = Mathf.Lerp(prevX, nextX, xPercent);
        curY = Mathf.Lerp(prevY, nextY, yPercent);
        curZ = Mathf.Lerp(prevZ, nextZ, zPercent);
        Vector3 newScale = new Vector3(curX, curY, curZ);

        transform.localScale = newScale;
        if (linked)
        {
            linkedObject.transform.localScale = newScale;
        }

        if (xPercent >= 1f && !closing)
        {
            xPercent = 0;
            curXTime = 0;
            nextXTime = Random.Range(xMinTime, xMaxTime);
            prevX = curX;
            nextX = Random.Range(originalScale.x + minDeltaX, originalScale.x + maxDeltaX);
        }
        if (yPercent >= 1f && !closing)
        {
            yPercent = 0;
            curYTime = 0;
            nextYTime = Random.Range(yMinTime, yMaxTime);
            prevY = curY;
            nextY = Random.Range(originalScale.y + minDeltaY, originalScale.y + maxDeltaY);
        }
        if (zPercent >= 1f && !closing)
        {
            zPercent = 0;
            curZTime = 0;
            nextZTime = Random.Range(zMinTime, zMaxTime);
            prevZ = curZ;
            nextZ = Random.Range(originalScale.z + minDeltaZ, originalScale.z + maxDeltaZ);
        }
        if (xPercent >= 1f && yPercent >= 1f && zPercent >= 1f && closing)
        {
            DestroyImmediate(gameObject);
            if (linked)
            {
                DestroyImmediate(linkedObject);
            }
        }
	}

    public void Close()
    {
        // The portal will contract to be very small and then destroy itself.
        nextX = 0.1f;
        nextY = 0.1f;
        nextZ = 0.1f;

        prevX = curX;
        prevY = curY;
        prevZ = curZ;

        curXTime = 0;
        curYTime = 0;
        curZTime = 0;

        nextXTime = Random.Range(xMinTime / 3, xMaxTime / 3);
        nextYTime = Random.Range(yMinTime / 3, yMaxTime / 3);
        nextZTime = Random.Range(zMinTime / 3, zMaxTime / 3);

        closing = true;
    }

    /// <summary>
    /// If the portal is inside the ground, it will be pushed out.
    /// </summary>
    void OnTriggerStay (Collider collider)
    {
        if (collider.tag == "Ground")
        {
            Vector3 distanceNormal = transform.position - collider.ClosestPointOnBounds(transform.position);
            distanceNormal.Normalize();
            transform.position += distanceNormal * 0.02f;
        }
    }
}
