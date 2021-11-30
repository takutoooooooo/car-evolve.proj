// SerialID: [1ba2ce2c-2b2a-4e6d-9764-8ce1f38e28f0]
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[CreateAssetMenu]
public class CarGeneOperator : GeneOperator
{
    // 変数の範囲
    // 必要に応じてこの部分を変更してもよいが、GAのパラメータではなく
    // 問題側のパラメーターであることに注意
    // body size
    private float MinRadius = 0.7f;
    private float MaxRadius = 1.6f;

    // wheel size
    private float MinWheelSize = 0.6f;
    private float MaxWheelSize = 1.8f;
    
    // power
    private float MinTorque = 2.0f;
    private float MaxTorque = 30.0f;

    private int VertexesNumber = 12;

    [Header("Mutation Variables"),SerializeField] private float PosProb = 0.5f;
    [SerializeField] private float RadiusProb = 0.5f;
    [SerializeField] private float RadiusMutationSize = 0.5f;

    [SerializeField] private float AngleProb = 0.5f;
    [SerializeField] private float AngleMutationSize = 0.5f;

    [SerializeField] private float WheelSizeProb = 0.5f;
    [SerializeField] private float WheelSizeMutationSize = 0.5f;

    [SerializeField] private float TorqueProb = 0.5f;
    [SerializeField] private float TorqueMutationSize = 0.5f;
    private float theta;
    private float eps = 0.00001f;

    void OnEnable()
    {
        // 角度に関する定数
        theta = Mathf.PI/VertexesNumber;
    }

    /*
    Gene.data[i] は以下の各要素に対応している
    0 : 前輪位置　([0,頂点数)、タイヤが付く頂点の番号)
    1 : 後輪位置
    2 : 前輪大きさ 
    3 : 後輪大きさ
    4 : 前輪トルク (タイヤには常にトルクが与えられ、これによって前進する)
    5 : 後輪トルク
    6~11  : 車体の各頂点の中心からの距離
    12~17 : 車体の各頂点の角度

     - 車体について
    車体の形は多角柱を横にしたもので表現される
    底面の多角形の頂点の位置を極座標形式で指定する
    頂点数を N, t = PI/Nとおくと、i番目の頂点の角度は [2ti - t, 2ti + t] の範囲内

     - タイヤ位置について
    いずれかの頂点に位置する.
    Gene.dataがfloat型なので実際に使う際はintにキャストする(CarAgent.cs ApplyGene())

     - トルクについて
    常に一定のトルクを与えることで前進する(MyWheel.cs FixedUpdate参照)
    */

    // 交叉と複製は基本的なものがGeneOperator.csに実装されている。
    // overrideして新しいものを実装してもよい。

    // Geneの初期化
    public override Gene Init(){
        var gene = new Gene();
        for(int i = 0;i < 2;i++)gene.data.Add(Random.Range(0.0f,VertexesNumber - eps));
        for(int i = 0;i < 2;i++)gene.data.Add(Random.Range(MinWheelSize,MaxWheelSize));
        for(int i = 0;i < 2;i++)gene.data.Add(Random.Range(MinTorque,MaxTorque));
        for(int i = 0;i < VertexesNumber;i++)gene.data.Add(Random.Range(MinRadius,MaxRadius));
        for(int i = 0;i < VertexesNumber;i++)gene.data.Add(2*theta*i + Random.Range(-theta,theta));
        return gene;
    }

    // 突然変異
    // 各変数について、変異のおこる確率、変異の大きさを決定するパラメーターをInspectorから設定できる
    public override Gene Mutate(Gene gene,int generation){
        var mutated_gene = new Gene();
        mutated_gene.data = new List<float>(gene.data);

        float mutrate = MutRate(generation);
        float r,p;
        p = mutrate * PosProb;
        for(int i = 0;i <= 1;i++){
            if(Random.value < p)mutated_gene.data[i] = Random.Range(0.0f,VertexesNumber - eps);
        }
        p = mutrate * WheelSizeProb;
        r = mutrate * WheelSizeMutationSize;
        for(int i = 2;i <= 3;i++){
            if(Random.value < p)mutated_gene.data[i] = MutateClamp(mutated_gene.data[i],r,MinWheelSize,MaxWheelSize);
        }
        p = mutrate * TorqueProb;
        r = mutrate * TorqueMutationSize;
        for(int i = 4;i <= 5;i++){
            if(Random.value < p)mutated_gene.data[i] = MutateClamp(mutated_gene.data[i],r,MinTorque,MaxTorque);
        }
        p = mutrate * RadiusProb;
        r = mutrate * RadiusMutationSize;
        for(int i = 6;i < 6+VertexesNumber;i++){
            if(Random.value < p)mutated_gene.data[i] = MutateClamp(mutated_gene.data[i],r,MinRadius,MaxRadius);
        }
        p = mutrate * AngleProb;
        r = mutrate * AngleMutationSize;
        for(int i = 0;i < VertexesNumber;i++){
            if(Random.value < p)mutated_gene.data[6+VertexesNumber+i] = MutateClamp(mutated_gene.data[6+VertexesNumber+i],r,2*theta*i - theta, 2*theta*i + theta);
        }
        return mutated_gene;
    }

    // 世代によって変化する値 
    private float MutRate(int generation) {
        // 0世代目 1, b世代目に a になる値
        // [0,1] の範囲
        float a = 0.05f;
        float b = 30.0f;
        return a + (1.0f - a) * Mathf.Max(0f, 1.0f - generation / b);
    }
    
    private float MutateClamp(float x, float p, float min, float max){
        //　値がとりうる範囲の 100p% の範囲で摂動を与える。
        // 0 <= p <= 1
        float r = (max - min)*p*0.5f;
        x += Random.Range(-r, r);
        x = Mathf.Max(x, min);
        x = Mathf.Min(x, max);
        return x;
    }
    
}
