using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class ProceduralGeneration
{
    public static List<Vector2> generatePoints(float radius, Vector2 regionSize, int attemptsToPlace= 25)
    {
        float cellSize = radius / Mathf.Sqrt(2);

        //A grid of the area to generate points
        //Value of each index of the grid is 1+(the index of the point), and 0 if there is no point in that index
        int[,] grid = new int[Mathf.CeilToInt(regionSize.x / cellSize), Mathf.CeilToInt(regionSize.y / cellSize)];
        //Stores all the generated points
        List<Vector2> points = new List<Vector2>();
        //Used to generate new points. Spawnpoints get deleted when no suitable points can be generated around them.
        List<Vector2> spawnPoints = new List<Vector2>();

        //Starting point
        spawnPoints.Add(regionSize / 2);

        while (spawnPoints.Count > 0)
        {
            //Grabbing a random spawnable point to generate a new point around
            int spawnIndex = Random.Range(0, spawnPoints.Count);
            Vector2 center = spawnPoints[spawnIndex];
            bool foundValidPoint = false;
            for(int i = 0; i<attemptsToPlace; i++)
            {
                float angle = Random.value * Mathf.PI * 2;
                Vector2 direction = new Vector2(Mathf.Sin(angle), Mathf.Cos(angle));
                Vector2 newPoint = center + direction*Random.Range(radius, radius * 2);

                //If the generated point is in a valid location, add it's information to each list
                if (isValid(newPoint, grid, regionSize, cellSize, radius, points))
                {
                    foundValidPoint = true;
                    spawnPoints.Add(newPoint);
                    points.Add(newPoint);
                    grid[(int)(newPoint.x / cellSize), (int)(newPoint.y / cellSize)] = spawnPoints.IndexOf(newPoint) + 1;
                    break;
                }
            }
            //Removing the points ability to spawn any other points if it couldn't create a new point
            if (!foundValidPoint)
            {
                spawnPoints.RemoveAt(spawnIndex);
            }
        }
        return points;
    }

    static bool isValid(Vector2 point, int[,] grid, Vector2 regionSize, float cellSize, float radius, List<Vector2> points)
    {
        if(point.x >=0 && point.x<regionSize.x && point.y >= 0 && point.y < regionSize.y)
        {
            //Only return true if the point meets of the following condition:
            //No point in a 5x5 grid around the current point is closer than 1 radius away from the current point
            int xLoc = (int)(point.x / cellSize);
            int yLoc = (int)(point.x / cellSize);
            int xStartSearch = Mathf.Max(0, xLoc - 2);
            int xEndSearch = Mathf.Min(xLoc + 2, grid.GetLength(0)-1);
            int yStartSearch = Mathf.Max(0, yLoc - 2);
            int yEndSearch = Mathf.Min(yLoc + 2, grid.GetLength(1) - 1);
            for(int i = xStartSearch; i<=xEndSearch; i++)
            {
                for(int j = yStartSearch; j<=yEndSearch; j++)
                {
                    int pointIndex = grid[i, j] - 1;
                    if (pointIndex != -1)
                    {
                        float sqDist = (point - points[pointIndex]).sqrMagnitude;
                        if(sqDist < radius * radius)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        return false;
    }
}
