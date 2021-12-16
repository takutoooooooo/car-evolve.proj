// SerialID: [1ba2ce2c-2b2a-4e6d-9764-8ce1f38e28f0]
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public abstract class GeneOperator : ScriptableObject
{ 
    public abstract Gene Init();
    public virtual Gene Clone(Gene gene){
        var cloned_gene = new Gene();
        cloned_gene.data = new List<float>(gene.data);
        return cloned_gene;
    }
    public abstract Gene Mutate(Gene gene,int generation);
    public virtual (Gene,Gene) Crossover(Gene gene1,Gene gene2,int generation){
        int Length = gene1.data.Count;
        var child_gene1 = new Gene();
        var child_gene2 = new Gene();
        child_gene1.data = new List<float>(gene1.data);
        child_gene2.data = new List<float>(gene2.data);
        var alpha = 0.3f;
        for(int i = 0;i < Length;i++){
            var min_x = Math.Min(gene1.data[i], gene2.data[i]);
            var max_x = Math.Max(gene1.data[i], gene2.data[i]);
            var dx = max_x - min_x;
            var min_cx = min_x - alpha * dx;
            var max_cx = max_x + alpha * dx;
            child_gene1.data[i] = UnityEngine.Random.Range(min_cx, max_cx);
            child_gene2.data[i] = UnityEngine.Random.Range(min_cx, max_cx);
        }
        return (child_gene1,child_gene2);
    }
}
