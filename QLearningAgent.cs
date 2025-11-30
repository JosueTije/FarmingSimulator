using System;
using UnityEngine;

[Serializable]
public class QLearningAgent
{
    int numStates;
    int numActions;
    float[,] Q;

    public float alpha = 0.1f;   // learning rate
    public float gamma = 0.9f;   // discount factor
    public float epsilon = 0.25f; // exploration rate

    public QLearningAgent(int numStates, int numActions)
    {
        this.numStates = numStates;
        this.numActions = numActions;
        Q = new float[numStates, numActions];
    }

    public int ChooseAction(int state)
    {
        // exploración
        if (UnityEngine.Random.value < epsilon)
            return UnityEngine.Random.Range(0, numActions);

        // explotación
        float best = float.NegativeInfinity;
        int bestA = 0;

        for (int a = 0; a < numActions; a++)
        {
            float q = Q[state, a];
            if (q > best)
            {
                best = q;
                bestA = a;
            }
        }

        return bestA;
    }

    public void UpdateQ(int s, int a, float reward, int sNext)
    {
        float oldQ = Q[s, a];

        float maxNext = float.NegativeInfinity;
        for (int ap = 0; ap < numActions; ap++)
        {
            if (Q[sNext, ap] > maxNext)
                maxNext = Q[sNext, ap];
        }

        float target = reward + gamma * maxNext;
        Q[s, a] = oldQ + alpha * (target - oldQ);
    }
}
