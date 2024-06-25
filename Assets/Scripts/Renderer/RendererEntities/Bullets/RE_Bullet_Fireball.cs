using System.Collections;
using UnityEngine;

public class RE_Bullet_Fireball : RendererEntity
{
    public AudioClip[] launchSounds;
    public AudioClip[] deathSounds;
    public SpriteRenderer sprite;
    public Sprite[] fly;
    public Sprite[] boom;
    public AudioSource source;
    private Sprite[] curAnim;
    private bool loopAnim;
    private int index;
    private AudioClip Play(params AudioClip[] clips)
    {
        var clip = clips[Random.Range(0, clips.Length)];
        source.clip = clip;
        source.Play();
        return clip;
    }
    protected override void OnInit()
    {
        Play(launchSounds);
        Play(fly, true);
    }
    private void Play(Sprite[] clips, bool loop)
    {
        curAnim = clips;
        loopAnim = loop;
        index = 0;
    }
    private void Update()
    {
        if (curAnim == null) return;
        var i = index / 3;
        if (i < curAnim.Length)
        {
            index++;
            sprite.sprite = curAnim[i];
        }
        else if (loopAnim) 
        {
            index = 0;
            sprite.sprite = curAnim[0];
        }
        else sprite.sprite = null;
    }
    public override void Kill()
    {
        Play(boom, false);
        var clip = Play(deathSounds);
        UIManager.Do(() => manager.Recycle(this), clip.length);
    }
}
