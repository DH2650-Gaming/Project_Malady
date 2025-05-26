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
        // 获取移动输入
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");
        moveInput.Normalize();

        // 计算速度大小
        float speed = moveInput.magnitude;

        // 设置 Blend Tree 的参数
        animator.SetFloat("moveSpeed", speed);

        // 攻击测试
        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("isAttack");
        }
    }
}
