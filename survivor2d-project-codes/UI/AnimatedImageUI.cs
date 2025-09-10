using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AnimatedImageUI : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 12f;

    Image img;
    int i;

    void Awake()
    {
        img = GetComponent<Image>();
        if (img) img.enabled = false;   // hide until the first frame is set
    }

    void OnEnable()
    {
        StopAllCoroutines();
        if (frames != null && frames.Length > 0)
        {
            img.enabled = true;
            img.sprite = frames[0];     // show first frame instead of a white box
            StartCoroutine(CoPlay());
        }
    }

    IEnumerator CoPlay()
    {
        float dt = 1f / Mathf.Max(1f, fps);
        while (true)
        {
            if (img && frames.Length > 0)
                img.sprite = frames[i % frames.Length];
            i++;
            yield return new WaitForSeconds(dt);
        }
    }
}
