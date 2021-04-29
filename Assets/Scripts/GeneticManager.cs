using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using TMPro;
using Random = UnityEngine.Random;

public class GeneticManager : MonoBehaviour
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
    

    public GolferSettings.Fitness fitnessFunc;
    public GolferSettings.MoveableJointsExtent moveableJoints;
    public GolferSettings.ClubGrip clubGrip;
    public int numAgents; // number of agents (golfers) in each generation
    public float holeDist; // distance to the hole
    public float holeDistRand; // range of offset that should be applied to the hole distance each generation
    public float timePerGen; // amount of time dedicated to each generation before the golfers are cleared out and a new gen is created
    public float crossoverProb;
    public float mutationProb;
    public int numGens;
    public int numElites; // how many of the most fit individuals should be preserved.
                                    // please keep this to an even number
    private Chromosome[] chroms;
    private GameObject[] agents;
    private float[] fitnesses;
    
    private Chromosome bestChrom; 
    private float bestFitness;
    private float [,] fitnessTrack;
    private GolferSettings settings;

    private const float INIT_TORQUE_MAG = 1000; // highest possible magnitude a torque component can have initially
    private const float DIST_BETWEEN_AGENTS = 5; // distance between active agents
    private const int NUM_SWINGS_PER_GEN = 3; // how many swings each agent should try in each generation

    // Called before the first frame update
    void Start()
    {
        // give a predicted time based on the initial values
        CalculateApproximateTime();
    }

    // Calculate approximate expected time to run the simulation for display in UI.
    // This gets called every time the number of generations or time between generations is updated.
    public void CalculateApproximateTime()
    {
        try
        {
            timePerGen = float.Parse(timePerGenInput.text);
            numGens = int.Parse(numGensInput.text);
            float totalSeconds = numGens * timePerGen * NUM_SWINGS_PER_GEN;
            int hours = TimeSpan.FromSeconds(totalSeconds).Hours;
            int minutes = TimeSpan.FromSeconds(totalSeconds).Minutes;
            int seconds = TimeSpan.FromSeconds(totalSeconds).Seconds;
            string expectedTime = "";
            if (hours > 0)
                expectedTime += hours + " hours ";
            if (minutes > 0)
                expectedTime += minutes + " minutes ";
            if (seconds > 0)
                expectedTime += seconds + " seconds ";

            approxTime.text = expectedTime;
        }
        catch
        {
            Debug.LogWarning("Time Per Generation and Number of Generations should both be numbers!");
            return;
        }
    }

    // Called when the user clicks the "Begin Simulation" button
    // Processes the input
    public void BeginSimulation()
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
        chroms = new Chromosome[numAgents];
        agents = new GameObject[numAgents];
        fitnesses = new float[numAgents];
        fitnessTrack = new float[numGens, numAgents]; // 2D array with fitness for each gen

        // determine the number of joints based on whether we're using the golfer's full body or not
        int numJoints = (moveableJoints == GolferSettings.MoveableJointsExtent.fullBody ? 12 : 8);

        // initialize chromosomes with random values
        for (int i = 0; i < chroms.Length; i++)
        {
            // create Vector3 array of random torques to give to the chromosome
            Vector3[] initTorques = new Vector3[numJoints];
            for (int j = 0; j < initTorques.Length; j++)
            {
                initTorques[j] = new Vector3(Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                             Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG),
                                             Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG));
            }
            chroms[i] = new Chromosome(initTorques);
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
                    agents[j] = Instantiate(agentPrefab, new Vector3(0, 0, - j * DIST_BETWEEN_AGENTS), Quaternion.identity);
                    agents[j].GetComponent<GolferBrain>().InitializeAgent(chroms[j], settings, holeDistOffset);
                    agents[j].GetComponent<GolferBrain>().BeginSwinging();
                }

                // Allow this generation to run for the specified time.
                // Execution of this code pauses here until time is up, then automatically resumes.
                yield return new WaitForSeconds(timePerGen);

                // Get the fitnesses of the agents and then clear them out
                for (int j = 0; j < agents.Length; j++)
                {
                    fitnesses[j] = agents[j].GetComponent<GolferBrain>().GetFitness();
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
            Chromosome[] newChroms = new Chromosome[chroms.Length];

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
                    newChroms[j] = chroms[Array.FindIndex(fitnesses, x => x == sortedFitnesses[j])];
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
                    Tuple<Chromosome, Chromosome> xresult = Crossover(chroms[par1idx], chroms[par2idx]);
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
    private Tuple<Chromosome, Chromosome> Crossover(Chromosome parentOne, Chromosome parentTwo)
    {
        int torqueLength = parentOne.torques.Length;
        int crossPoint = Random.Range(1,torqueLength);
        //Debug.Log("Before: " + parentOne.torques + ", " + parentTwo.torques);
        //Debug.Log("Cross point: " + crossPoint);
        for(int i = crossPoint; i < torqueLength; i++){
            Vector3 temp = parentOne.torques[i];
            parentOne.torques[i] = parentTwo.torques[i];
            parentTwo.torques[i] = temp;
        }
        //Debug.Log("After: " + parentOne.torques + ", " + parentTwo.torques + "\n");
        return new Tuple<Chromosome, Chromosome>(parentOne, parentTwo);
    }


    /*
      Handles mutating a chromosome passed in by adding a randomly generated value
      to the torques of the chromosome
      @author Ernest Essuah Mensah
    */
    private Chromosome Mutate(Chromosome parent)
    {
        // Copy torques from parent to mutate
        Vector3[] torques = new Vector3[parent.torques.Length];
        
        for (int i = 0; i < parent.torques.Length; i++) {

            Vector3 current = parent.torques[i];
	  
            // Pick a random axis to mutate
            // 0: x-axis; 1: y-axis, 2: z-axis
            int mutationAxis = Random.Range(0,2);
            float mutation = Random.Range(-INIT_TORQUE_MAG, INIT_TORQUE_MAG);
            Vector3 newMovement;
                
            if (mutationAxis == 0) {
                newMovement = new Vector3(current.x + mutation, current.y, current.z);
                
            } else if (mutationAxis == 1) {
                newMovement = new Vector3(current.x, current.y + mutation, current.z);
            
            } else {
                newMovement = new Vector3(current.x, current.y, current.z + mutation);
            }

            torques[i] = newMovement;
        }

        return new Chromosome(torques);
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
        filename += "_chrom-1";
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
