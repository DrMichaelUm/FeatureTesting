using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PotentialField : MonoBehaviour
{
    public float radius = 5f;
    [Range(1f, 10f)]
    public float KR = 5f;

    public List<Transform> obstacles;

    private float[] ox;
    private float[] oy;
    private float cashedPotential = 0f;
    // Start is called before the first frame update
    void Start()
    {
        ox = new float[obstacles.Count];
        oy = new float[obstacles.Count];
        int i = 0;
        foreach (var obstacle in obstacles)
        {
            ox[i] = obstacle.position.x;
            oy[i] = obstacle.position.y;
            i++;
        }
    }

    // Update is called once per frame
    void Update()
    {
        float potential = calculate_repulsive_potential(transform.position.x, transform.position.y, ox, oy, radius);
        if (cashedPotential != potential)
        {
            cashedPotential = potential;
            Debug.Log(potential);
        }
    }

    float calculate_repulsive_potential (float x, float y, float[] ox, float[] oy, float robot_rad)
    {
        // find a better/concise way to find this index
        int minind = -1;
        float dmin = float.MaxValue;

        for (int i = 0; i < ox.Length; i++)
        {
            float d = Mathf.Pow(x - ox[i], 2) + Mathf.Pow(y - oy[i], 2);

            if (d <= dmin)
            {
                dmin = d;
                minind = i;
            }
        }

        float dq = Mathf.Pow(x - ox[minind], 2) + Mathf.Pow(y - oy[minind], 2);

        if (dq <= robot_rad)
        {
            if (dq <= 0.1f)
                dq = 0.1f;

            return 0.5f * KR * Mathf.Pow((1.0f / dq - 1.0f / robot_rad), 2);
        }
        else
            return 0.0f;
    }
}
