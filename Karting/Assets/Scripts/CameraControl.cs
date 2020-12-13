using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
public class CameraControl : NetworkBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if (!isLocalPlayer)
        {
            Transform father = this.transform;
            for (int i = 0; i < father.childCount; i++)
            {
                string child = father.GetChild(i).tag;
                if (child == "MainCamera")
                {
                    father.GetChild(i).gameObject.SetActive(false);
                }
            }
        }
    }
}
