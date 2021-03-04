using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadControllerTest : MonoBehaviour
{
    public bool drawPotentialMap;
    [Range(1f, 750f)]
    public float maxPotentialValue;
    public List<Collider> enemyColliders;
    public List<UnitControllerTest> unitsInChunk;
    float[][,] m_PotentialMap;
    HashSet<Vector2Int> m_AllPositions = new HashSet<Vector2Int>();
    (Vector3 unitPos, Vector3 dir)[] m_UnitsPotentialDirection;
    private Vector3[] m_EnemyUnitsPos;
    //private float m_CashedPotentialValue = 0f;
    
    [Range(0.1f, 5f)]
    public float unitRadius = 1f;


    private UnitControllerTest m_King;
    // Start is called before the first frame update
    void Start()
    {
        foreach (var unitController in unitsInChunk)
        {
            if (unitController.IsKing)
                m_King = unitController;
        }
    }

    // Update is called once per frame
    void Update()
    {
        m_AllPositions.Clear();

        m_AllPositions.Add(new Vector2Int((int) m_King.transform.position.x, (int) m_King.transform.position.z));
        BakePotentialMap();
    }

    private void BakePotentialMap()
    {
        m_EnemyUnitsPos = PotentialFieldControllerTest.GetObstacleCellsPos(enemyColliders);
        m_UnitsPotentialDirection = new (Vector3 unitPos, Vector3 dir)[unitsInChunk.Count];
        m_PotentialMap = new float[m_AllPositions.Count][,];

        int unitIndex = 0;
        int chunkIndex = 0;

        //if (m_enemyUnits.Count != 0 && allPositions.Count != 0)
        foreach (var position in m_AllPositions)
        {
            unitIndex = 0;
            float leftAreaBound = position.x - PotentialFieldControllerTest.s_ChunkDemX / 2;
            float bottomAreaBound = position.y - PotentialFieldControllerTest.s_ChunkDemY / 2;

            m_PotentialMap[chunkIndex] = PotentialFieldControllerTest.GetPotentialMap(m_EnemyUnitsPos, leftAreaBound, bottomAreaBound, unitRadius);

            foreach (var unit in unitsInChunk)
            {
                var unitPos = unit.transform.position;

                (Vector3 optimalPoint, float potentialValue) =
                    PotentialFieldControllerTest.CalculateOptimalPotentialVector(unitPos, m_PotentialMap[chunkIndex], leftAreaBound, bottomAreaBound);
                m_UnitsPotentialDirection[unitIndex] = (unitPos, optimalPoint);
                
                 unit.DirectionToMinPotential = optimalPoint - unitPos;
                 unit.PotentialValue = potentialValue;
                 unit.MaxPotentialValue = maxPotentialValue;

                unitIndex++;
            }

            chunkIndex++;
        }
    }

    void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            if (m_EnemyUnitsPos != null)
            foreach (var enemyUnitPos in m_EnemyUnitsPos)
            {
                Gizmos.DrawSphere(enemyUnitPos, .2f);
            }
            Gizmos.color = Color.white;
            
            int chunkIndex = 0;
            if (drawPotentialMap && m_AllPositions.Count > 0)
                foreach (var position in m_AllPositions)
                {
                    float leftAreaBound = position.x - PotentialFieldControllerTest.s_ChunkDemX / 2;
                    float bottomAreaBound = position.y - PotentialFieldControllerTest.s_ChunkDemY / 2;
                    var potentialMap = m_PotentialMap[chunkIndex];
                    for (int i = 0; i < PotentialFieldControllerTest.AreaWidth; i++)
                    {
                        for (int j = 0; j < PotentialFieldControllerTest.AreaHeight; j++)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawSphere(new Vector3(leftAreaBound, 0, bottomAreaBound), .2f);
                            if (potentialMap[i, j] != 0)
                            {
                                Gizmos.color = new Color(potentialMap[i, j] * 0.1f, potentialMap[i, j] * 0.1f, potentialMap[i, j] * 0.1f);
                                //Gizmos.color = Color.yellow;
                            }
                            else
                                Gizmos.color = Color.blue;
                            Gizmos.DrawSphere(PotentialFieldControllerTest.PotentialMapPosToPosition(i,j,leftAreaBound,bottomAreaBound), .2f);
                        }
                    }

                    chunkIndex++;
                }
            
            Gizmos.color = Color.white;
            
            // Gizmos.color = Color.yellow;
            // if (unitsInChunks != null)
            //     foreach (var unit in unitsInChunks)
            //     {
            //         if (unit)
            //         Gizmos.DrawSphere(unit.transform.position + Vector3.up * 1f, .2f);
            //     }
            //
            // Gizmos.color = Color.white;
            
            Gizmos.color = Color.magenta;
            
            if (m_UnitsPotentialDirection != null)
                foreach (var direction in m_UnitsPotentialDirection)
                {
                    Gizmos.DrawLine(direction.unitPos + Vector3.up * 1f, direction.dir + Vector3.up * 1f);
                }
            
            Gizmos.color = Color.white;
        }
    
        
}
