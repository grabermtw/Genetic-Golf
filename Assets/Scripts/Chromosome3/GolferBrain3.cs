/* 
    This script is attached to each agent that gets instantiated.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolferBrain3 : MonoBehaviour
{
    [SerializeField]
    private Rigidbody[] joints; // Think of these as muscles
    [SerializeField]
    private FixedJoint secondHandJoint; // joint for the second hand which will be destroyed if going one-handed
    [SerializeField]
    private GameObject[] golfClubs;
    [SerializeField]
    private Rigidbody golfBall;
    [SerializeField]
    private Transform hole;
    [SerializeField]
    private Transform tee;
    [SerializeField]
    private bool useGravity;

    private Chromosome3 chrom;
    private GolferSettings settings;
    private Rigidbody[] jointsInUse; // the joints that are actually being used in this sim (may or may not be full body)
    private float actualHoleDist = 1;
    

    // This should be called right after the golfer is instantiated
    public void InitializeAgent(Chromosome3 newChrom, GolferSettings newSettings, float holeDistOffset = 0)
    {
        chrom = newChrom;
        jointsInUse = new Rigidbody[chrom.jointMovements.Length];
        settings = newSettings;
        // hide the golf hole if we don't need it
        if (settings.fitnessFunc == GolferSettings.Fitness.drivingDist)
        {
            hole.gameObject.SetActive(false);
        }
        else 
        {   // position the hole based on the distance it's supposed to be from the golfer, maintaining the y-coordinate
            hole.position = transform.position - Vector3.right * (settings.holeDist + holeDistOffset) + new Vector3(0, hole.position.y, 0);
            actualHoleDist = Vector3.Distance(transform.position, hole.position);
        }
        
        // Destroy the joint connecting the club to the second hand if we're going one-handed
        if (settings.clubGrip == GolferSettings.ClubGrip.oneHand)
            Destroy(secondHandJoint);
    }

    // Begin the golfing simulation with this agent by allowing it to swing the club
    public void BeginSwinging()
    {
        /*  Prepare joints for movement.
            By default, all joints will be kinematic (meaning physics does not act upon them).
            Here, we set all joints that are added to the joints array to not be kinematic,
            allowing them to move. */
        int jointIndex = 0;
        foreach(Rigidbody joint in joints)
        {
            // unlock every joint if we're doing the full body, and if we're just doing torso and arms,
            // don't unlock the legs
            if (settings.moveableJoints == GolferSettings.MoveableJointsExtent.fullBody ||
                (!joint.gameObject.name.Contains("Leg") && !joint.gameObject.name.Contains("Hips")))
            {
                joint.isKinematic = false;
                joint.useGravity = useGravity;
                if (!joint.gameObject.name.Contains("Hips"))
                    jointsInUse[jointIndex++] = joint;
            }
        }

        // start the joint-moving coroutine for each joint
        for (int i = 0; i < chrom.jointMovements.Length; i++)
        {
            StartCoroutine(MoveJoint(i));
        }
        
    }

    // This coroutine adds the torques to the joint, separated by the specified times.
    private IEnumerator MoveJoint(int jointIndex)
    {
        /*  consider multiplying by the distance to the hole to
            explore if that makes the agent better able to hit holes at different
            distances */
        // consideration of distance to hole (see if this helps when hole position is randomized)
        for (int i = 0; i < chrom.jointMovements[jointIndex].Length; i++)
        {
            // wait for the specified time
            yield return new WaitForSeconds(chrom.jointMovements[jointIndex][i].Item1);
            // apply the torque for the duration specified by the chromosome
            float timeRemaining = chrom.jointMovements[jointIndex][i].Item3;
            while (timeRemaining > 0)
            {
                // wait for physics update
                yield return new WaitForFixedUpdate();
                jointsInUse[jointIndex].AddRelativeTorque(chrom.jointMovements[jointIndex][i].Item2 * actualHoleDist * Time.fixedDeltaTime);
                timeRemaining -= Time.fixedDeltaTime;
            }
        }
    }

    /*  Calculates and returns the fitness of this agent
        based on what our chromosome says our fitness function should be */
    public float GetFitness()
    {
        float fitness = 0;
        switch(settings.fitnessFunc)
        {
            case GolferSettings.Fitness.drivingDist:
                // Calculate fitness based on total distance ball has traveled in agent's direction
                float ballDist = Vector3.Distance(golfBall.position, tee.position);
                Vector3 ballDir = golfBall.position - tee.position;
                float ballAngle = Vector3.Angle(ballDir, -tee.right);
                fitness = ballDist * Mathf.Cos(ballAngle);
                Debug.Log("position: " + golfBall.position + "fitness: " + fitness);
                break;
            case GolferSettings.Fitness.accuracy:
                // Calculate fitness based on accuracy of hitting it toward the hole
                // make this negative so that more fit agents will always have higher fitness values
                fitness = - Vector3.Distance(golfBall.position, hole.position);
                break;
            default:
                Debug.LogWarning("Unrecognized fitness function");
                break;
        }
        return fitness;
    }

}
