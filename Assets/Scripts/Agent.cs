// SerialID: [1ba2ce2c-2b2a-4e6d-9764-8ce1f38e28f0]
using System.Collections.Generic;
using UnityEngine;

public abstract class Agent : MonoBehaviour
{
    public bool IsDone { get; private set; }
    public bool IsFrozen { get; private set; }

    public float Fitness { get; private set; }

    public void SetFitness(float fitness) {
        Fitness = fitness;
    }

    public void AddFitness(float fitness) {
        Fitness += fitness;
    }

    public abstract void AgentUpdate();

    public abstract void AgentReset();

    public abstract void ApplyGene(Gene gene);


    public abstract void Stop();

    public void Done()
    {
        IsDone = true;
    }

    public void Freeze()
    {
        Stop();
        IsFrozen = true;
        //gameObject.SetActive(false);
    }

    public void Reset()
    {
        //gameObject.SetActive(true);
        Stop();
        AgentReset();
        IsDone = false;
        IsFrozen = false;
    }
}
