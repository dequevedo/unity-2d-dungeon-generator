using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float moveSpeed = 5.0f;

    private void Update()
    {
        var horizontalInput = Input.GetAxis("Horizontal");
        var verticalInput = Input.GetAxis("Vertical");
        var moveDirection = new Vector3(horizontalInput, verticalInput, 0);
        transform.position += moveDirection * (Time.deltaTime * moveSpeed);
    }
}
