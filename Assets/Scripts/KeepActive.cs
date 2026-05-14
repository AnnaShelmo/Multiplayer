using UnityEngine;
using System.Collections;

public class KeepActive : MonoBehaviour
{
    private void Start()
    {
        StartCoroutine(KeepActiveCoroutine());
    }

    private IEnumerator KeepActiveCoroutine()
    {
        yield return new WaitForEndOfFrame(); // ждём один кадр
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
        yield return new WaitForSeconds(0.2f);
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);
    }
}