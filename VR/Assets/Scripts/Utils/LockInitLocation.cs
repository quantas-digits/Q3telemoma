using UnityEngine;

public class LockVirtualKeyboardPosition : MonoBehaviour
{
    public Vector3 lockedPosition;
    public Vector3 lockedRotation;

    void LateUpdate()
    {
        transform.position = lockedPosition;
        transform.rotation = Quaternion.Euler(lockedRotation);
    }
}
