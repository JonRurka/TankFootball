using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
public class RenderMaterialFullscreen : MonoBehaviour {
    [SerializeField]
    private Material material;

    [SerializeField, Range(10, 200)]
    private int width = 100;

    [SerializeField, Range(10, 200)]
    private int height = 100;

    void Awake() {
        int lowResRenderTarget = Shader.PropertyToID("_LowResRenderTarget");

        CommandBuffer cb = new CommandBuffer();

        cb.GetTemporaryRT(lowResRenderTarget, width, height, 0, FilterMode.Trilinear, RenderTextureFormat.ARGB32);

        cb.Blit(lowResRenderTarget, lowResRenderTarget, material);

        cb.Blit(lowResRenderTarget, BuiltinRenderTextureType.CameraTarget);

        cb.ReleaseTemporaryRT(lowResRenderTarget);

        GetComponent<Camera>().AddCommandBuffer(CameraEvent.BeforeForwardOpaque, cb);
    }

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
