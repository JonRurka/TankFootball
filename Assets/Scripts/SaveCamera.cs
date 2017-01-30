using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class SaveCamera : MonoBehaviour {
    public Camera cam_forground;
    public Camera cam_sky;

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	    if (Input.GetKeyDown(KeyCode.K)) {
            TakeScreenShot();
        }
	}

    public void TakeScreenShot() {
        string file = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\screenshot.png";
        Application.CaptureScreenshot(file);
        Debug.Log("Camera saved to " + file);
        return;
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);

        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        RenderTexture.active = rt;


        cam_sky.targetTexture = rt;
        cam_sky.Render();

        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

        cam_forground.targetTexture = rt;
        cam_forground.Render();

        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

        cam_forground.targetTexture = null;
        cam_sky.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        byte[] data = tex.EncodeToPNG();
        file = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\screenshot.png";
        FileStream fstr = File.Create(file);
        fstr.Write(data, 0, data.Length);
        fstr.Close();
        Debug.Log("Camera saved to " + file);
    }
}
