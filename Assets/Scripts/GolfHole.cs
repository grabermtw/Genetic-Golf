/*
    This script is attached to all the golf holes that get instantiated.
    Essentially, if a ball enters the golf hole's collider (set to be a trigger),
    we treat that as the ball going in the hole, and so this script takes the ball
    and puts it at its center so that the golfer observes that they succeeded.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolfHole : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        // only do this for golf balls
        if (other.gameObject.CompareTag("GolfBall"))
        {
            // freeze the golf ball in the center of the hole.
            Rigidbody ballRb = other.GetComponent<Rigidbody>();
            ballRb.velocity = Vector3.zero;
            ballRb.position = transform.position;
            ballRb.isKinematic = true;
        }
    }
}
