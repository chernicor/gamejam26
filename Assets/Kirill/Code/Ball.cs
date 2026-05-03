using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Dany;
using SiberianGJ26.YouAreDoing.Antos.Modules;

public class Ball : MonoBehaviour
{
    public float damage;
    [SerializeField] GameObject splash;
    private void Start()
    {
        Destroy(gameObject, 10);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if(collision.gameObject.TryGetComponent(out FirstPersonController fps))
        {
            fps.GetComponent<MonoHealth>().TrySet(-damage);
        }
        GameObject _splash = Instantiate(splash, transform.parent);
        _splash.transform.position = transform.position;
        Destroy(_splash, 2.0f);
        GetComponent<Rigidbody>().isKinematic = true;
        GetComponent<Collider>().enabled = false;
        GetComponent<MeshRenderer>().enabled = false;
        Destroy(gameObject, 3);
    }
}
