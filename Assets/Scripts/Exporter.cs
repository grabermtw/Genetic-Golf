using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Exporter : MonoBehaviour
{
    private int numFinished = 0;
    
    // When this has been called 3 times, export the CSV
    public void FinishedSimulation()
    {
        numFinished++;
        if(numFinished == 3)
            ExportCSV();
    }


    /* @author John Gansallo */
    public void ExportCSV()
    {
        // get all the data needed for the filename
        GeneticManager chrom1Manager = GetComponent<GeneticManager>();
        int numGens = chrom1Manager.numGens;
        int numAgents = chrom1Manager.numAgents;
        GolferSettings.Fitness fitnessFunc = chrom1Manager.fitnessFunc;
        GolferSettings.MoveableJointsExtent moveableJoints = chrom1Manager.moveableJoints;
        GolferSettings.ClubGrip clubGrip = chrom1Manager.clubGrip;
        float holeDist = chrom1Manager.holeDist;
        float holeDistRand = chrom1Manager.holeDistRand;
        float timePerGen = chrom1Manager.timePerGen;
        float crossoverProb = chrom1Manager.crossoverProb;
        float mutationProb = chrom1Manager.mutationProb;
        int numElites = chrom1Manager.numElites;
        // get the actual data for the csv
        float[,] results1 = chrom1Manager.GetResults();
        float[,] results2 = GetComponent<GeneticManager2>().GetResults();
        float[,] results3 = GetComponent<GeneticManager3>().GetResults();

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
	    string path = Application.dataPath + @"/" + filename + ".csv";
        if (!File.Exists(path))
        {
            // Create a file to write to.
            using (StreamWriter gen = File.CreateText(path))
            {
                gen.WriteLine("Generation #,Chrom1 Best Fitness,Chrom1 Average Fitness,Chrom1 Best Fitness,Chrom3 Average Fitness,Chrom3 Best Fitness,Chrom3 Average Fitness");
		        for (int i = 0; i < results1.GetLength(0); i++)
                {
                    float avgFit1 = 0;
                    float avgFit2 = 0;
                    float avgFit3 = 0;
                    float bestFit1 = Single.MinValue;
                    float bestFit2 = Single.MinValue;
                    float bestFit3 = Single.MinValue;
                    for (int j = 0; j < numAgents; j++)
                    {
                        avgFit1 += results1[i,j];
                        avgFit2 += results2[i,j];
                        avgFit3 += results3[i,j];
                        if (results1[i,j] > bestFit1)
                            bestFit1 = results1[i,j];
                        if (results2[i,j] > bestFit2)
                            bestFit2 = results2[i,j];
                        if (results3[i,j] > bestFit3)
                            bestFit3 = results3[i,j];
                    }
                    avgFit1 = avgFit1 / numAgents;
                    avgFit2 = avgFit2 / numAgents;
                    avgFit3 = avgFit3 / numAgents;
                    gen.WriteLine((i + 1).ToString() + "," + bestFit1 + "," + avgFit1 + "," + bestFit2 + "," + avgFit2 + "," + bestFit3 + "," + avgFit3);
                }
            }	
        }

    }
}
