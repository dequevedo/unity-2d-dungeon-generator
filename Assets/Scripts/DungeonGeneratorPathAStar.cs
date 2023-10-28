using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGeneratorPathAStar : MonoBehaviour
{
    static int maxIterations = 20000;

    public static void FindPath(Vector2Int start, Vector2Int goal, Tilemap tilemap, TileBase pathMarker, TileBase doorMarker)
    {
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var costSoFar = new Dictionary<Vector2Int, float>();
        var iterations = 0;

        var frontier = new PriorityQueue<Vector2Int>();
        frontier.Enqueue(start, 0);

        cameFrom[start] = start;
        costSoFar[start] = 0;

        while (frontier.Count > 0 && iterations < maxIterations)
        {
            var current = frontier.Dequeue();
            iterations++;

            if (current.Equals(goal))
            {
                ReconstructPath(cameFrom, current, tilemap, pathMarker);
                break;
            }

            foreach (var next in GetNeighbors(current))
            {
                var newCost = costSoFar[current] + 1;

                if (costSoFar.ContainsKey(next) && !(newCost < costSoFar[next])) continue;
                var nextTile = tilemap.GetTile(new Vector3Int(next.x, next.y, 0));

                if (nextTile != null && nextTile != doorMarker && nextTile != pathMarker) continue;
                costSoFar[next] = newCost;
                var priority = newCost + Heuristic(goal, next);
                frontier.Enqueue(next, priority);
                cameFrom[next] = current;
            }
        }
    }

    private static void ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current, Tilemap tilemap, TileBase pathMarker)
    {
        var path = new List<Vector2Int>();
        while (!cameFrom[current].Equals(current))
        {
            path.Add(current);
            current = cameFrom[current];
        }
        path.Reverse();

        for (var i = 0; i < path.Count - 1; i++)
        {
            tilemap.SetTile(new Vector3Int(path[i].x, path[i].y, 0), pathMarker);
        }
    }

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    private static List<Vector2Int> GetNeighbors(Vector2Int current)
    {
        return new List<Vector2Int>
        {
            new Vector2Int(current.x + 1, current.y),
            new Vector2Int(current.x - 1, current.y),
            new Vector2Int(current.x, current.y + 1),
            new Vector2Int(current.x, current.y - 1)
        };
    }
}

public class PriorityQueue<T>
{
    private List<KeyValuePair<T, float>> elements = new List<KeyValuePair<T, float>>();

    public int Count => elements.Count;

    public void Enqueue(T item, float priority)
    {
        elements.Add(new KeyValuePair<T, float>(item, priority));
    }

    public T Dequeue()
    {
        var bestIndex = 0;

        for (var i = 0; i < elements.Count; i++)
        {
            if (elements[i].Value < elements[bestIndex].Value)
            {
                bestIndex = i;
            }
        }

        var bestItem = elements[bestIndex].Key;
        elements.RemoveAt(bestIndex);
        return bestItem;
    }
}
