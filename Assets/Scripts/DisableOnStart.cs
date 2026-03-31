using UnityEngine;

public class DisableOnStart : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameObject.transform.root.gameObject.SetActive(false);
    }

}