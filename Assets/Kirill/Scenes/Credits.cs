using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Kirill
{
    public class Credits : MonoBehaviour
    {
        [SerializeField] private float speed;
        [SerializeField] private float stopCord;
        [SerializeField] private float stopSpeed;
        [SerializeField] private float exitDelay;
        private bool exit;
        private void Update()
        {
            transform.Translate(0, speed * Time.deltaTime, 0);
            if (transform.localPosition.y > stopCord)
            {
                speed -= stopSpeed * Time.deltaTime;
                if (speed < 0) { speed = 0; stopSpeed = 0; }
                if (!exit) { Invoke("Exit", exitDelay); exit = true; } 
            }
        }
        void Exit()
        {
            SceneManager.LoadScene(0);
        }
    }
}

