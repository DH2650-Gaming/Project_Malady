using UnityEngine;

public class animationTest : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private float switchInterval = 2f; // �л��������
    private int currentDirection = 0;

    private float timer = 0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        animator.SetBool("isMoving", true);
        timer += Time.deltaTime;

        if (timer >= switchInterval)
        {
            timer = 0f;

            // ѭ���л����� 0->1->2->3->0
            currentDirection = (currentDirection + 1) % 4;

            animator.SetFloat("Direction", currentDirection);
            Debug.Log($"Direction switched to {currentDirection}");
        }
    }
}

