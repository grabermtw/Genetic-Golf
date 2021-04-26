using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class GeneticManager2 : MonoBehaviour
{
    [SerializeField]
    private GameObject agentPrefab;
    [SerializeField]
    private GameObject completionText;

    // References to menu input stuff
    [SerializeField]
    private GameObject menuUIParent;
    [SerializeField]
    private TMP_Dropdown fitnessDropdown;
    [SerializeField]
    private TMP_Dropdown gripDropdown;
    [SerializeField]
    private TMP_Dropdown jointsDropdown;
    [SerializeField]
    private TMP_InputField numAgentsInput;
    [SerializeField]
    private TMP_InputField holeDistInput;
    [SerializeField]
    private TMP_InputField holeDistRandInput;
    [SerializeField]
    private TMP_InputField timePerGenInput;
    [SerializeField]
    private TMP_InputField crossoverProbInput;
    [SerializeField]
    private TMP_InputField mutationProbInput;
    [SerializeField]
    private TMP_InputField numGensInput;
    [SerializeField]
    private TMP_InputField elitismInput;
    [SerializeField]
    private TextMeshProUGUI approxTime;

    [SerializeField]
    private GameObject simulationUI; // UI appearing in the top corner while running the simulation
    [SerializeField]
    private TextMeshProUGUI currentGenText; // displays the current generation number
    

    private GolferSettings.Fitness fitnessFunc;
    private GolferSettings.MoveableJointsExtent moveableJoints;
    private GolferSettings.ClubGrip clubGrip;
    private int numAgents; // number of agents (golfers) in each generation
    private float holeDist; // distance to the hole
    private float holeDistRand; // range of offset that should be applied to the hole distance each generation
    private float timePerGen; // amount of time dedicated to each generation before the golfers are cleared out and a new gen is created
    private float crossoverProb;
    private float mutationProb;
    private int numGens;
    private int numElites; // how many of the most fit individuals should be preserved.
                                    // please keep this to an even number

    private Chromosome2[] chroms;
    private GameObject[] agents;
    private float[] fitnesses;
    
    private Chromosome2 bestChrom; 
    private float bestFitness;
    private float [,] fitnessTrack;
    private GolferSettings settings;

    private const float INIT_TORQUE_MAG = 1000; // highest possible magnitude a torque component can have initially
    private const float DIST_BETWEEN_AGENTS = 5; // distance between active agents
    private const int COMPLEX_CHROMOSOME_LENGTH = 40; // length of each array in the Chromosome2 structure
    private const float MAX_TIME_BETWEEN_TORQUES = 1.5f; // maximum time between two torques
    private const int NUM_SWINGS_PER_GEN = 3; // how many swings each agent should try in each generation
    private const float PARALLEL_POSITION_OFFSET = 25; // how far off from the origin are these people swinging


    // Called when the user clicks the "Begin Simulation" button
    // Processes the input
    public void BeginComplexSimulation()
    {
        // Get numbers from the input fields
        try
        {
            numAgents = int.Parse(numAgentsInput.text);
            holeDist = float.Parse(holeDistInput.text);
            holeDistRand = float.Parse(holeDistRandInput.text);
            timePerGen = float.Parse(timePerGenInput.text);
            crossoverProb = float.Parse(crossoverProbInput.text);
            mutationProb = float.Parse(mutationProbInput.text);
            numGens = int.Parse(numGensInput.text);
            numElites = int.Parse(elitismInput.text);
            if (mutationProb < 0 || mutationProb > 1 || crossoverProb < 0 || crossoverProb > 1)
            {
                Debug.LogWarning("Crossover and mutation values should be between 0 and 1!");
                return;
            }
            if (numAgents < 0 || holeDist < 0 || holeDistRand < 0 || timePerGen < 0 || numGens < 0)
            {
                Debug.LogWarning("None of these numbers should be negative!");
                return;
            }
        }
        catch
        {
            Debug.LogWarning("Only numbers are allowed in the input fields!");
            return;
        }
        // Get the values from the dropdown options
        fitnessFunc = (GolferSettings.Fitness) fitnessDropdown.value;
        moveableJoints = (GolferSettings.MoveableJointsExtent) jointsDropdown.value;
        clubGrip = (GolferSettings.ClubGrip) gripDropdown.value;

        // Create the GolferSettings object
        settings = new GolferSettings(fitnessFunc, moveableJoints, clubGrip, holeDist);

        // hide the main menu UI
        menuUIParent.SetActive(false);
        // display the simulation UI
        simulationUI.SetActive(true);
        // Begin the simulation coroutine
        StartCoroutine(Simulate());
    }



    // --------------- ACTUAL GENETIC ALGORITHM STUFF BELOW ----------------


    /* @author Matthew Graber, Azhdaha Fayyaz, Andrew DeBiase, Vladislav Dozorov */
    // Here is where the actual simulations are managed
    private IEnumerator Simulate()
    {

        // ----------- CREATE INITIAL POPULATION, SET UP SIMULATION ------------

        float holeDistOffset;

        // Initialize arrays
        chroms = new Chromosome2[numAgents];
        agents = new GameObject[numAgents];
        fitnesses = new float[numAgents];
        fitnessTrack = new float[numGens, numAgents]; // 2D array with fitness for each gen

        // determine the number of joints based on whether we're using the golfer's full body or not
        int numJoints = (moveableJoints == GolferSettings.MoveableJointsExtent.fullBody ? 12 : 8);

        // initialize chromosomes with random values
        for (int i = 0; i < chroms.Length; i++)
        {
            // create Vector3 array of random torques to give to the chromosome
            Tuple<float, Vector3>[][] initJointMovements = new Tuple<float, Vector3>[numJoints][];
            for (int j = 0; j < initJointMovements.Length; j++)
            {
                initJointMovements[j] = new Tuple<float, Vector3>[COMPLEX_CHROMOSOME_LENGTH];
                for (int k = 0; k < COMPLEX_CHROMOSOME_LENGTH; k++)
                {
                    initJointMovements[j][k] = new Tuple<float, Vector3>(Random.Range(0f, MAX_TIME_BETWEEN_TORQUES),
                                                                         new Vector3(Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                                                                     Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                                                                     Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG)));
                }
            }
            chroms[i] = new Chromosome2(initJointMovements);
        }

        // ----------- RUN THE SIMULATION ------------

        for (int i = 0; i < numGens; i++)
        {
            // display the current generation number
            currentGenText.text = "" + (i + 1) + " / " + numGens;

            // Determine the random hole distance offset for this generation
            // (if holeDistRand == 0 then it will just be 0)
            holeDistOffset = Random.Range(-holeDistRand, holeDistRand);

            
            // have each agent do several swings and average the fitnesses of those swings
            // clear out fitnesses array
            fitnesses = new float[numAgents];
            for (int swingNum = 0; swingNum < NUM_SWINGS_PER_GEN; swingNum++)
            {
                // Instantiate new agents, give them their respective chromosomes, tell them to start swinging their clubs
                for (int j = 0; j < agents.Length; j++)
                {
                    agents[j] = Instantiate(agentPrefab, new Vector3(PARALLEL_POSITION_OFFSET, 0, - j * DIST_BETWEEN_AGENTS), Quaternion.identity);
                    agents[j].GetComponent<GolferBrain2>().InitializeAgent(chroms[j], settings, holeDistOffset);
                    agents[j].GetComponent<GolferBrain2>().BeginSwinging();
                }

                // Allow this generation to run for the specified time.
                // Execution of this code pauses here until time is up, then automatically resumes.
                yield return new WaitForSeconds(timePerGen);

                // Get the fitnesses of the agents and then clear them out
                for (int j = 0; j < agents.Length; j++)
                {
                    fitnesses[j] += agents[j].GetComponent<GolferBrain2>().GetFitness();
                    Destroy(agents[j]);
                }
            }
            // complete the calculation of the average fitnesses over those three swings for the gen
            for (int j = 0; j < fitnesses.Length; j++)
            {
                fitnesses[j] = fitnesses[j] / NUM_SWINGS_PER_GEN;
            }
            
            // Track fitness
            // Initialize best chrom & fit as the first one
            if (i == 0) {
                bestChrom = chroms[0];
                bestFitness = fitnesses[0];
            }

            // Add fitnesses to 2d fitnessTrack array
            // Assign new best chrom if there is one
            for (int j = 0; j < agents.Length; j++)
            {   
                fitnessTrack[i,j] = fitnesses[j]; 

                if (fitnessTrack[i,j] > bestFitness) { 
                    bestFitness = fitnessTrack[i,j];
                    bestChrom = chroms[j];
                }
            }

            // create new chromosome array
            Chromosome2[] newChroms = new Chromosome2[chroms.Length];

            /* find most fit individuals
                by sorting the fitnesses array in descending order,
                then taking the first however many biggest fitnesses
                and putting their respective chromosomes in the new chromosome array
                */
            float[] sortedFitnesses = fitnesses;
            Array.Sort(sortedFitnesses);
            Array.Reverse(sortedFitnesses);
            for (int j = 0; j < numElites; j++)
            {
                try {
                    newChroms[i] = chroms[Array.FindIndex(fitnesses, x => x == sortedFitnesses[j])];
                }
                catch {
                    Debug.LogWarning("something weird happened");
                }
            }
            
            // Andrew DeBiase
            // Crossover selection
            for (int pair = numElites; pair < chroms.Length; pair += 2)
            {
                //Tournament selection with 2 candidates for each parent
                // first parent:
                int cand1idx = Random.Range(0, numAgents);
                int cand2idx = Random.Range(0, numAgents);
                while(cand1idx == cand2idx){
                    cand2idx = Random.Range(0, numAgents);
                }
                int par1idx = (fitnesses[cand1idx] > fitnesses[cand2idx] ? cand1idx : cand2idx);
                // second parent
                cand1idx = Random.Range(0, numAgents);
                cand2idx = Random.Range(0, numAgents);
                while(cand1idx == cand2idx){
                    cand2idx = Random.Range(0, numAgents);
                }
                int par2idx = (fitnesses[cand1idx] > fitnesses[cand2idx] ? cand1idx : cand2idx);

                // determine if we crossover
                float crossValue = Random.Range(0.0f, 1.0f);
                if (crossValue < crossoverProb){
                    // crossover
                    Tuple<Chromosome2, Chromosome2> xresult = Crossover(chroms[par1idx], chroms[par2idx]);
                    newChroms[pair] = xresult.Item1;
                    newChroms[pair + 1] = xresult.Item2;
                }
                else
                {   // don't crossover, just send the parents on to the new array
                    newChroms[pair] = chroms[par1idx];
                    newChroms[pair + 1] = chroms[par2idx];
                }
            }

            //Vladislav Dozorov
            //Handle when mutation occurs
            for (int candmidx = numElites; candmidx < newChroms.Length; candmidx++)
            {
                float mutationValue = Random.Range(0.0f, 1.0f);
                if(mutationValue <= mutationProb) {
                    newChroms[candmidx] = Mutate(newChroms[candmidx]);
                }
            }
        }
        completionText.SetActive(true);
        // ExportCSV()
        GetComponent<Exporter>().FinishedSimulation();
        yield break;
    }

    /* @author Andrew DeBiase */
    private Tuple<Chromosome2, Chromosome2> Crossover(Chromosome2 parentOne, Chromosome2 parentTwo)
    {
        int jointsLength = parentOne.jointMovements.Length;
        int crossPoint = Random.Range(1,jointsLength);
        for(int i = crossPoint; i < jointsLength; i++){
            Tuple<float, Vector3>[] temp = parentOne.jointMovements[i];
            parentOne.jointMovements[i] = parentTwo.jointMovements[i];
            parentTwo.jointMovements[i] = temp;
        }

        return new Tuple<Chromosome2, Chromosome2>(parentOne, parentTwo);
    }


    /*
      Handles mutating a chromosome passed in by adding a randomly generated value
      to the torques of the chromosome
      @author Ernest Essuah Mensah
    */
    private Chromosome2 Mutate(Chromosome2 parent)
    {
        // Copy torques from parent to mutate
        Tuple<float, Vector3>[][] jointMovements = parent.jointMovements;

         int mutationIndex = Random.Range(0, parent.jointMovements.Length);
        
        for (int i = 0; i < COMPLEX_CHROMOSOME_LENGTH; i++)
        {
            Tuple<float, Vector3> current = parent.jointMovements[mutationIndex][i];

            // Mutate along the three coordinates
            float mutationX = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
            float mutationY = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
            float mutationZ = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
            
            Vector3 newMovement = new Vector3(current.Item2.x + mutationX, current.Item2.y + mutationY, current.Item2.z + mutationZ);

            // Mutate the time between joint movements
            float mutationTime = Random.Range(-MAX_TIME_BETWEEN_TORQUES, MAX_TIME_BETWEEN_TORQUES);

            float newTime = Mathf.Max(current.Item1 + mutationTime, 0);

            jointMovements[mutationIndex][i] = new Tuple<float, Vector3>(newTime, newMovement);
        }     
        
        return new Chromosome2(jointMovements);
    }

    public float[,] GetResults()
    {
        return fitnessTrack;
    }


    /* @author John Gansallo
        Deprecated, see ExportCSV() in Export.cs */
    public void ExportCSV()
    {
	    string filename = "numGens-" + numGens;
        filename += "_fitness-" + (fitnessFunc == GolferSettings.Fitness.accuracy ? "accuracy" : "distance");
        filename += "_jointsExtent-" + (moveableJoints == GolferSettings.MoveableJointsExtent.armsTorso ? "upperBody" : "fullBody");
        filename += "_grip-" + (clubGrip == GolferSettings.ClubGrip.oneHand ? "1hand" : "2hand");
        filename += "_numAgents-" + numAgents;
        if (fitnessFunc == GolferSettings.Fitness.accuracy)
        {
            filename += "_holeDist-" + holeDist;
            filename += "_holeRandOffset-" + holeDistRand;
        }
        filename += "_genTime-" + timePerGen;
        filename += "_pc-" + crossoverProb;
        filename += "_mc-" + mutationProb;
        filename += "_elitism-" + numElites;
        filename += "_chrom-2";
	    string path = Application.dataPath + @"/" + filename + ".csv";
        if (!File.Exists(path))
        {
            // Create a file to write to.
            using (StreamWriter gen = File.CreateText(path))
            {
                gen.WriteLine("Generation #,Best Fitness,Average Fitness");
		        for (int i = 0; i < fitnessTrack.GetLength(0); i++)
                {
                    float avgFit = 0;
                    float bestFit = Single.MinValue;
                    for (int j = 0; j < numAgents; j++)
                    {
                        Debug.Log(j + " " + numAgents);
                        avgFit += fitnessTrack[i,j];
                        if (fitnessTrack[i,j] > bestFit)
                            bestFit = fitnessTrack[i,j];
                    }
                    avgFit = avgFit / numAgents;
                    gen.WriteLine((i + 1).ToString() + "," + bestFit + "," + avgFit);
                }
            }	
        }

    }

}
