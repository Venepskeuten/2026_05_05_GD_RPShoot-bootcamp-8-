using UnityEngine;

public class ProjectileBehavior : MonoBehaviour
{
    // Stamp this when the projectile is created in Shoot()
    public PlayerBehavior.PlayerType shooterType;

    void OnCollisionEnter(Collision collision)
    {
        var hitPlayer = collision.gameObject.GetComponent<PlayerBehavior>();

        if (hitPlayer != null)
        {
            // Award point to whoever fired this projectile
            if (shooterType == PlayerBehavior.PlayerType.Player01)
            {
                GameMaster.Instance.AddPointToPlayer01();
            }
            else if (shooterType == PlayerBehavior.PlayerType.Player02)
            {
                GameMaster.Instance.AddPointToPlayer02();
            }

            GameMaster.Instance.EndOfRound();
        }

        // Destroy projectile on any hit — player OR wall OR floor
        Destroy(gameObject);
    }
}