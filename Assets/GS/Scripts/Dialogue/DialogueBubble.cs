// DialogueBubble.cs
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DialogueBubble : MonoBehaviour
{
    [Header("气泡设置")]
    public GameObject bubblePrefab;
    public float displayDuration = 3f;
    public Vector3 offset = new Vector3(0, 3.5f, 0);  // 向上1.5个单位

    private GameObject currentBubble;
    private Coroutine currentCoroutine;
    private BubbleFollower follower;

    public void ShowDialogue(string text, float duration = -1)
    {
        if (duration < 0)
            duration = displayDuration;

        if (currentBubble != null)
        {
            Destroy(currentBubble);
            if (currentCoroutine != null)
                StopCoroutine(currentCoroutine);
        }

        // 实例化气泡（不设父对象）
        currentBubble = Instantiate(bubblePrefab);

        // 添加跟随脚本
        follower = currentBubble.AddComponent<BubbleFollower>();
        follower.target = transform;
        follower.offset = offset;

        // 初始位置
        currentBubble.transform.position = transform.position + offset;

        Text bubbleText = currentBubble.GetComponentInChildren<Text>();
        if (bubbleText != null)
        {
            bubbleText.text = text;
        }

        currentCoroutine = StartCoroutine(DestroyAfterDelay(duration));
    }

    private IEnumerator DestroyAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentBubble != null)
            Destroy(currentBubble);
        currentBubble = null;
        currentCoroutine = null;
    }
}
public class BubbleFollower : MonoBehaviour
{
    public Transform target;
    public Vector3 offset;

    void LateUpdate()
    {
        if (target != null)
        {
            transform.position = target.position + offset;
        }
    }
}