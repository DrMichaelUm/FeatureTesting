using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PotentialField : MonoBehaviour
{
    public float radius = 5f;
    [Range(0.1f, 5f)] public float resolution = .5f;

    private const float KR = 100f;
    private const float AREA_WIDTH = 50f;

    public List<Collider> obstacles;

    private float[,] m_potentialMap;

    private Vector3 m_potentialDir = Vector3.zero;
    private float m_potentialValue;

    private float m_leftAreaBound = 0;
    private float m_bottomAreaBound = 0;
    private int m_areaWidth = 0;
    private int m_areaHeight = 0;

    private float speed = 5f;
    void Start()
    {
        Vector3[] obsCellsPos;
        obsCellsPos = GetObstacleCellsPos(obstacles);

        (m_leftAreaBound, m_bottomAreaBound, m_areaWidth, m_areaHeight) = CalculateAreaParams(obsCellsPos, resolution);
        
        m_potentialMap = CalculatePotentialField(obsCellsPos, m_areaWidth, m_areaHeight, m_leftAreaBound, m_bottomAreaBound, resolution, radius);
    }

    Vector3[] GetObstacleCellsPos(List<Collider> obstacles)
    {
        List<Vector3> obsPos = new List<Vector3>();
        foreach (var obstacle in obstacles)
        {
            float minX = obstacle.bounds.min.x;
            float minY = obstacle.bounds.min.y;
            float maxX = obstacle.bounds.max.x;
            float maxY = obstacle.bounds.max.y;

            for (int i = 0; i <= maxX - minX; i++)
            {
                float x = minX + i;
                //Debug.Log("-----X: " + x + "-----------");
                for (int j = 0; j <= maxY - minY; j++)
                {
                    float y = minY + j;

                    if (x == minX || x == maxX || y == minY || y == maxY)
                    {
                        //Debug.Log("x " + x + "; y " + y);
                        obsPos.Add(new Vector3(x, y, 0));
                    }
                }
            }
        }
        
        return obsPos.ToArray();
    }
    
    (float minX, float minY, int width, int height) CalculateAreaParams (Vector3[] obsPos, float res)
    {
        Vector3 leftBottomObstacle = FindPos(obsPos, 0);
        Vector3 rightTopObstacle = FindPos(obsPos, 1);
        
        float leftAreaBound = leftBottomObstacle.x - AREA_WIDTH / 2f;
        float bottomAreaBound = leftBottomObstacle.y - AREA_WIDTH / 2f;
        int areaWidth = (int) Mathf.Round((rightTopObstacle.x - leftBottomObstacle.x + AREA_WIDTH) / res);
        int areaHeight = (int) Mathf.Round((rightTopObstacle.x - leftBottomObstacle.y + AREA_WIDTH) / res);

        return (leftAreaBound, bottomAreaBound, areaWidth, areaHeight);
    }

    void Update()
    {
        SimpleMovement();

        (m_potentialDir, m_potentialValue) = 
            CalculateOptimalPotentialVector(transform.position, m_potentialMap, m_leftAreaBound, m_bottomAreaBound, resolution);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.magenta;
        if (m_potentialValue > 0)
            Gizmos.DrawLine(transform.position + Vector3.forward * -6, m_potentialDir + Vector3.forward * -6);
        Gizmos.color = Color.white;
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(new Vector3(m_leftAreaBound, m_bottomAreaBound, -10), .5f);
        Gizmos.DrawSphere(new Vector3(m_areaWidth * resolution + m_leftAreaBound, m_areaHeight * resolution + m_bottomAreaBound, -10), .5f);
        Gizmos.color = Color.white;
        
        
        for (int i = 0; i < m_areaWidth; i++)
        {
            for (int j = 0; j < m_areaHeight; j++)
            {
                if (m_potentialMap[i, j] != 0)
                {
                    Gizmos.color = new Color(m_potentialMap[i,j]* 0.1f, m_potentialMap[i,j]* 0.1f, m_potentialMap[i,j]* 0.1f);
                    Gizmos.DrawSphere(new Vector3(i * resolution + m_leftAreaBound, j * resolution + m_bottomAreaBound, -5), .1f);
                }
            }
        }
        Gizmos.color = Color.white;
    }
    
    (Vector3 potentialDirection, float potentialValue) CalculateOptimalPotentialVector (Vector3 startPos, float[,] potentialMap, float minX, float minY, float res)
    {
        startPos.x = Mathf.Round((startPos.x - minX) / res);
        startPos.y = Mathf.Round((startPos.y - minY) / res);
        
        float[,] motionMap = GetMotionMap();
        
        float minPotential = float.MaxValue;
        float minMovedX = -1, minMovedY = -1;

        for (int i = 0; i < motionMap.GetLength(0); i++)
        {
            int movedX = (int)(startPos.x + motionMap[i,0]);
            int movedY = (int)(startPos.y + motionMap[i,1]);
            float currentPotential = float.MaxValue;
            
            if (movedX < potentialMap.GetLength(0) && movedY < potentialMap.GetLength(1))
                currentPotential = potentialMap[movedX, movedY];

            if (currentPotential < minPotential)
            {
                minPotential = currentPotential;
                minMovedX = movedX;
                minMovedY = movedY;
            }
        }

        return (new Vector3(minMovedX * res + minX, minMovedY * res + minY, 0), minPotential);
    }
    
    float[,] CalculatePotentialField (Vector3[] obsPos, int areaWidth, int areaHeight, float minx, float miny, float res,
                                      float unitRadius)
    {
        float[,] potentialMap = new float[areaWidth, areaHeight];
        
        for (int i = 0; i < areaWidth; i++)
        {
            float x = i * res + minx;

            for (int j = 0; j < areaHeight; j++)
            {
                float y = j * res + miny;

                float uAttractionPotential = 0;
                float uRepulsionPotential = CalculateRepulsivePotential (x, y, obsPos, unitRadius);
                float uForce = uAttractionPotential + uRepulsionPotential;

                potentialMap[i, j] = uForce;
            }
        }

        return potentialMap;
    }
    
    float CalculateRepulsivePotential (float x, float y, Vector3[] obsPos, float unitRadius)
    {
        float minDistance = float.MaxValue;

        for (int i = 0; i < obsPos.Length; i++)
        {
            float dist = Mathf.Pow(x - obsPos[i].x, 2) + Mathf.Pow(y - obsPos[i].y, 2);

            if (dist <= minDistance)
                minDistance = dist;
        }

        minDistance = Mathf.Sqrt(minDistance);
        
        if (minDistance <= unitRadius)
        {
            if (minDistance <= 0.1f)
                minDistance = 0.1f;

            return 0.5f * KR * Mathf.Pow((1.0f / minDistance - 1.0f / unitRadius), 2);
        }

        return 0.0f;
    }

    float[,] GetMotionMap()
    {
        float[,] motion =
        {
            {1, 0},
            {0, 1},
            {-1, 0},
            {0, -1},
            {-1, -1},
            {-1, 1},
            {1, -1},
            {1, 1}
        };

        return motion;
    }
    
    private void SimpleMovement()
    {
        float translationX = Input.GetAxis("Horizontal") * speed;
        float translationY = Input.GetAxis("Vertical") * speed;

        translationX *= Time.deltaTime;
        translationY *= Time.deltaTime;

        transform.Translate(translationX, translationY, 0);
    }

    private Vector3 FindPos(Vector3[] vectorArr, int mode)
    {
        Vector3 targetValue;
        switch (mode)
        {
            case 0:
                targetValue = Vector3.positiveInfinity; 
                
                foreach (var vector in vectorArr)
                {
                    if (vector.x <= targetValue.x && vector.y <= targetValue.y)
                        targetValue = vector;
                }

                return targetValue;
            
            case 1:
                targetValue = Vector3.negativeInfinity; 
                
                foreach (var vector in vectorArr)
                {
                    if (vector.x >= targetValue.x && vector.y >= targetValue.y)
                        targetValue = vector;
                }

                return targetValue;
        }
        
        throw new Exception("There is only 0 an 1 mode");
    }
}
