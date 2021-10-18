using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class MirrorCamera : MonoBehaviour
{
    public Camera MainCamera;
    public GameObject target;
    private RenderTexture mirrorRT;
    private Camera mCamera;
    void OnEnable()
    {
        mCamera = GetComponent<Camera>();
        Init();
    }

    void Init()
    {
        mirrorRT = RenderTexture.GetTemporary(Screen.width,Screen.height);
        mirrorRT.useMipMap = false;
        mirrorRT.filterMode = FilterMode.Bilinear;
        Vector3 pos = Vector3.zero;
        Vector3 normal = Vector3.up;
        if (target != null)
        {
            pos = target.transform.position;
            normal = target.transform.up;
        }

        mCamera.targetTexture = mirrorRT;
    }

    private readonly int MirrorRTId = Shader.PropertyToID("_PlaneReflectTex");

    void OnPreRender()
    {
        Shader.SetGlobalTexture(MirrorRTId,mCamera.targetTexture);
    }

    void OnDisable()
    {        
        mCamera.targetTexture = null;
    }

    void LateUpdate()
    {
        var pos  = MainCamera.transform.position;
        var euler = MainCamera.transform.eulerAngles;
        transform.position = new Vector3(pos.x,-pos.y,pos.z);
        transform.eulerAngles = new Vector3(-euler.x,euler.y,euler.z);
    }

    // Given position/normal of the plane, calculates plane in camera space.
    private Vector4 CameraSpacePlane(Camera cam, Vector3 pos, Vector3 normal, float sideSign)
    {
        pos -= normal * 5f;
        var m = cam.worldToCameraMatrix;
        var cameraPosition = m.MultiplyPoint(pos);
        var cameraNormal = m.MultiplyVector(normal).normalized * sideSign;
        return new Vector4(cameraNormal.x, cameraNormal.y, cameraNormal.z, -Vector3.Dot(cameraPosition, cameraNormal));
    }
}
