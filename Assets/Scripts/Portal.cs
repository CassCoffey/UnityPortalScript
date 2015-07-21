using UnityEngine;
using System.Collections;

/// <summary>
/// A heavily modified version of the Unity Pro water reflection shader, based off the modified version at: http://wiki.unity3d.com/index.php/MirrorReflection4
/// </summary>

[ExecuteInEditMode] // Make portal live-update even when not in play mode
public class Portal : MonoBehaviour {

    // Optimization fields
    public bool m_DisablePixelLights = true;
    public int m_TextureSize = 1080;

    // Used for spherical portals, to set the offset for their clipping plane.
    public float m_ClipRadius = 2f;

    // If this is true, the portal will be treated as perfectly flat.
    public bool plane = true;

    // If true, the portal will act like a mirror and render from the opposite side of the other portal
    public bool mirror = false;

    // The portal will not render these layers
    public LayerMask m_PortalLayers = -1;

    // The portal that this portal is visually linked with.
    public GameObject otherPortal;

    // A hashtable to keep track of all the portal cameras and their corresponding cameras
    private Hashtable m_PortalCameras = new Hashtable();

    public Hashtable PortalCameras
    {
        get
        {
            return m_PortalCameras;
        }
    }

    // The texture that this will render to.
    private RenderTexture m_PortalTexture = null;
    private int m_OldPortalTextureSize = 0;

    // Safequard boolean against recursive rendering.
    private static bool s_InsideRendering = false;

    // On start, duplicate all materials so that you can render 2 portals at once.
    void Start()
    {
        for (int i = 0; i < GetComponent<Renderer>().materials.Length; i++)
        {
            GetComponent<Renderer>().materials[i] = new Material(GetComponent<Renderer>().materials[i]);
        }
    }

    // This is called when it's known that the object will be rendered by some
    // camera. We render portals and do other updates here.
    // Because the script executes in edit mode, reflections for the scene view
    // camera will just work!
    public void OnWillRenderObject()
    {
        var rend = GetComponent<Renderer>();
        if (!enabled || !rend || !rend.sharedMaterial || !rend.enabled || !otherPortal)
            return;

        Camera cam = Camera.current;
        if (!cam)
            return;

        // Safeguard from recursive portals.        
        if (s_InsideRendering)
            return;
        s_InsideRendering = true;

        Camera portalCamera;
        CreatePortalObjects(cam, out portalCamera);

        Vector3 pos = otherPortal.transform.position;
        // Figure out the relative position of the camera to the other portal.
        Vector3 localPos = transform.InverseTransformPoint(cam.transform.position);
        if (mirror)
        {
            localPos = -localPos;
        }
        Vector3 oldpos = otherPortal.transform.TransformPoint(localPos);
        // Calculate the clip plane, so that only things in view are drawn.
        Vector3 normal;
        Vector3 clipPos;
        if (plane)
        {
            normal = -transform.forward;
            if (localPos.z < 0)
            {
                normal = transform.forward;
            }
            clipPos = pos;
        }
        else
        {
            normal = cam.transform.forward;
            if (mirror)
            {
                normal = -cam.transform.forward;
            }
            clipPos = pos - (normal * m_ClipRadius);
            if (Vector3.Distance(pos, clipPos) + 0.7f >= Vector3.Distance(pos, oldpos))
            {
                clipPos = oldpos + (normal * 0.7f);
            }
        }

        // Optionally disable pixel lights for portal rendering
        int oldPixelLightCount = QualitySettings.pixelLightCount;
        if (m_DisablePixelLights)
            QualitySettings.pixelLightCount = 0;

        UpdateCameraModes(cam, portalCamera);

        // Create a transform matrix that moves the camera to the other portal.
        if (mirror)
        {
            // Render reflection
            // Reflect camera around reflection plane
            float d = -Vector3.Dot(normal, pos);
            Vector4 reflectionPlane = new Vector4(normal.x, normal.y, normal.z, d);
            Matrix4x4 reflection = Matrix4x4.zero;
            CalculateReflectionMatrix(ref reflection, reflectionPlane);
            localPos = -localPos;
            portalCamera.worldToCameraMatrix = cam.worldToCameraMatrix * Matrix4x4.TRS(pos - transform.position, Quaternion.identity, Vector3.one).inverse * reflection;
        }
        else
        {
            portalCamera.worldToCameraMatrix = cam.worldToCameraMatrix * Matrix4x4.TRS(pos - transform.position, Quaternion.identity, Vector3.one).inverse;
        }
        
        Vector4 clipPlane = CameraSpacePlane(portalCamera, clipPos, normal, 1.0f);
        Matrix4x4 projection = cam.CalculateObliqueMatrix(clipPlane);
        portalCamera.projectionMatrix = projection;

        // Render the texture and set it as the material's texture.
        portalCamera.cullingMask = ~(1 << 4) & m_PortalLayers.value; // never render water layer
        portalCamera.targetTexture = m_PortalTexture;
        portalCamera.transform.position = oldpos;
        if (mirror)
        {
            GL.invertCulling = true;
        }
        Vector3 euler = cam.transform.eulerAngles;
        portalCamera.transform.eulerAngles = new Vector3(0, euler.y, euler.z);
        portalCamera.Render();
        portalCamera.transform.position = oldpos;
        if (mirror)
        {
            GL.invertCulling = false;
        }
        Material[] materials = rend.sharedMaterials;
        foreach (Material mat in materials)
        {
            if (mat.HasProperty("_PortalTex"))
                mat.SetTexture("_PortalTex", m_PortalTexture);
        }

        // Restore pixel light count
        if (m_DisablePixelLights)
            QualitySettings.pixelLightCount = oldPixelLightCount;

        s_InsideRendering = false;
    }


    // Cleanup all the objects we possibly have created
    void OnDisable()
    {
        if (m_PortalTexture)
        {
            DestroyImmediate(m_PortalTexture);
            m_PortalTexture = null;
        }
        foreach (DictionaryEntry kvp in m_PortalCameras)
            DestroyImmediate(((Camera)kvp.Value).gameObject);
        m_PortalCameras.Clear();
    }


    private void UpdateCameraModes(Camera src, Camera dest)
    {
        if (dest == null)
            return;
        // set camera to clear the same way as current camera
        dest.clearFlags = src.clearFlags;
        dest.backgroundColor = src.backgroundColor;
        if (src.clearFlags == CameraClearFlags.Skybox)
        {
            Skybox sky = src.GetComponent(typeof(Skybox)) as Skybox;
            Skybox mysky = dest.GetComponent(typeof(Skybox)) as Skybox;
            if (!sky || !sky.material)
            {
                mysky.enabled = false;
            }
            else
            {
                mysky.enabled = true;
                mysky.material = sky.material;
            }
        }
        // update other values to match current camera.
        // even if we are supplying custom camera&projection matrices,
        // some of values are used elsewhere (e.g. skybox uses far plane)
        dest.farClipPlane = src.farClipPlane;
        dest.nearClipPlane = src.nearClipPlane;
        dest.orthographic = src.orthographic;
        dest.fieldOfView = src.fieldOfView;
        dest.aspect = src.aspect;
        dest.orthographicSize = src.orthographicSize;
    }

    // On-demand create any objects we need
    private void CreatePortalObjects(Camera currentCamera, out Camera portalCamera)
    {
        portalCamera = null;

        // Portal render texture
        if (!m_PortalTexture || m_OldPortalTextureSize != m_TextureSize)
        {
            if (m_PortalTexture)
                DestroyImmediate(m_PortalTexture);
            m_PortalTexture = new RenderTexture(m_TextureSize, m_TextureSize, 16);
            m_PortalTexture.name = "__PortalView" + GetInstanceID();
            m_PortalTexture.isPowerOfTwo = true;
            m_PortalTexture.hideFlags = HideFlags.DontSave;
            m_OldPortalTextureSize = m_TextureSize;
        }

        // Camera for reflection
        portalCamera = m_PortalCameras[currentCamera] as Camera;
        if (!portalCamera) // catch both not-in-dictionary and in-dictionary-but-deleted-GO
        {
            GameObject go = new GameObject("Portal Camera id" + GetInstanceID() + " for " + currentCamera.GetInstanceID(), typeof(Camera), typeof(Skybox));
            portalCamera = go.GetComponent<Camera>();
            portalCamera.enabled = false;
            portalCamera.transform.position = otherPortal.transform.position;
            portalCamera.transform.rotation = otherPortal.transform.rotation;
            portalCamera.gameObject.AddComponent<FlareLayer>();
            go.hideFlags = HideFlags.HideAndDontSave;
            m_PortalCameras[currentCamera] = portalCamera;
        }
    }

    // Extended sign: returns -1, 0 or 1 based on sign of a
    private static float sgn(float a)
    {
        if (a > 0.0f) return 1.0f;
        if (a < 0.0f) return -1.0f;
        return 0.0f;
    }

    // Given position/normal of the plane, calculates plane in camera space.
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        Vector3 offsetPos = pos;
        Matrix4x4 m = cam.worldToCameraMatrix;
        Vector3 cpos = m.MultiplyPoint(offsetPos);
        Vector3 cnormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cnormal.x, cnormal.y, cnormal.z, -Vector3.Dot(cpos, cnormal));
    }

    // Calculates reflection matrix around the given plane
    private static void CalculateReflectionMatrix(ref Matrix4x4 reflectionMat, Vector4 plane)
    {
        reflectionMat.m00 = (1F - 2F * plane[0] * plane[0]);
        reflectionMat.m01 = (-2F * plane[0] * plane[1]);
        reflectionMat.m02 = (-2F * plane[0] * plane[2]);
        reflectionMat.m03 = (-2F * plane[3] * plane[0]);

        reflectionMat.m10 = (-2F * plane[1] * plane[0]);
        reflectionMat.m11 = (1F - 2F * plane[1] * plane[1]);
        reflectionMat.m12 = (-2F * plane[1] * plane[2]);
        reflectionMat.m13 = (-2F * plane[3] * plane[1]);

        reflectionMat.m20 = (-2F * plane[2] * plane[0]);
        reflectionMat.m21 = (-2F * plane[2] * plane[1]);
        reflectionMat.m22 = (1F - 2F * plane[2] * plane[2]);
        reflectionMat.m23 = (-2F * plane[3] * plane[2]);

        reflectionMat.m30 = 0F;
        reflectionMat.m31 = 0F;
        reflectionMat.m32 = 0F;
        reflectionMat.m33 = 1F;
    }
}
