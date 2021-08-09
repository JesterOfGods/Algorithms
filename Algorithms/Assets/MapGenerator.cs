// Cellular Automata Cave Generation Using Conway's Game of life rules and Flood fill algorithm to make sure every room is connected, there are no pillars(no one tile walls) and there are no inaccessable parts of the map. 



using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator : MonoBehaviour
{


    //dimentions of the map in tiles
    public int width=128;
    public int height=72;
    //Reprecentation of the map in a table. X=width,Y=height. we are creating a 2d map, or a top down view of the map if you like. 
    int[,] map;

    //Seed for the randomization. This is done in order to be able to compair the results in different numbers of eterations.
    public string seed="0";
    //In case we want a different number each time and we dont care about compairing the results. By default this is off
    public bool useRandomSeed =true;

    //How many walls cover the map generated at random before we aply the algorithm.(0-100)%
    [Range(0, 100)]
    public int randomFillPercentage=47;
    //How many eterations of the algorith are we going to apply. (We could clamp this to make sure it doesnt go for too long and to make
    //it produces some interesting results.
    [Min (2)]
    public int smoothAmount = 5;

    //How thick of a border we want.
    [Min(1)]
    public int borderSize=1;
    //what is the lowest number of tiles a wall can be
    public int WallThicknessLowerThreshold = 3;
    //what is the lowest number of tiles a room can be
    public int RoomThreshold = 10;

    [Min(1)]
    public int corridorRadious = 1;
    void Start()
    {
        GenerateMap();
    }
    
    void GenerateMap()
    {
        map = new int[width, height];
        //intitialize a random map based on a seed.
        RandomFillMap();
        //Itterations of the algorithm, how many times are we going to loop using Cellular Automata algorithm+ based on Conway's game of life rules
        for (int i = 0; i < smoothAmount; i++)
        {
            SmoothMap();
        }
        //remove walls based on threshold
        RemoveSmallWalls();
        //remove rooms based on thershold
        RemoveSmallRooms();
        // make a border around map
        map = MakeBorder();

    }

    //Make a pseudorandom table of ones and zeros based on a seed.
    //A one represents a wall, a zero represents a floor
    void RandomFillMap()
    {
        if (useRandomSeed)
        {
            seed = System.DateTime.Now.ToString();
            
        }
        Debug.Log(seed);
        System.Random pseudoRandom = new System.Random(seed.GetHashCode());
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (x == 0 || x == width - 1 || y == height - 1 || y == 0)
                {
                    map[x, y] = 1;
                }
                else
                {
                    map[x, y] = (pseudoRandom.Next(0, 100) < randomFillPercentage) ? 1 : 0;
                }
            }
        }
    }

    //Cellular Automata algorithm based on Conway's game of life rules.
    //For each eteration we take each cell in the table and look it's surrounding tiles in all directions including diagonals.
    //If the tile has more than four walls around it it becomes a wall
    //If it has less than four it becomes a floor
    //If it has exactly four it remains the same as it was. 
    //This is a diversion from the Conway's game of life rules as we keep the equals to four the same in order to produse more interesting results
    void SmoothMap()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int neighbourWallTiles = GetWallNumberAroundTile(x,y);
                if (neighbourWallTiles > 4)
                {
                    map[x, y] = 1;
                }
                else if (neighbourWallTiles < 4)
                {
                    map[x,y] = 0;
                }
            }
        }

    }

    //Helping function that counts the number of walls around a tile.
    int GetWallNumberAroundTile(int GridX,int GridY)
    {
        int wallCount = 0;
        for (int neighbourX = GridX - 1; neighbourX <= GridX + 1; neighbourX++)
        {
            for (int neighbourY = GridY - 1; neighbourY <= GridY + 1; neighbourY++)
            {
                //We use this to make sure the tile is within bounds.
                if (IsInsideMap(neighbourX,neighbourY))
                {
                    if (neighbourX != GridX || neighbourY != GridY)
                    {
                        wallCount += map[neighbourX, neighbourY];
                    }
                }
                //we add walls to the perimeter in order to promote the algorithm to produce walls in the external bounds of the map.
                else
                {
                    wallCount++;
                }
            }
        }
        return wallCount;
    }

    //Helping Function to determind if coordinates are within the bounds of map, valid tile location
    bool IsInsideMap(int x, int y)
    {
        return x >= 0 && x < width && y >= 0 && y < height;
    }


    //Function to create a border around the map, Making sure the whole map is surrounded by walls.
    int[,] MakeBorder()
    {
        int[,] borderedMap = new int[width , height];
        for (int x = 0; x < borderedMap.GetLength(0); x++)
        {
            for (int y = 0; y < borderedMap.GetLength(1); y++)
            {
                if (x >= borderSize && x < width - borderSize && y >= borderSize && y < height - borderSize)
                {
                    borderedMap[x, y] = map[x, y];

                }
                else
                {
                    borderedMap[x, y] = 1;
                }
            }
        }
        return borderedMap;
    }

    // We use a flood fill algorithm to find all rooms created by the Cellular Automata Algorithm

    //In order to do that we need to represent the tiles in some way, so a struck will hold the x and y values
    //of the tile on the map representation we have already made.
    struct Coord
    {
        public int tileX;
        public int tileY;

        public Coord(int x, int y)
        {
            tileX = x;
            tileY = y;
        }
    }

    //eterate on all regions of walls and find walls too small. then remove them bymaking the region be all floor tiles
    void RemoveSmallWalls()
    {
        List<List<Coord>> wallRegions = GetRegions(1);
        foreach (List<Coord> wallRegion in wallRegions)
        {
            if (wallRegion.Count < WallThicknessLowerThreshold)
            {
                foreach (Coord tile in wallRegion)
                {
                    map[tile.tileX, tile.tileY] = 0;
                }
            }
        }
    }
    //eterate on all regions of floors and find rooms too small. then remove them by making the region be all wall tiles
    void RemoveSmallRooms()
    {
        List<List<Coord>> roomRegions = GetRegions(0);

        //Since we have removed the rooms that are too small, we also keep track of the rooms that remained.
        //This allows us to connect the room regions later, without having to reeterate on the map again.
        List<Room> roomsThatRemain = new List<Room>();
        foreach (List<Coord> roomRegion in roomRegions)
        {
            if (roomRegion.Count < RoomThreshold)
            {
                foreach (Coord tile in roomRegion)
                {
                    map[tile.tileX, tile.tileY] = 1;
                }
            }
            else
            {
                roomsThatRemain.Add(new Room(roomRegion, map));
            }

        }
        roomsThatRemain.Sort();
        roomsThatRemain[0].isMainRoom = true;
        roomsThatRemain[0].accessibleFromMainRoom = true;
        ConnectClosestRooms(roomsThatRemain);
    }



    //Helping functions to get all regions of specific type (wall/floor). This calls the GetRegionTiles that is an implementation of the FloodFill algorithm based on the type of region we are looking for.
    //We loop on each coordinate on the map and for each coordinate we check if its visited or not. If its unvisited we check if it is of the type we are examining and if it is find the region it belongs.
    //Then we find all other tiles that belong on the same region and also mark them as visited. 
    //Lastly we return the list of regions. Each region is a list of tiles represented by the Coord struct.
    List<List<Coord>> GetRegions(int tileType)
    {
        List<List<Coord>> regions = new List<List<Coord>>();
        int[,] TileOnRegionVisited = new int[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (TileOnRegionVisited[x, y] == 0 && map[x,y] ==tileType)
                {
                    List<Coord> newRegion = GetRegionTiles(x, y);
                    regions.Add(newRegion);
                    foreach (Coord tile in newRegion)
                    {
                        TileOnRegionVisited[tile.tileX, tile.tileY] = 1;
                    }
                }
            }
        }
        return regions;
    }

    //FloodFill Algorithm. We chose an unvisited tile from the map mark it as visited and and look at its orthogonal neibours. 
    //if they are unvisited and of the same type we add them to the queue to check later and add them to a list. 
    //This list is the region that all the visited tiles of that type belong to. We then repeat the prosses for each tile in the queue, until the queue is empty and return the region.
    List<Coord> GetRegionTiles(int StartX, int StartY)
    {
        List<Coord> region = new List<Coord>();
        int[,] RegionalVisitedTile = new int[width,height];
        int tileType = map[StartX, StartY];
        Queue<Coord> queue = new Queue<Coord>();
        queue.Enqueue(new Coord(StartX, StartY));
        RegionalVisitedTile[StartX, StartY] = 1;

        while (queue.Count > 0)
        {
            Coord tile = queue.Dequeue();
            region.Add(tile);
            for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
            {
                for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                {
                    if (IsInsideMap(x, y)&& (x==tile.tileX||tile.tileY==y))
                    {
                        if (RegionalVisitedTile[x, y] == 0 && map[x, y] == tileType)
                        {
                            RegionalVisitedTile[x,y] = 1;
                            queue.Enqueue(new Coord(x, y));
                        }
                    }
                }
            }
        }
        return region;

    }





    //The class Room is used to store the regions of floors we have previously found. We use a custom class as its easier to encapsulate more information about the region.
    // the room size in tiles as well as the edge tiles (tiles that have a wall as a neibour) are important for creating the corridors. 
    //Also it is important to know if the room is already connected to other rooms and what rooms it is connected to, so we dont have to take them into account 
    //when creating corridors as the calculation of the corridors is also recursive (we will eterate more than once through the rooms to make sure everything is connected with the shortest possible route. 
    // IComperable is an interface build by unity that allows us to compair classes based on a variable, in this case the roomSize.
    class Room : IComparable<Room>
    {
        public List<Coord> tiles;
        public List<Coord> edgeTiles;
        public List<Room> connectedRooms;
        public int roomSize;
        public bool isMainRoom;
        public bool accessibleFromMainRoom;
        public Room() { }

        public Room(List<Coord> roomTiles, int[,] map)
        {
            tiles = roomTiles;
            roomSize = tiles.Count;
            connectedRooms = new List<Room>();
            edgeTiles = new List<Coord>();
            foreach (Coord tile in tiles)
            {
                for (int x = tile.tileX - 1; x <= tile.tileX + 1; x++)
                {
                    for (int y = tile.tileY - 1; y <= tile.tileY + 1; y++)
                    {
                        if (x == tile.tileX || y == tile.tileY)
                        {
                            if (map[x, y] == 1)
                            {
                                edgeTiles.Add(tile);
                            }
                        }
                    }
                }
            }
        }
        //add rooms to the list of connected rooms of eachother
        public static void ConnectRooms(Room roomA, Room roomB)
        {
            if (roomA.accessibleFromMainRoom)
            {
                roomB.setAccessableFromMainRoom();
            }
            else if (roomB.accessibleFromMainRoom)
            {
                roomA.setAccessableFromMainRoom();
            }
            roomA.connectedRooms.Add(roomB);
            roomB.connectedRooms.Add(roomA);

        }
        public bool IsConnected(Room otherRoom)
        {
            return connectedRooms.Contains(otherRoom);
        }

        //helping function that allows us to find the biggest room to make our main room.
        public int CompareTo(Room otherRoom)
        {
            return otherRoom.roomSize.CompareTo(roomSize);
        }


        public void setAccessableFromMainRoom()
        {
            if (!accessibleFromMainRoom)
            {
                accessibleFromMainRoom = true;
                foreach (Room connectedRoom in connectedRooms)
                {
                    connectedRoom.setAccessableFromMainRoom();
                }
            }
        }

    }



    //Find the best possible connection points based on the minimum Manhantant distance between the edge tiles of each room.
    void ConnectClosestRooms(List<Room> allRooms, bool forceConnectivityFromMainRoom=false)
    {
        List<Room> roomListA = new List<Room>();
        List<Room> RoomListB = new List<Room>();

        //force connectivity from main room allows us to first create corridors on close by rooms and then on
        //a second pass make sure that all rooms are connected to the larger room of the map(main room)
        //if we enforce it we add all connected rooms to listB and all unconnected rooms to listA, otherwise we simply add all rooms to both lists.
        if (forceConnectivityFromMainRoom)
        {
            foreach (Room room in allRooms)
            {
                if (room.accessibleFromMainRoom)
                {
                    RoomListB.Add(room);
                }
                else
                {
                    roomListA.Add(room);
                }
            }
        }
        else
        {
            roomListA = allRooms;
            RoomListB = allRooms;

        }
        //we use manhantan distance since we dont really care about the actual distance but a distance we can compaire between the tiles/rooms.
        //Manhantan distance is easier to compute therefore faster, as the square root needed for the actual distance calculation is more taxing.
        int bestManhantanDistance=0;
        Coord bestTileA = new Coord();
        Coord bestTileB = new Coord();
        Room bestRoomA = new Room();
        Room bestRoomB = new Room();
        bool possibleConnectionFound = false;
        //Loop through all rooms in listA and if we are not forcing connectivity check that there is no connection to that room.
        foreach (Room roomA in roomListA)
        {
            if (!forceConnectivityFromMainRoom)
            {
                possibleConnectionFound = false;
                if (roomA.connectedRooms.Count > 0)
                {
                    continue;
                }
            }
            //Then for each room of listA check if the distance to each room in listB. Make sure that the two rooms checked are not the same room
            //and calculate the manhatan Distance of each of their edge tiles. find the two tiles that are closest each eteration and store them. 
            // when the for each loops end we will have the best route between the two rooms, connecting thier closest edge tiles.
            foreach (Room roomB in RoomListB)
            {
                if (roomA == roomB || roomA.IsConnected(roomB))
                {
                    continue;
                }
                
                for (int tileIndexA = 0; tileIndexA < roomA.edgeTiles.Count; tileIndexA++)
                {
                    for (int tileIndexB = 0; tileIndexB < roomB.edgeTiles.Count; tileIndexB++)
                    {
                        Coord tileA = roomA.edgeTiles[tileIndexA];
                        Coord tileB = roomB.edgeTiles[tileIndexB];
                        int distance = Mathf.Abs(tileA.tileX - tileB.tileX) + Mathf.Abs(tileA.tileY - tileB.tileY);
                        if (distance < bestManhantanDistance || !possibleConnectionFound)
                        {
                            bestManhantanDistance = distance;
                            possibleConnectionFound = true;
                            bestTileA = tileA;
                            bestTileB = tileB;
                            bestRoomA = roomA;
                            bestRoomB = roomB;
                        }
                    }
                }
            }
            //Since we dont force connectivity and we have found a connection between two rooms we go ahead and create it.
            if (possibleConnectionFound && !forceConnectivityFromMainRoom)
            {
                CreateCorridor(bestRoomA, bestRoomB, bestTileA, bestTileB);
            }
        }
        //Once the first eteration of the function is done we call the same function again to force connectivity to the main room.
        //This will create routes between rooms that where connected to another room that was not the main room. Note that this time the 
        // algorithm will go through all possible connections of listA and listB that are now containing unconnected and connected rooms to the main 
        // room respectively
        if (!forceConnectivityFromMainRoom)
        {
            ConnectClosestRooms(allRooms, true);
        }

        //Since we have now found the best routes for all unconected rooms to the main room we are creating those paths. 
        //We then call the function again with force connectivity true. This will ensure that any possible connections will 
        // be created. if there are no possible connections remaining the algorithm simply stops.
        if (possibleConnectionFound && forceConnectivityFromMainRoom)
        {
            CreateCorridor(bestRoomA, bestRoomB, bestTileA, bestTileB);
            ConnectClosestRooms(allRooms, true);
        }
    }

    //Create the corridor by changing all the tiles in the found path to be floors.
    void CreateCorridor(Room roomA, Room roomB, Coord tileA, Coord tileB)
    {
        Room.ConnectRooms(roomA, roomB);

        //Visual help for recognizing corridor paths
        //Debug.DrawLine(coordToWorld(tileA), coordToWorld(tileB),Color.green,100);
        List<Coord> line = GetLine(tileA, tileB);
        foreach (Coord c in line)
        {
            DrawCircle(c, corridorRadious);
        }
            
    }

    List<Coord> GetLine(Coord from, Coord to)
    {
        List<Coord> line = new List<Coord>();
        int x = from.tileX;
        int y = from.tileY;
        int dx = to.tileX - from.tileX;
        int dy = to.tileY - from.tileY;
        bool inverted = false;

        //find if we need to incriment x possitivly or negativly.
        int step = Math.Sign(dx);
        //find if we need to incriment y possitivly or negativly.
        int gradientStep = Math.Sign(dy);

        int longest = Mathf.Abs(dx);
        int shortest = Mathf.Abs(dy);

        if (longest<=shortest)
        {
            inverted = true;
            longest = Mathf.Abs(dy); 
            shortest = Mathf.Abs(dx);
            step = Math.Sign(dy);
            gradientStep = Math.Sign(dx);
        }

        int gradientAccumulation = longest / 2;
        for (int i = 0; i < longest; i++)
        {
            line.Add(new Coord(x, y));
            if (inverted)
            {
                y+=step;
            }
            else
            {
                x+=step;
            }
            gradientAccumulation += shortest;
            if (gradientAccumulation >= longest)
            {
                if (inverted)
                {
                    x += gradientStep;
                }
                else
                {
                    y += gradientStep;
                }
                gradientAccumulation -= longest;
            }
        }
        return line;
    }


    void DrawCircle(Coord c, int r)
    {
        for (int x = -r; x <= r; x++)
        {
            for (int y = -r; y <= r; y++)
            {
                if (x * x + y * y <= r*r)
                {
                    int drawX = c.tileX + x;
                    int drawY = c.tileY + y;
                    if (IsInsideMap(drawX, drawY))
                    {
                        map[drawX, drawY] = 0;
                    }
                }
            }
        }
    }

    Vector3 coordToWorld(Coord tile)
    {
        return new Vector3(-width / 2 + 0.5f+ tile.tileX, 0.6f, -height / 2 + 0.5f + tile.tileY);
    }



    //Used forr visualizing the results of the algorithm. This is a build in function of Unity. We could Implement our own in later stages 
    //That would allow for spawning custom assets for the floor and wall tiles.
    void OnDrawGizmos()
    {
        if (map != null)
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Gizmos.color = (map[x, y] == 1) ? Color.black : Color.white;
                    //We make sure the tiles are spread around the game objects smoothly. In this case the game object is in the middle of the 
                    //empty gameObject "mapGenerator" which is the gameObject it contains this script which is located in Vector(0,0,0) world position.
                    //This was done in order to be easier to move the map in case it was ever needed.
                    //We could easily change the pos variable calculation in order to only have possitive numbers in the pos Vector3
                    //That would mean that the gameObject would be on the left bottom corner of the map.
                    Vector3 pos = new Vector3(-width / 2 + x + 0.5f, 0, -height / 2 + y + 0.5f);
                    //Build in function to create a visual reprecentation of a cube. The scale of the cube must be Vector3(1,1,1) to match 
                    //reprecentation of the map we have created in the map table.
                    Gizmos.DrawCube(pos, Vector3.one);
                }
            }
        }
    }

}
