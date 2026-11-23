using UnityEngine;

public class ObjectToggle : MonoBehaviour
{
    public GameObject objectToToggle;

    public void ToggleObject()
    {
        objectToToggle.SetActive(!objectToToggle.activeSelf);
    }
}