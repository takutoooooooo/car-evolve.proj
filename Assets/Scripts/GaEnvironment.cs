// SerialID: [1ba2ce2c-2b2a-4e6d-9764-8ce1f38e28f0]
using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;
using System.Linq;
using UnityEngine.UI;
using System.Diagnostics; 
using Debug = UnityEngine.Debug;

public class GaEnvironment : Environment
{
    [Header("Settings"), SerializeField] private int totalPopulation = 100;
    private int TotalPopulation { get { return totalPopulation; } }

    [SerializeField] private int tournamentSelection = 85;
    private int TournamentSelection { get { return tournamentSelection; } }

    [SerializeField] private int eliteSelection = 4;
    private int EliteSelection { get { return eliteSelection; } }

    [SerializeField] private int nAgents = 4;
    private int NAgents { get { return nAgents; } }

    [Header("Agent Prefab"), SerializeField] private GameObject GObjectAgent = null;

    [Header("UI References"), SerializeField] private Text populationText = null;
    private Text PopulationText { get { return populationText; } }

    private float GenBestRecord { get; set; }

    private float SumFitness { get; set; }
    private float AvgFitness { get; set; }

    private List<GameObject> GObjects { get; } = new List<GameObject>();
    private List<Agent> Agents { get; } = new List<Agent>();
    private List<Gene> Genes { get; set; } = new List<Gene>();
    private int Generation { get; set; }

    private float BestRecord { get; set; }

    private List<AgentPair> AgentsSet { get; } = new List<AgentPair>();
    private Queue<Gene> CurrentGenes { get; set; }

    [Header("Gene"),SerializeField] private GeneOperator Operator = null;
    
    //DateTime dt;
    //private string filename;
    private string logpath ;
    private GroundGenerator Ground;

    // 個体オブジェクトと遺伝子を生成
    // 個体オブジェクトはNAgentsコだけ作って使いまわす
    void Awake() {
        for(int i = 0; i < TotalPopulation; i++) {
            Gene gene = Operator.Init();
            Genes.Add(gene);
        }

        for(int i = 0; i < NAgents; i++) {
            var obj = Instantiate(GObjectAgent);
            obj.SetActive(true);
            GObjects.Add(obj);
            Agents.Add(obj.GetComponent<Agent>());
        }
    }

    void Start()
    {
        SetStartAgents();
        Ground = GameObject.Find("Grounds").GetComponent<GroundGenerator>();
        DateTime dt = DateTime.Now;
        string filename = dt.ToString("ddHHmm");
        logpath = $"Assets/data/blx_alpha/test_{filename}.csv";
        Debug.Log(logpath);
        File.WriteAllText(logpath, "generations, 最高距離, 平均\n");
    }

    // Agent,Geneを組としてAgentsSetにいれる
    // AgentsSetは生きているAgentとGeneの組を扱うList
    // GeneをAgentに適用する
    void SetStartAgents() {
        CurrentGenes = new Queue<Gene>(Genes);
        AgentsSet.Clear();
        var size = Math.Min(NAgents, TotalPopulation);
        for(var i = 0; i < size; i++) {
            AgentsSet.Add(new AgentPair {
                agent = Agents[i],
                gene = CurrentGenes.Dequeue()
            });
        }
        foreach(var pair in AgentsSet){
            pair.agent.ApplyGene(pair.gene);
        }
    }

    // 生きているAgentを更新
    // 死んでしまったAgentは報酬の処理をして除去
    // 次の世代を生成、もしくは次のAgent,Geneの組を追加
    void FixedUpdate() {
        foreach(var pair in AgentsSet.Where(p => !p.agent.IsDone)) {
            pair.agent.AgentUpdate();
        }

        AgentsSet.RemoveAll(p => {
            if(p.agent.IsDone) {
                float r = p.agent.Fitness;
                BestRecord = Mathf.Max(r, BestRecord);
                GenBestRecord = Mathf.Max(r, GenBestRecord);
                p.gene.Fitness = r;
                SumFitness += r;
            }
            return p.agent.IsDone;
        });

        if(CurrentGenes.Count == 0 && AgentsSet.Count == 0) {
            SetNextGeneration();
        }
        else {
            SetNextAgents();
        }
    }

    private void SetNextAgents() {
        int size = Math.Min(NAgents - AgentsSet.Count, CurrentGenes.Count);
        for(var i = 0; i < size; i++) {
            var nextAgent = Agents.First(a => a.IsDone);
            var nextGene = CurrentGenes.Dequeue();
            nextAgent.Reset();
            nextAgent.ApplyGene(nextGene);
            AgentsSet.Add(new AgentPair {
                agent = nextAgent,
                gene = nextGene
            });
        }
        UpdateText();
    }

    private void SetNextGeneration() {
        AvgFitness = SumFitness / TotalPopulation;
        Debug.Log(logpath);
        //File.AppendAllText(logpath, "0");
        File.AppendAllText(logpath, $"{Generation + 1}, {GenBestRecord}, {AvgFitness}\n");
        //new generation
        GenPopulation();
        SumFitness = 0;
        GenBestRecord = 0;
        Agents.ForEach(a => a.Reset());
        SetStartAgents();
        UpdateText();
        Ground.MakeGroundData();
        Ground.CreateMesh();
    }

    // 適応度で降順ソートするための関数
    private static int CompareGenes(Gene a, Gene b) {
        if(a.Fitness > b.Fitness) return -1;
        if(b.Fitness > a.Fitness) return 1;
        return 0;
    }


    // 選択、交叉、突然変異といった遺伝的操作をくわえて次の世代を生成する
    private void GenPopulation() {
        var children = new List<Gene>();
        var bestGenes = Genes.ToList();
        //Elite selection
        bestGenes.Sort(CompareGenes);
        for(int i = 0; i < EliteSelection;i++){
            children.Add(Operator.Clone(bestGenes[i]));
        }
        float mutate_only = 0.3f;
        // トーナメント選択 + 突然変異
        while(children.Count < TotalPopulation * mutate_only) {
            var tournamentMembers = Genes.AsEnumerable().OrderBy(x => Guid.NewGuid()).Take(tournamentSelection).ToList();
            tournamentMembers.Sort(CompareGenes);
            children.Add(Operator.Mutate(tournamentMembers[0],Generation));
            if(children.Count < TotalPopulation * mutate_only) children.Add(Operator.Mutate(tournamentMembers[1],Generation));
        }
        // トーナメント選択 + 交叉
        while(children.Count < TotalPopulation) {
            var tournamentMembers = Genes.AsEnumerable().OrderBy(x => Guid.NewGuid()).Take(tournamentSelection).ToList();
            tournamentMembers.Sort(CompareGenes);
            var (child1,child2) = Operator.Crossover(tournamentMembers[0],tournamentMembers[1],Generation);
            children.Add(child1);
            if(children.Count < TotalPopulation)children.Add(child2);
        }
        Genes = children;
        Generation++;
    }

    private void UpdateText() {
        PopulationText.text = "Population: " + (TotalPopulation - CurrentGenes.Count) + "/" + TotalPopulation
            + "\nGeneration: " + (Generation + 1)
            + "\nBest Record: " + BestRecord
            + "\nBest this gen: " + GenBestRecord
            + "\nAverage: " + AvgFitness;
    }

    private struct AgentPair
    {
        public Gene gene;
        public Agent agent;
    }
}
