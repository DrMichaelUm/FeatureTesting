using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PotentialFieldControllerTest
{
	public static float Resolution => 1f;

	public static readonly uint s_ChunkDemX = 20;
	public static readonly uint s_ChunkDemY = 20;
	public static uint AreaWidth => (uint) (s_ChunkDemX / Resolution);
	public static uint AreaHeight => (uint) (s_ChunkDemY / Resolution);

	const float k_RepulsiveCoefficient = 100f;

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

	readonly struct BoundsData
	{
		public readonly float XMinBound;
		public readonly float ZMinBound;

		public readonly int XLength;
		public readonly int ZLength;

		public BoundsData (Bounds bounds)
		{
			XMinBound = bounds.min.x;
			ZMinBound = bounds.min.z;
			XLength = (int) Mathf.Floor(bounds.max.x - XMinBound);
			ZLength = (int) Mathf.Floor(bounds.max.z - ZMinBound);
		}
	}

	public static Vector3[] GetObstacleCellsPos (IEnumerable<Collider> obstacles)
	{
		var count = 0;

		var boundsDatas =
			obstacles.Select(obstacle => new BoundsData(obstacle.bounds)).ToList();

		foreach (var boundsData in boundsDatas)
		{
			if (boundsData.XLength == 0 && boundsData.ZLength == 0)
				count += 1;
			else
				count += (boundsData.XLength + boundsData.ZLength) * 2;
		}

		var obsPos = new Vector3[count];

		if (count > 0)
		{
			var index = 0;

			foreach (var boundsData in boundsDatas)
			{
				for (var i = 0; i != boundsData.XLength + 1; ++i)
				{
					var x = boundsData.XMinBound + i;

					//Debug.Log("-----X: " + x + "-----------");
					for (var j = 0; j != boundsData.ZLength + 1; ++j)
					{
						var z = boundsData.ZMinBound + j;

						if (i == 0 || i == boundsData.XLength || j == 0 || j == boundsData.ZLength)
						{
							obsPos[index] = new Vector3(x, 0, z);
							index++;
						}
					}
				}
			}
		}

		return obsPos;
	}

	public static (Vector3 optimalPoint, float potentialValue) CalculateOptimalPotentialVector
	(
		Vector3 currentPos,
		float[,] potentialMap,
		float minX,
		float minY
	)
	{
		var currentPosRes = PositionToPotentialMapPos(currentPos, minX, minY);
		var potentialRows = potentialMap.GetLength(0);
		var potentialColumns = potentialMap.GetLength(1);
		var motionMapRows = k_MotionMap.GetLength(0);

		var currentPosPotential = 0f;

		if ((uint) currentPosRes.x < potentialRows && (uint) currentPosRes.z < potentialColumns)
		{
			currentPosPotential = potentialMap[(uint) currentPosRes.x, (uint) currentPosRes.z];

			if (currentPosPotential == 0)
				return (currentPos, 0);
		}

		var minPotential = float.MaxValue;
		var minMovedX = -1;
		var minMovedY = -1;

		for (var i = 0; i != motionMapRows; ++i)
		{
			var movedX = (int) (currentPosRes.x + k_MotionMap[i, 0]);
			var movedY = (int) (currentPosRes.z + k_MotionMap[i, 1]);
			var currentPotential = float.MaxValue;

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

		return minMovedX == -1
			       ? (currentPos, 0)
			       : (PotentialMapPosToPosition(minMovedX, minMovedY, minX, minY), currentPosPotential);
	}

	public static float[,] GetPotentialMap
	(
		Vector3[] obsPos,
		float minX,
		float minY,
		float unitRadius
	)
	{
		float[,] potentialMap = new float[AreaWidth, AreaHeight];
		CalculatePotentialMap(in potentialMap, obsPos, AreaWidth, AreaHeight, minX, minY, Resolution, unitRadius);

		return potentialMap;
	}

	static void CalculatePotentialMap
	(
		in float[,] potentialMap,
		IList<Vector3> obsPos,
		uint areaWidthRes,
		uint areaHeightRes,
		float minX,
		float minY,
		float res,
		float unitRadius
	)
	{
		for (var i = 0; i != areaWidthRes; ++i)
		{
			var x = i * res + minX;

			for (var j = 0; j != areaHeightRes; ++j)
			{
				var y = j * res + minY;

				// In case we want to add some kind of goal for unit
				const float uAttractionPotential = 0f; //0.5 * KP * distanceToGoal 

				var uRepulsionPotential = CalculateRepulsivePotential(x, y, obsPos, unitRadius);
				var uForce = uAttractionPotential + uRepulsionPotential;

				potentialMap[i, j] = uForce;
			}
		}
	}

	static float CalculateRepulsivePotential (float x, float y, IList<Vector3> obsPos, float unitRadius)
	{
		var minDistance = float.MaxValue;

		for (var i = 0; i != obsPos.Count; ++i)
		{
			//float dist = Mathf.Pow(x - obsPos[i].x, 2) + Mathf.Pow(y - obsPos[i].z, 2);
			var dist = (x - obsPos[i].x) * (x - obsPos[i].x) + (y - obsPos[i].z) * (y - obsPos[i].z);
			//Debug.Log("X " + x + "; Y " + y + "; ObsX " + obsPos[i].x + "; ObsY " + obsPos[i].z); 

			if (dist <= minDistance)
				minDistance = dist;
		}

		if (minDistance <= unitRadius * unitRadius)
		{
			minDistance = Mathf.Sqrt(minDistance);

			if (minDistance <= 0.1f)
				minDistance = 0.1f;

			var dDependency = 1f / minDistance - 1f / unitRadius;

			return 0.5f * k_RepulsiveCoefficient * dDependency * dDependency;
		}

		return 0.0f;
	}

	public static Vector3 PositionToPotentialMapPos (Vector3 position, float leftBound, float bottomBound) =>
		new Vector3 {x = (position.x - leftBound) / Resolution, z = (position.z - bottomBound) / Resolution};

	public static Vector3 PositionToPotentialMapPos (float xPos, float yPos, float leftBound, float bottomBound) =>
		new Vector3 {x = (xPos - leftBound) / Resolution, z = (yPos - bottomBound) / Resolution};

	public static Vector3 PotentialMapPosToPosition (Vector3 potentialMapPos, float leftBound, float bottomBound) =>
		new Vector3 {x = potentialMapPos.x * Resolution + leftBound, z = potentialMapPos.z * Resolution + bottomBound};

	public static Vector3 PotentialMapPosToPosition (float xPos, float yPos, float leftBound, float bottomBound) =>
		new Vector3 {x = xPos * Resolution + leftBound, z = yPos * Resolution + bottomBound};
}