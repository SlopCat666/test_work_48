using UnityEngine;

public class TriggerAttack : MonoBehaviour
{
    [SerializeField] private float damage = 25f;

    private void OnTriggerEnter(Collider other)
    {
        CreatureStats cs= other.gameObject.GetComponent<CreatureStats>();
        if (cs != null)
        {
            cs.Damage(damage);
        }
    }
}
