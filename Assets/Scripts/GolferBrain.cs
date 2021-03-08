﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GolferBrain : MonoBehaviour
{
    [SerializeField]
    private Rigidbody[] joints = null; // Think of these as muscles
    [SerializeField]
    private GameObject[] golfClubs = null;
    [SerializeField]
    private Rigidbody golfBall = null;
    [SerializeField]
    private Transform hole = null;
    [SerializeField]
    private bool useGravity;

    private Chromosome chrom;
    private bool swinging;
    

    // Start is called before the first frame update
    void Start()
    {
        // Code for testing muscles, remove later
        Vector3[] testTorques = new Vector3[joints.Length];
        for (int i = 0; i < testTorques.Length; i++)
        {
            testTorques[i] = new Vector3(Random.Range(0,1000),Random.Range(0,1000),Random.Range(0,1000));
        }
        SetChromosome(new Chromosome(testTorques, Chromosome.Fitness.drivingDist));
        BeginSwinging();
    }

    // This should be called right after the golfer is instantiated
    public void SetChromosome(Chromosome newChrom)
    {
        chrom = newChrom;
        // hide the golf hole if we don't need it
        if (chrom.fitnessFunc == Chromosome.Fitness.drivingDist)
            hole.gameObject.SetActive(false);
    }

    // Returns the number of joints in use
    public int GetNumberOfJoints()
    {
        return joints.Length;
    }

    // Begin the golfing simulation with this agent by allowing it to swing the club
    public void BeginSwinging()
    {
        /*  Prepare joints for movement.
            By default, all joints will be kinematic (meaning physics does not act upon them).
            Here, we set all joints that are added to the joints array to not be kinematic,
            allowing them to move. */
        foreach(Rigidbody joint in joints)
        {
            joint.isKinematic = false;
            joint.useGravity = useGravity;
        }
        StartCoroutine(MoveJoints());
    }

    // Call this to interrupt the MoveJoints coroutine
    public void StopSwinging()
    {
        swinging = false;
    }

    // This coroutine adds torque to each joint.
    /*  could change this later if we have a more complex chromosome
        with multiple torques per joint */
    private IEnumerator MoveJoints()
    {
        swinging = true;
        while (swinging)
        {
            for (int i = 0; i < joints.Length; i++)
            {
                // multiplying by Time.fixedDeltaTime keeps it framerate-independent
                /*  consider multiplying by the distance to the hole to
                    explore if that makes the agent better able to hit holes at different
                    distances */
                joints[i].AddRelativeTorque(chrom.torques[i] * Time.fixedDeltaTime);
            }
            yield return new WaitForFixedUpdate();
        }
    }

    /*  Calculates and returns the fitness of this agent
        based on what our chromosome says our fitness function should be */
    public float GetFitness()
    {
        float fitness = 0;
        switch(chrom.fitnessFunc)
        {
            case Chromosome.Fitness.drivingDist:
                // Calculate fitness based on total distance ball has traveled in agent's direction
                float ballDist = Vector3.Distance(golfBall.position, transform.position);
                Vector3 ballDir = golfBall.position - transform.position;
                float ballAngle = Vector3.Angle(ballDir, -transform.right);
                fitness = ballDist * Mathf.Cos(ballAngle);
                break;
            
            case Chromosome.Fitness.accuracy:
                // Calculate fitness based on accuracy of hitting it toward the hole
                fitness = Vector3.Distance(golfBall.position, hole.position);
            break;
            default:
                Debug.LogWarning("Unrecognized fitness function");
                break;
        }
        return fitness;
    }

}
