using UnityEngine;

public class OBJ_BULLET_Fireball : RendererEntity
{
    public AudioClip[] launchSounds;
    public AudioClip[] deathSounds;
    public AudioSource source;
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
    }
    public override void Kill()
    {
        var clip = Play(deathSounds);
        UIManager.Do(Recycle, clip.length);
    }
}
