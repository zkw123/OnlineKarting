using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goal : MonoBehaviour
{
    Transform target;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            target = other.transform.GetChild(0);
            Debug.Log("Goal");
            StartCoroutine(Rotate());

        }
    }

    public IEnumerator Rotate()
    {
        var center = target.Find("CenterTrans");
        Quaternion quaternion = Quaternion.AngleAxis(360 * Time.fixedDeltaTime, center.forward);
        float timer = 0;
        while (timer < 100)
        {
            target.localRotation = quaternion * target.localRotation;
            timer++;
            yield return new WaitForFixedUpdate();
        }
    }
}
