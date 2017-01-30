using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GlobalShaderVariables : MonoBehaviour {

    [SerializeField]
    private Texture2D noiseOffsetTexture;

    private Camera cam;

    private void Awake() {
        cam = GetComponent<Camera>();
        Shader.SetGlobalTexture("_NoiseOffsets", noiseOffsetTexture);
    }

    private void Update() {
        Shader.SetGlobalVector("_CamPos", transform.position);
        Shader.SetGlobalVector("_CamRight", transform.right);
        Shader.SetGlobalVector("_CamUp", transform.up);
        Shader.SetGlobalVector("_CamForward", transform.forward);
        Shader.SetGlobalFloat("_AspectRatio", (float)Screen.width / (float)Screen.height);
        Shader.SetGlobalFloat("_FieldOfView", Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * 2f);

    }

}
