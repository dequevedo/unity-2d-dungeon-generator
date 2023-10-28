using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    public GameObject firstRoomPrefab;
    public List<GameObject> roomPrefabs;
    
    [Header("Generation Params")]
    public int numberOfRooms = 10;
    public float maxDistanceFromLastRoom = 10f;
    public int maxPlacementAttempts = 100;
    public int boundsSizeFactor = 2;
    public int roomMargins = 8;
    public bool squareConstrained = true;

    public Tilemap destinationTilemap;

    [Header("Empty Space Filler")]
    public TileBase fillerTile;
    public int margin = 5;
    
    [Header("Room Merger")]
    public List<Grid> sourceGrids;
    public float offsetMultiplier = .5f;

    [Header("Markers")]
    public TileBase doorMarker;
    public TileBase pathMarker;
    public TileBase floorMarker;

    private List<BoundsInt> roomBounds = new();
    private List<DungeonGeneratorRoom> spawnedRooms = new();
    private BoundsInt currentAttemptedBounds;
    private Vector3Int lastPosition = Vector3Int.zero;

    private void Start()
    {
        StartCoroutine(GenerateDungeon());
    }

    private IEnumerator GenerateDungeon()
    {
        for (var i = 0; i < numberOfRooms; i++)
        {
            // Select a random room Prefab
            var newRoomPrefab = i == 0 ? firstRoomPrefab : roomPrefabs[Random.Range(0, roomPrefabs.Count)];
            var newRoomTilemap = newRoomPrefab.GetComponent<DungeonGeneratorRoom>().GetTilemap();
            var newRoomSize = GetRoomSizeFromTilemap(newRoomTilemap);
            var newRoomBoundsCenterOffset = GetBoundsCenterOffsetFromTilemap(newRoomTilemap);

            // Find a spot in range and position the room
            if(i != 0) yield return StartCoroutine(FindRoomSpot(newRoomSize));
            var newRoomPosition = lastPosition - newRoomBoundsCenterOffset * boundsSizeFactor;
            var newRoomInstance = Instantiate(newRoomPrefab, newRoomPosition, Quaternion.identity);
            var newRoomInstanceComponent = newRoomInstance.GetComponent<DungeonGeneratorRoom>();
            sourceGrids.Add(newRoomInstance.GetComponent<Grid>());

            // Connects new room to previous spawned room
            if (i != 0) spawnedRooms[^1].roomConnections.Add(newRoomInstanceComponent);

            // Stores new room bounds to avoid future overlaps
            var newRoomBounds = new BoundsInt(lastPosition, newRoomSize);
            roomBounds.Add(newRoomBounds);
            
            // Stores new room
            spawnedRooms.Add(newRoomInstanceComponent);
        }
        
        MergeTilemaps();
        ConnectDoors();
        FillEmptyTiles();
        Cleaner();
        
        yield return new WaitForSeconds(5); //TODO mudar isso, talvez em pcs piores isso dê problema, talvez fazer uma coroutine no Cleaner
    }
    
    private void ConnectDoors()
    {
        if (roomBounds.Count < 2)
        {
            Debug.LogWarning("There isn`t enough rooms to connect.");
            return;
        }

        for (var i = 0; i < roomBounds.Count - 1; i++)
        {
            var firstRoom = roomBounds[i];
            var secondRoom = roomBounds[i + 1];
            
            var firstDoors = DungeonGeneratorUtils.FindDoorsInsideBounds(firstRoom, offsetMultiplier, destinationTilemap, doorMarker);
            var secondDoors = DungeonGeneratorUtils.FindDoorsInsideBounds(secondRoom, offsetMultiplier, destinationTilemap, doorMarker);

            if (firstDoors.Count == 0 || secondDoors.Count == 0)
            {
                Debug.Log("One of the rooms doesn't have any doors.");
                continue;
            }

            //TODO ADICIONAR LOGICA PARA ESCOLHER O LADO DA PORTA
            var firstDoor = firstDoors[Random.Range(0, firstDoors.Count)];
            var secondDoor = secondDoors[Random.Range(0, secondDoors.Count)];

            DungeonGeneratorPathAStar.FindPath(firstDoor, secondDoor, destinationTilemap, pathMarker, doorMarker);
        }
    }

    private Vector3Int GetRoomSizeFromTilemap(Tilemap tilemap)
    {
        tilemap.CompressBounds();
        return new Vector3Int(Mathf.RoundToInt(
            tilemap.size.x * boundsSizeFactor) + roomMargins, 
            Mathf.RoundToInt(tilemap.size.y * boundsSizeFactor) + roomMargins, 
            0);
    }
    
    private Vector3 GetBoundsCenterOffsetFromTilemap(Tilemap tilemap)
    {
        var bounds = tilemap.cellBounds;
        var cellCenter = new Vector3Int(
            bounds.xMin - (roomMargins/4), 
            bounds.yMin - (roomMargins/4), 
            0);
        return cellCenter;
    }

    private IEnumerator FindRoomSpot(Vector3Int roomSize)
    {
        var attempts = 0;
        
        while (attempts < maxPlacementAttempts)
        {
            var offset = GetOffset(roomSize);
            var potentialPosition = DungeonGeneratorUtils.MakeEven(lastPosition + offset);
            var newRoomBounds = new BoundsInt(potentialPosition, roomSize);
            var positionValid = DungeonGeneratorUtils.ValidatePosition(newRoomBounds, roomBounds);

            yield return new WaitForSeconds(.1f); //TODO REMOVER

            if (positionValid)
            {
                lastPosition = potentialPosition;
                break;
            }

            attempts++;
        }

        if (attempts >= maxPlacementAttempts)
        {
            Debug.LogWarning("Max attempts exceeded. Placing room in the last successful position.");
        }
    }

    private Vector4 GetTilemapExtents()
    {
        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        foreach (Vector3Int pos in destinationTilemap.cellBounds.allPositionsWithin)
        {
            var tile = destinationTilemap.GetTile(pos);
            if (tile == null) continue;
            if (pos.x < minX) minX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y > maxY) maxY = pos.y;
        }

        minX -= margin;
        minY -= margin;
        maxX += margin;
        maxY += margin;

        return new Vector4(minX, minY, maxX, maxY);
    }

    private void FillEmptyTiles()
    {
        var extents = GetTilemapExtents();
        var minX = (int)extents.x;
        var minY = (int)extents.y;
        var maxX = (int)extents.z;
        var maxY = (int)extents.w;

        for (var x = minX; x <= maxX; x++)
        {
            for (var y = minY; y <= maxY; y++)
            {
                var pos = new Vector3Int(x, y, 0);
                if (destinationTilemap.GetTile(pos) == null)
                {
                    destinationTilemap.SetTile(pos, fillerTile);
                }
            }
        }
    }
    
    private void Cleaner()
    {
        var bounds = destinationTilemap.cellBounds;
        for (var x = bounds.xMin; x < bounds.xMax; x++)
        {
            for (var y = bounds.yMin; y < bounds.yMax; y++)
            {
                var tilePosition = new Vector3Int(x, y, 0);
                var tileAtPosition = destinationTilemap.GetTile(tilePosition);
                
                if (tileAtPosition == doorMarker || tileAtPosition == pathMarker || tileAtPosition == floorMarker)
                {
                    destinationTilemap.SetTile(tilePosition, null);
                }
            }
        }
    }

    private void MergeTilemaps()
    {
        foreach (var sourceGrid in sourceGrids)
        {
            var sourceTilemap = sourceGrid.GetComponentInChildren<Tilemap>();
            var bounds = sourceTilemap.cellBounds;
            var allTiles = sourceTilemap.GetTilesBlock(bounds);
            var tempOffset = sourceGrid.transform.position * offsetMultiplier;
            var offset = new Vector3Int(Mathf.FloorToInt(tempOffset.x), Mathf.FloorToInt(tempOffset.y), Mathf.FloorToInt(tempOffset.z));

            for (var x = 0; x < bounds.size.x; x++)
            {
                for (var y = 0; y < bounds.size.y; y++)
                {
                    var tile = allTiles[x + y * bounds.size.x];
                    if (tile == null) continue;
                    var destinationPos = new Vector3Int(x + bounds.x + offset.x, y + bounds.y + offset.y, 0);
                    destinationTilemap.SetTile(destinationPos, tile);
                }
            }
            
            var parentTransform = sourceGrid.transform;
            var childTransform = parentTransform.Find("Tilemap");

            if (childTransform != null) 
            {
                Destroy(childTransform.gameObject);
            }
            else
            {
                Debug.LogWarning("Tilemap GameObject not found.");
            }

        }
    }

    private Vector3Int GetOffset(Vector3Int roomSize)
    {
        Vector3Int[] possibleDirections = {
            new (0, roomSize.y, 0),
            new (0, -roomSize.y, 0),
            new (roomSize.x, 0, 0),
            new (-roomSize.x, 0, 0)
        };
        
        if (squareConstrained)
        {
            var randomIndex = Random.Range(0, possibleDirections.Length);
            return possibleDirections[randomIndex];
        }
        
        var randomDirection = Random.insideUnitCircle * maxDistanceFromLastRoom;
        return new Vector3Int(Mathf.RoundToInt(randomDirection.x * roomSize.x), Mathf.RoundToInt(randomDirection.y * roomSize.y), 0);
    }
    
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        foreach (var bound in roomBounds)
        {
            Gizmos.DrawWireCube(bound.center, bound.size);
        }
    }

}
