using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGeneratorUtils
{
    public static Vector2Int GetRoomCenter(Rect room)
    {
        return new Vector2Int(
            Mathf.RoundToInt(room.x + room.width * 0.5f),
            Mathf.RoundToInt(room.y + room.height * 0.5f)
        );
    }
    
    public static bool IsOverlapping(Rect newRoom, List<Rect> rooms)
    {
        foreach (Rect room in rooms)
        {
            if (room.Overlaps(newRoom))
            {
                return true;
            }
        }
        return false;
    }
    
    public static bool Intersects(BoundsInt a, BoundsInt b)
    {
        return a.xMin < b.xMax && a.xMax > b.xMin &&
               a.yMin < b.yMax && a.yMax > b.yMin;
    }
    
    public static Vector3Int MakeEven(Vector3Int vec)
    {
        vec.x = MakeCoordinateEven(vec.x);
        vec.y = MakeCoordinateEven(vec.y);
        vec.z = MakeCoordinateEven(vec.z);
        return vec;
    }
    
    public static bool ValidatePosition(BoundsInt newRoomBounds, List<BoundsInt> roomBounds)
    {
        foreach (var existingBounds in roomBounds)
        {
            if (DungeonGeneratorUtils.Intersects(newRoomBounds, existingBounds))
            {
                return false;
            }
        }
        return true;
    }
    
    public static Vector2Int FindRandomDoor(Tilemap tilemap, TileBase targetTile)
    {
        var foundPositions = new List<Vector2Int>();
        var bounds = tilemap.cellBounds;
        var allTiles = tilemap.GetTilesBlock(bounds);

        for (var x = 0; x < bounds.size.x; x++)
        {
            for (var y = 0; y < bounds.size.y; y++)
            {
                var tile = allTiles[x + y * bounds.size.x];
                if (tile == targetTile)
                {
                    // Armazena a posição da célula onde o tile foi encontrado
                    var cellPosition = new Vector2Int(x + bounds.x, y + bounds.y);
                    foundPositions.Add(cellPosition);
                }
            }
        }

        if (foundPositions.Count == 0)
        {
            // Nenhum Tile encontrado
            return new Vector2Int(-1, -1); //TODO melhorar isso pra evitar bug
        }

        // Escolhe uma posição aleatória da lista
        int randomIndex = Random.Range(0, foundPositions.Count);
        return foundPositions[randomIndex];
    }
    
    public static List<Vector2Int> FindDoorsInsideBounds(BoundsInt roomBounds, float offsetMultiplier, Tilemap tilemap, TileBase doorMarker)
    {
        var doors = new List<Vector2Int>();

        for (var x = (int)(roomBounds.xMin * offsetMultiplier); x < (int)(roomBounds.xMax * offsetMultiplier); x++)
        {
            for (var y = (int)(roomBounds.yMin * offsetMultiplier); y < (int)(roomBounds.yMax * offsetMultiplier); y++)
            {
                var position3d = new Vector3Int(x, y, 0);
                var actualTile = tilemap.GetTile(position3d);

                if (actualTile == doorMarker)
                {
                    var position2d = new Vector2Int(x, y);
                    doors.Add(position2d);
                }
            }
        }

        return doors;
    }

    private static int MakeCoordinateEven(int coordinate)
    {
        if (coordinate % 2 != 0)
        {
            coordinate++;
        }
        return coordinate;
    }
}