using UnityEngine;

public class Finish : MonoBehaviour
{
    
    private void OnTriggerEnter(Collider other)
    {
        // коснулся игрок - значит победа
        if (other.gameObject.GetComponent<PlayerController>() != null)
        {
            GameManager.Instance.LevelComplete();
        }
    }
}
