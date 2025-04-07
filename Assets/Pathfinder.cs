using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // Required for OrderBy or Min operations on the list

public static class Pathfinder
{
    // --- Helper Class for A* Nodes ---
    private class PathNode
    {
        public Vector3Int cellPosition;
        public int gCost; // Cost from start node to this node
        public int hCost; // Heuristic cost from this node to target node
        public int fCost; // Total cost (gCost + hCost)
        public PathNode parent; // Reference to the node we came from

        public PathNode(Vector3Int position)
        {
            cellPosition = position;
            gCost = int.MaxValue; // Initialize with infinity
            hCost = 0;
            fCost = int.MaxValue;
            parent = null;
        }

        public void CalculateFCost()
        {
            fCost = gCost + hCost;
        }
    }

    // Performs A* Search to find the shortest path
    // Returns a list of world positions for the path, or null if no path found.
    public static List<Vector3> FindPath(Tilemap groundTilemap, Tilemap obstacleTilemap, Vector3Int startCell, Vector3Int targetCell)
    {
        // --- Initialization ---
        // Use a list to store nodes to visit (Open List). In a highly optimized scenario, a Priority Queue/MinHeap would be better.
        List<PathNode> openList = new List<PathNode>();
        // Use a hash set to store nodes already visited (Closed List) for quick lookup.
        HashSet<Vector3Int> closedList = new HashSet<Vector3Int>();
        // Dictionary to quickly access node data based on cell position
        Dictionary<Vector3Int, PathNode> nodeData = new Dictionary<Vector3Int, PathNode>();


        // --- Basic Checks --- (Same as BFS)
        if (IsCellBlocked(obstacleTilemap, targetCell))
        {
            Debug.LogWarning("Target cell is blocked.");
            return null;
        }
        if (IsCellBlocked(obstacleTilemap, startCell))
        {
            Debug.LogWarning("Start cell is blocked.");
            return null;
        }
         // Check if start and target are the same
        if (startCell == targetCell) {
            // Path is just the starting cell's center
            return new List<Vector3> { groundTilemap.GetCellCenterWorld(startCell) };
        }


        // --- Prepare Starting Node ---
        PathNode startNode = new PathNode(startCell);
        startNode.gCost = 0;
        startNode.hCost = CalculateManhattanDistance(startCell, targetCell);
        startNode.CalculateFCost();

        openList.Add(startNode);
        nodeData[startCell] = startNode;


        // --- A* Loop ---
        while (openList.Count > 0)
        {
            // Find the node with the lowest fCost in the open list
            // (This is where a Priority Queue would be much more efficient than searching the list)
            PathNode currentNode = GetLowestFCostNode(openList);

            // --- Target Found ---
            if (currentNode.cellPosition == targetCell)
            {
                // Path found, reconstruct it
                return ReconstructPath(currentNode, groundTilemap);
            }

            // Move current node from open list to closed list
            openList.Remove(currentNode);
            closedList.Add(currentNode.cellPosition);

            // --- Explore Neighbors (4 Directions) ---
            foreach (Vector3Int neighborCell in GetNeighbors(currentNode.cellPosition))
            {
                // Skip if neighbor is already processed or is not walkable
                if (closedList.Contains(neighborCell) || !IsCellWalkable(groundTilemap, obstacleTilemap, neighborCell, targetCell))
                {
                    continue;
                }

                // Calculate tentative gCost for the neighbor
                // Cost from start to current + cost from current to neighbor (which is 1 for adjacent cells)
                int tentativeGCost = currentNode.gCost + 1; // Assuming cost between adjacent cells is 1

                // Get or create the neighbor node data
                PathNode neighborNode;
                if (nodeData.TryGetValue(neighborCell, out neighborNode))
                {
                    // Neighbor node data already exists
                    if (tentativeGCost < neighborNode.gCost)
                    {
                        // Found a shorter path to this neighbor, update it
                        neighborNode.parent = currentNode;
                        neighborNode.gCost = tentativeGCost;
                        neighborNode.hCost = CalculateManhattanDistance(neighborCell, targetCell); // Recalculate H just in case (though usually static)
                        neighborNode.CalculateFCost();

                        // If it wasn't in the open list (though it should be if we reached here via a longer path), add it.
                        // This check handles cases slightly differently depending on exact A* implementation details.
                        // For simplicity here, if we found a better path, ensure it's considered.
                         if (!openList.Contains(neighborNode)) {
                             openList.Add(neighborNode);
                         }
                    }
                }
                else
                {
                    // Neighbor node data doesn't exist, create it
                    neighborNode = new PathNode(neighborCell);
                    nodeData[neighborCell] = neighborNode; // Add to our data dictionary

                    neighborNode.parent = currentNode;
                    neighborNode.gCost = tentativeGCost;
                    neighborNode.hCost = CalculateManhattanDistance(neighborCell, targetCell);
                    neighborNode.CalculateFCost();

                    openList.Add(neighborNode); // Add new node to the open list
                }
            }
        }

        // --- No Path Found ---
        // Open list is empty, but target was not reached
        Debug.LogWarning("Path not found!");
        return null;
    }

    // Helper to find the node with the lowest F cost in a list
    private static PathNode GetLowestFCostNode(List<PathNode> pathNodeList)
    {
        PathNode lowestFCostNode = pathNodeList[0];
        for (int i = 1; i < pathNodeList.Count; i++)
        {
            // If fCosts are equal, optionally use hCost as a tie-breaker
            if (pathNodeList[i].fCost < lowestFCostNode.fCost ||
               (pathNodeList[i].fCost == lowestFCostNode.fCost && pathNodeList[i].hCost < lowestFCostNode.hCost))
            {
                lowestFCostNode = pathNodeList[i];
            }
        }
        return lowestFCostNode;
    }


    // Helper to reconstruct the path from the end node
    private static List<Vector3> ReconstructPath(PathNode endNode, Tilemap groundTilemap)
    {
        List<Vector3> path = new List<Vector3>();
        PathNode currentNode = endNode;
        while (currentNode.parent != null) // Stop when we reach the start node (whose parent is null)
        {
            path.Add(groundTilemap.GetCellCenterWorld(currentNode.cellPosition));
            currentNode = currentNode.parent;
        }
        // Optional: Add the start node's center if needed (depends if you want path to include start)
        // path.Add(groundTilemap.GetCellCenterWorld(currentNode.cellPosition));

        path.Reverse(); // Reverse the list to get path from start to target
        return path;
    }


    // --- Heuristic Calculation (Manhattan Distance for 4-directional grid) ---
    private static int CalculateManhattanDistance(Vector3Int a, Vector3Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }


    // --- Existing Helper Methods (Unchanged) ---

    // Helper to check if a cell is blocked (has a tile on the obstacle map)
    private static bool IsCellBlocked(Tilemap obstacleTilemap, Vector3Int cell)
    {
        if (obstacleTilemap == null) return false; // No obstacle map means nothing is blocked this way
        return obstacleTilemap.HasTile(cell);
    }

    // Helper to check if a cell is within ground bounds and not blocked
    private static bool IsCellWalkable(Tilemap groundTilemap, Tilemap obstacleTilemap, Vector3Int cell, Vector3Int targetCell)
    {
        if(cell == targetCell)
        {
            return true; // Target cell is walkable
        }
        // Check if the cell exists on the ground tilemap (basic bounds check)
        if (!groundTilemap.HasTile(cell))
        {
            // If empty space outside explicitly drawn ground is unwalkable:
            // return false;
            // If empty space *is* walkable, remove this check or use map boundaries.
             // Assuming empty space outside ground is NOT walkable for this example.
             return false;
        }

        // Check if the cell is blocked by an obstacle
        return !IsCellBlocked(obstacleTilemap, cell);
    }

    // Helper to get the 4 direct neighbors of a cell
    private static Vector3Int[] GetNeighbors(Vector3Int cell)
    {
        return new Vector3Int[] {
            cell + Vector3Int.up,
            cell + Vector3Int.down,
            cell + Vector3Int.left,
            cell + Vector3Int.right
        };
    }
}