using UnityEngine;

public class AnimationTrap : MonoBehaviour
{

    private Animator anim; // контроллер аниации
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        anim = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.GetComponent<CreatureStats>()!=null)
        {
            anim.SetTrigger("Attack");
        }
    }
}
