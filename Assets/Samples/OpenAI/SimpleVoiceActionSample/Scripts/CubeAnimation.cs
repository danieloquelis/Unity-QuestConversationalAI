using UnityEngine;

public class CubeAnimation : MonoBehaviour
{
    [SerializeField] private float degreesPerSecond = 90f;
    private bool _isSpinning;

    public void SetRotationSpeed(float dps)
    {
        degreesPerSecond = dps;
    }

    public void StartSpin()
    {
        _isSpinning = true;
    }

    public void StopSpin()
    {
        _isSpinning = false;
    }

    private void Update()
    {
        if (_isSpinning)
        {
            transform.Rotate(Vector3.up, degreesPerSecond * Time.deltaTime, Space.World);
        }
    }
}
