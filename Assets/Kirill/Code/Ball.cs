using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dany;
using SiberianGJ26.YouAreDoing.Antos.Modules;

public class Ball : MonoBehaviour
{
    public float damage;
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent(out FirstPersonController fps))
        {
            fps.GetComponent<MonoHealth>().TrySet(-damage);
        }
        Destroy(gameObject);
    }
}
