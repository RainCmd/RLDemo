using UnityEngine;

public class RE_Unit_Player : RendererEntity
{
    public Animator animator;
    public override void PlayAnim(string name, float time)
    {
        switch (name)
        {
            case "move":
                animator.SetBool("running", true);
                break;
            case "idle":
                animator.SetBool("running", false);
                break;
        }
    }
}
