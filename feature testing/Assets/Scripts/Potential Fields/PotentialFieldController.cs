using System;
using System.Collections.Generic;
using UnityEngine;

public static class PotentialFieldController
{
	const float k_REPULSIVE_COEF = 100f;

	static readonly float[,] k_MotionMap =
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

	public static (Vector3 potentialDirection, float currentPotentialValue) CalculateOptimalPotentialVector 
	(
		Vector3 currentPos, 
		float[,] potentialMap, 
		float minX, 
		float minY, 
		float res
	)
	{
		// Debug.Log("LX " + potentialMap.GetLength(0) + "; LY " + potentialMap.GetLength(1) + 
		//           "; PosX " + currentPos.x + "; PosY " + currentPos.z +
		//           "; MinX " + minX + "; MinY " + minY);
		Vector3 currentPosRes = new Vector3();
		currentPosRes.x = (currentPos.x - minX) / res;
		currentPosRes.z = (currentPos.z - minY) / res;
		int potentialRows = potentialMap.GetLength(0);
		int potentialColumns = potentialMap.GetLength(1);
		int motionMapRows = k_MotionMap.GetLength(0);

		float currentPosPotential = 0f;

		if ((uint)currentPosRes.x < potentialRows && (uint)currentPosRes.z < potentialColumns)
		{
			currentPosPotential = potentialMap[(uint) currentPosRes.x, (uint) currentPosRes.z];
			
			if (potentialMap[(uint)currentPosRes.x, (uint)currentPosRes.z] == 0)
				return (currentPos, 0);
		}
		
		

		float minPotential = float.MaxValue;
		float minMovedX = -1, minMovedY = -1;

		for (int i = 0; i < motionMapRows; i++)
		{
			uint movedX = (uint)(currentPosRes.x + k_MotionMap[i,0]);
			uint movedY = (uint)(currentPosRes.z + k_MotionMap[i,1]);
			float currentPotential = float.MaxValue;
            
			//Debug.Log("LX " + potentialMap.GetLength(0) + "; LY " + potentialMap.GetLength(1) + "; IndX " + movedX + "; IndY " + movedY);
			if (movedX < potentialRows && movedY < potentialColumns)
				currentPotential = potentialMap[movedX, movedY];

			if (currentPotential < minPotential)
			{
				minPotential = currentPotential;
				minMovedX = movedX;
				minMovedY = movedY;
			}
		}
			
		if (minMovedX == -1)
		{
			return (currentPos, 0);
		}

		return (new Vector3(minMovedX * res + minX, 0, minMovedY * res + minY), currentPosPotential);
	}

	public static float[,] GetPotentialMap
	(
		List<Collider> obstacles,
		uint areaWidth,
		uint areaHeight,
		Vector3 areaPos,
		float res,
		float unitRadius
	)
	{
		Vector3[] obsPos = GetObstacleCellsPos(obstacles);

		(float minX, float minY, uint areaWidthRes, uint areaHeightRes) =
			CalculateAreaParams(areaPos, areaWidth, areaHeight, res);

		return CalculatePotentialMap(obsPos, areaWidthRes, areaHeightRes, minX, minY, res, unitRadius);
	}

	public static float[,] GetPotentialMap
	(
		List<Collider> obstacles,
		float minX,
		float minY,
		uint areaWidth,
		uint areaHeight,
		float res,
		float unitRadius
	)
	{
		Vector3[] obsPos = GetObstacleCellsPos(obstacles);

		return CalculatePotentialMap(obsPos, areaWidth, areaHeight, minX, minY, res, unitRadius);
	}

	public static /*float[,]*/ void GetPotentialMap
	(
		in float[,] potentialMap,
		Vector3[] obsPos,
		float minX,
		float minY,
		uint areaWidth,
		uint areaHeight,
		float res,
		float unitRadius
	)
	{
		CalculatePotentialMap(in potentialMap, obsPos, areaWidth, areaHeight, minX, minY, res, unitRadius);
	}

	public static Vector3[] GetObstacleCellsPos (List<Collider> obstacles)
	{
		List<Vector3> obsPosList = new List<Vector3>();
		float count = 0;

		foreach (var obstacle in obstacles)
		{
			var bounds = obstacle.bounds;
			float xMinBound = bounds.min.x;
			float zMinBound = bounds.min.z;
			int xLength = (int)Mathf.Floor(bounds.max.x - xMinBound);
			int zLength = (int)Mathf.Floor(bounds.max.z - zMinBound);

			CountBounds();

			if (xLength >= 2 && zLength >= 2)
			{
				xLength -= 2;
				zLength -= 2;
				CountBounds();
			}


			void CountBounds()
			{
				if (xLength == 0 && zLength == 0)
					count += 1;
				else
					count += (xLength + zLength) * 2;
			}
			//Debug.Log("XLength: " + xLength + "; zLength: " + zLength + "; Count: " + count);
		}


		Vector3[] obsPos = new Vector3[(int) count];

		if (count > 0)
		{
			int index = 0;

			foreach (var obstacle in obstacles)
			{
				var bounds = obstacle.bounds;
				float xMinBound = bounds.min.x;
				float zMinBound = bounds.min.z;
				int xLength = (int)Mathf.Floor(bounds.max.x - xMinBound);
				int zLength = (int)Mathf.Floor(bounds.max.z - zMinBound);

				for (int i = 0; i <= xLength; i++)
				{
					float x = xMinBound + i;

					//Debug.Log("-----X: " + x + "-----------");
					for (int j = 0; j <= zLength; j++)
					{
						float z = zMinBound + j;

						if (i == 0 || i == xLength || (xLength > 1 && (i == 1 || i == xLength - 1))  || j == 0 || j == zLength || (zLength > 1 && (j == 1 || j == zLength - 1)) ) 
						{
							//Debug.Log("x " + x + "; y " + y);
							obsPosList.Add(new Vector3(x, 0, z));

							try
							{
								obsPos[index] = new Vector3(x, 0, z);
								index++;
							}
							catch (Exception ex)
							{
								Debug.LogError(ex.Message);

								Debug.Log("Length: " + obsPosList.Count + "; Index: " + index + "; Count: " + count +
								          "; ObsCount: " + obstacles.Count);
							}
						}
					}
				}
			}

			//Debug.Log("Index: " + index + "; Count: " + obsPos.Length + "; ObsCount " + obstacles.Count);
		}

		return obsPos;
	}

	static (float minX, float minY, uint areaWidth, uint areaHeight) CalculateAreaParams
	(
		Vector3 initPos,
		float areaWidth,
		float areaHeight,
		float res
	)
	{
		float leftAreaBound = initPos.x - areaWidth / 2f;
		float bottomAreaBound = initPos.y - areaHeight / 2f;
		uint areaWidthRes = (uint) Mathf.Round(areaWidth / res);
		uint areaHeightRes = (uint) Mathf.Round(areaHeight / res);

		//print("aw " + areaWidth + "; ah " + areaHeight + "; lb " + leftAreaBound + "; bb " + bottomAreaBound);

		return (leftAreaBound, bottomAreaBound, areaWidthRes, areaHeightRes);
	}

	static /*float[,]*/ void CalculatePotentialMap
	(
		in float[,] potentialMap,
		Vector3[] obsPos,
		uint areaWidthRes,
		uint areaHeightRes,
		float minX,
		float minY,
		float res,
		float unitRadius
	)
	{
		//float[,] potentialMap = new float[areaWidthRes, areaHeightRes];

		for (int i = 0; i < areaWidthRes; i++)
		{
			float x = i * res + minX;

			for (int j = 0; j < areaHeightRes; j++)
			{
				float y = j * res + minY;

				float uAttractionPotential = 0; //0.5 * KP * distanceToGoal
				float uRepulsionPotential = CalculateRepulsivePotential(x, y, obsPos, unitRadius);
				float uForce = uAttractionPotential + uRepulsionPotential;

				potentialMap[i, j] = uForce;
			}
		}

		//return potentialMap;
	}

	static float[,] CalculatePotentialMap
	(
		Vector3[] obsPos,
		uint areaWidthRes,
		uint areaHeightRes,
		float minX,
		float minY,
		float res,
		float unitRadius
	)
	{
		float[,] potentialMap = new float[areaWidthRes, areaHeightRes];

		for (int i = 0; i < areaWidthRes; i++)
		{
			float x = i * res + minX;

			for (int j = 0; j < areaHeightRes; j++)
			{
				float y = j * res + minY;

				float uAttractionPotential = 0; //0.5 * KP * distanceToGoal
				float uRepulsionPotential = CalculateRepulsivePotential(x, y, obsPos, unitRadius);
				float uForce = uAttractionPotential + uRepulsionPotential;

				potentialMap[i, j] = uForce;
			}
		}

		return potentialMap;
	}

	static float CalculateRepulsivePotential (float x, float y, Vector3[] obsPos, float unitRadius)
	{
		float minDistance = float.MaxValue;
		for (int i = 0; i < obsPos.Length; i++)
		{
			//float dist = Mathf.Pow(x - obsPos[i].x, 2) + Mathf.Pow(y - obsPos[i].z, 2);
			float dist = (x - obsPos[i].x) * (x - obsPos[i].x) + (y - obsPos[i].z) * (y - obsPos[i].z);
			//Debug.Log("X " + x + "; Y " + y + "; ObsX " + obsPos[i].x + "; ObsY " + obsPos[i].z); 

			if (dist <= minDistance)
			{
				minDistance = dist;
			}
		}

		minDistance = Mathf.Sqrt(minDistance);
		//Debug.Log("MIN DIST " + minDistance);

		if (minDistance <= unitRadius)
		{
			if (minDistance <= 0.1f)
				minDistance = 0.1f;

			//Debug.Log("POTENTIAL ");
			float dDependency = 1.0f / minDistance - 1.0f / unitRadius;

			return 0.5f * k_REPULSIVE_COEF * dDependency * dDependency;
		}

		return 0.0f;
	}
}