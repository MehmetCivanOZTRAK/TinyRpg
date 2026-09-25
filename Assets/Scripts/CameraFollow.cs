using System;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 offset;
    [SerializeField] private float followspeed;

    private void LateUpdate()
    {
        transform.position = Vector3.Lerp(transform.position, target.position + offset, followspeed * Time.deltaTime);
    }
}
