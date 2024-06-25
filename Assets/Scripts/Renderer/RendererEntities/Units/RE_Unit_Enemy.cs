using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RE_Unit_Enemy : RendererEntity
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
    public override void Kill()
    {
        animator.SetBool("dead", true);
        UIManager.Do(() => manager.Recycle(this), 3);
    }
}
