using UnityEngine;

/*  ========================================
                PROJECTILE BEHAVIOR
    ========================================
    This script lives on the projectile prefab.
    It has one job: detect what the projectile hits, award a point to the correct
    player if a player was hit, trigger the end of the round, and then destroy itself.

    The shooterType field is stamped onto this script by PlayerBehavior.Shoot()
    immediately after the projectile is instantiated. This is the link that tells
    GameMaster which player should receive the point.

    Flow:
        PlayerBehavior.Shoot()
            → Instantiate projectile
            → projBehavior.shooterType = this.PlayerSelect   ← identity stamp
        ProjectileBehavior.OnCollisionEnter()
            → hit a player? → GameMaster.AddPointToPlayer01/02 → GameMaster.EndOfRound()
            → hit anything? → Destroy(gameObject)
    ========================================    */

public class ProjectileBehavior : MonoBehaviour
{
    // Set by PlayerBehavior.Shoot() after instantiation.
    // Identifies who fired this projectile so the correct player receives the point on a hit.
    // Linked to GameMaster.AddPointToPlayer01() and AddPointToPlayer02().
    public PlayerBehavior.PlayerType shooterType;

    // Called by Unity when this object's collider makes contact with another collider.
    // Handles both player hits (point + round end) and surface hits (destroy only).
    void OnCollisionEnter(Collision collision)
    {
        // 1. Check if the object hit is a player by looking for a PlayerBehavior component.
        //    Non-player objects (walls, floor) will return null here.
        var hitPlayer = collision.gameObject.GetComponent<PlayerBehavior>();

        if (hitPlayer != null)
        {
            // 2. Award the point to whoever fired this projectile.
            //    shooterType was stamped onto this script in PlayerBehavior.Shoot().
            //    GameMaster.AddPointToPlayer01/02 increments the relevant score counter.
            if      (shooterType == PlayerBehavior.PlayerType.Player01) GameMaster.Instance.AddPointToPlayer01();
            else if (shooterType == PlayerBehavior.PlayerType.Player02) GameMaster.Instance.AddPointToPlayer02();

            // 3. End the round. GameMaster.EndOfRound() handles cleanup, UI reset,
            //    and checks whether the match is over (AreWeDoneYet).
            GameMaster.Instance.EndOfRound();
        }

        // 4. Destroy this projectile on any collision — player hit, wall, or floor.
        //    This runs regardless of whether step 2-3 executed.
        //    Any remaining projectiles in the scene are also caught by GameMaster.Cleanup()
        //    via FindGameObjectsWithTag("Projectile") at round end.
        Destroy(gameObject);
    }
}