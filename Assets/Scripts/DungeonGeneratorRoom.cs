using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGeneratorRoom : MonoBehaviour
{
    public List<DungeonGeneratorRoom> roomConnections;

    public Tilemap GetTilemap() //Da pra melhorar essa performance
    {
        return GetComponentInChildren<Tilemap>();
    }
}
