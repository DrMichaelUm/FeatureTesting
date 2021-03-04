using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadController : MonoBehaviour
{
    public bool drawPotentialMap;
    [Range(1f, 750f)]
    public float maxPotentialValue;
    public List<Collider> enemyColliders;
    public List<UnitController> unitsInChunk;
    float[][,] m_potentialMap;
    HashSet<Vector2Int> allPositions = new HashSet<Vector2Int>();
    (Vector3 unitPos, Vector3 dir)[] unitsPotentialDirection;
    private Vector3[] m_enemyUnitsPos;
    private float cashedPotentialValue = 0f;
    
    public float resolution = 1f;
    [Range(0.1f, 5f)]
    public float unitRadius = 1f;
    readonly uint m_chunkDemX = 20;
    readonly uint m_chunkDemY = 20;
    uint AreaWidth
    {
        get => (uint)(m_chunkDemX / resolution);
    }
    uint AreaHeight
    {
        get => (uint)(m_chunkDemY / resolution);
    }


    private UnitController m_king;
    // Start is called before the first frame update
    void Start()
    {
        foreach (var unitController in unitsInChunk)
        {
            if (unitController.IsKing)
                m_king = unitController;
        }
    }

    // Update is called once per frame
    void Update()
    {
        allPositions.Clear();

        allPositions.Add(new Vector2Int((int) m_king.transform.position.x, (int) m_king.transform.position.z));
        BakePotentialMap();
    }

    private void BakePotentialMap()
    {
        m_enemyUnitsPos = PotentialFieldController.GetObstacleCellsPos(enemyColliders);
        unitsPotentialDirection = new (Vector3 unitPos, Vector3 dir)[unitsInChunk.Count];
        m_potentialMap = new float[allPositions.Count][,];

        int unitIndex = 0;
        int chunkIndex = 0;

        //if (m_enemyUnits.Count != 0 && allPositions.Count != 0)
        foreach (var position in allPositions)
        {
            unitIndex = 0;
            float leftAreaBound = position.x - m_chunkDemX / 2;
            float bottomAreaBound = position.y - m_chunkDemY / 2;
            m_potentialMap[chunkIndex] = new float[AreaWidth, AreaHeight];

            PotentialFieldController.GetPotentialMap(in m_potentialMap[chunkIndex], m_enemyUnitsPos, leftAreaBound,
                                                     bottomAreaBound, AreaWidth, AreaHeight, resolution, unitRadius);

            foreach (var unit in unitsInChunk)
            {
                var unitPos = unit.transform.position;

                (Vector3 optimalPoint, float potentialValue) =
                    PotentialFieldController.CalculateOptimalPotentialVector(unitPos,
                                                                             m_potentialMap[chunkIndex], leftAreaBound,
                                                                             bottomAreaBound, resolution);
                unitsPotentialDirection[unitIndex] = (unitPos, optimalPoint);
                
                
                //unit.AddToTargetDirection(optimalPoint - unitPos);
                
                //nit.LerpToTargetDirection(optimalPoint - unitPos, potentialValue, 74);
                 unit.CorrectionDirection = optimalPoint - unitPos;
                 unit.PotentialValue = potentialValue;
                 unit.MaxPotentialValue = maxPotentialValue;
                
                // if (potentialValue > cashedPotentialValue)
                // {
                //     Debug.Log(potentialValue);
                //     cashedPotentialValue = potentialValue;
                // }

                unitIndex++;
            }

            chunkIndex++;
        }
    }

    void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            if (m_enemyUnitsPos != null)
            foreach (var enemyUnitPos in m_enemyUnitsPos)
            {
                Gizmos.DrawSphere(enemyUnitPos, .2f);
            }
            Gizmos.color = Color.white;
            
            int chunkIndex = 0;
            if (drawPotentialMap && allPositions.Count > 0)
                foreach (var position in allPositions)
                {
                    float leftAreaBound = position.x - m_chunkDemX / 2;
                    float bottomAreaBound = position.y - m_chunkDemY / 2;
                    var potentialMap = m_potentialMap[chunkIndex];
                    for (int i = 0; i < AreaWidth; i++)
                    {
                        for (int j = 0; j < AreaHeight; j++)
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
                            Gizmos.DrawSphere(new Vector3(i * resolution + leftAreaBound, 0, j * resolution + bottomAreaBound), .2f);
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
            
            if (unitsPotentialDirection != null)
                foreach (var direction in unitsPotentialDirection)
                {
                    Gizmos.DrawLine(direction.unitPos + Vector3.up * 1f, direction.dir + Vector3.up * 1f);
                }
            
            Gizmos.color = Color.white;
        }
    
        
}
