using UnityEngine;
using System;
using System.Collections;
using System.IO;

public class SaveCamera : MonoBehaviour {
    public Camera cam;

	// Use this for initialization
	IEnumerator Start () {
        yield return new WaitForEndOfFrame();
        cam = GetComponent<Camera>();

        //yield return new WaitForSeconds(0.1f);
        Texture2D tex = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        RenderTexture rt = new RenderTexture(Screen.width, Screen.height, 24);
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;

        tex.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);

        cam.targetTexture = null;
        RenderTexture.active = null;
        DestroyImmediate(rt);

        byte[] data = tex.EncodeToJPG();
        string file = Environment.GetFolderPath(Environment.SpecialFolder.Desktop) + "\\screenshot.jpg";
        FileStream fstr = File.Create(file);
        fstr.Write(data, 0, data.Length);
        fstr.Close();
        Debug.Log("Camera saved to " + file);
	}
	
	// Update is called once per frame
	void Update () {
	
	}


}
