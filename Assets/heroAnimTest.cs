using UnityEngine;

public class PlayerAnimationController : MonoBehaviour
{
    [SerializeField]private Animator animator;
    private Vector2 moveInput;

    void Start()
    {
        //animator = GetComponent<Animator>();
    }

    void Update()
    {
        // ��ȡ�ƶ�����
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        // �����ٶȴ�С
        float speed = moveInput.magnitude;

        // ���� Blend Tree �Ĳ���
        animator.SetFloat("moveSpeed", speed);

        // ��������
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("isAttack");
        }
    }
}
