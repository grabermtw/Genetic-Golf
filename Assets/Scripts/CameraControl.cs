using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public GameObject[] cameras;
    private int activeCamIndex;

    public void SwitchCamera()
    {
        cameras[activeCamIndex].SetActive(false);
        activeCamIndex += 1;
        activeCamIndex = activeCamIndex % cameras.Length;
        cameras[activeCamIndex].SetActive(true);
    }
}
