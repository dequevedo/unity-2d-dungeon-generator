using UnityEngine;
using UnityEngine.Tilemaps;

public class RoadGenerator : MonoBehaviour
{
    public Tilemap tilemap;
    public TileBase roadTile;
    public TileBase endRoadTile;
    public float chanceToTurn = 0.035f;
    public float chanceToBranch = 0.02f;
    public int roadLength = 300;
    public int maxBranchLength = 10;
    private Vector3Int currentPosition = Vector3Int.zero;

    private void Start()
    {
        GenerateRoads();
    }

    private void GenerateRoads()
    {
        tilemap.SetTile(currentPosition, roadTile);
        var currentRoadLength = 0;
        var direction = Vector3Int.right;

        while (currentRoadLength < roadLength)
        {
            var randomTurn = Random.Range(0f, 1f);

            if (randomTurn < chanceToTurn)
            {
                direction = Turn(direction);
            }

            var randomBranch = Random.Range(0f, 1f);

            if (randomBranch < chanceToBranch)
            {
                GenerateBranch(currentPosition, direction);
            }

            currentPosition += direction;
            tilemap.SetTile(currentPosition, roadTile);

            currentRoadLength++;
        }

        tilemap.SetTile(currentPosition, endRoadTile);
    }

    private void GenerateBranch(Vector3Int startPosition, Vector3Int startDirection)
    {
       var branchPosition = startPosition;
       var branchDirection = Turn(startDirection);
       var branchLength = Random.Range(1, maxBranchLength);

        for (int i = 0; i < branchLength; i++)
        {
            branchPosition += branchDirection;
            tilemap.SetTile(branchPosition, roadTile);

            float randomTurn = Random.Range(0f, 1f);

            if (randomTurn < chanceToTurn)
            {
                branchDirection = Turn(branchDirection);
            }
        }

        tilemap.SetTile(branchPosition, endRoadTile);
    }

    private Vector3Int Turn(Vector3Int currentDirection)
    {
        var randomDirection = Random.Range(0f, 1f);

        if (currentDirection == Vector3Int.right)
        {
            return randomDirection < 0.5f ? Vector3Int.up : Vector3Int.down;
        }
        if (currentDirection == Vector3Int.left)
        {
            return randomDirection < 0.5f ? Vector3Int.up : Vector3Int.down;
        }
        if (currentDirection == Vector3Int.up)
        {
            return randomDirection < 0.5f ? Vector3Int.left : Vector3Int.right;
        }
        
        return randomDirection < 0.5f ? Vector3Int.left : Vector3Int.right;
    }
}
