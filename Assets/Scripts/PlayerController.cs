using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float _moveSpeed;
    [SerializeField] private Transform _playerTransform;
    private Rigidbody _playerRigidbody;

    private void Awake()
    {
        _playerRigidbody = GetComponent<Rigidbody>();
    }
    
    private void Update()
    {
        
    }
}
