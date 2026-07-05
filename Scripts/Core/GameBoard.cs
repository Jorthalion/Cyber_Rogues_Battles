using System.Collections.Generic;

namespace CyberRogueWars
{
	/// <summary>
	/// Represents a single cell on the 12×8 game board.
	/// Knows its terrain type and which unit (if any) is standing on it.
	/// </summary>
	public class BoardCell
	{
		public int X { get; }
		public int Y { get; }
		public TerrainType Terrain { get; set; } = TerrainType.Open;

		/// <summary>The unit currently occupying this cell. Null if empty.</summary>
		public UnitInstance OccupyingUnit { get; set; } = null;

		public bool IsOccupied => OccupyingUnit != null;

		public BoardCell(int x, int y, TerrainType terrain = TerrainType.Open)
		{
			X       = x;
			Y       = y;
			Terrain = terrain;
		}

		public override string ToString()
			=> $"Cell({X},{Y}) [{Terrain}] {(IsOccupied ? OccupyingUnit.Data.UnitName : "empty")}";
	}

	/// <summary>
	/// The 12×8 game board. Owns all BoardCells and provides helpers for
	/// querying, placing, moving, and removing units.
	///
	/// Grid origin (0,0) is top-left.
	///   X → increases right  (0–11)
	///   Y ↓ increases down   (0–7)
	/// </summary>
	public class GameBoard
	{
		// ─── Dimensions ───────────────────────────────────────────
		public const int Cols = 12;
		public const int Rows = 8;

		// ─── Internal Grid ────────────────────────────────────────
		private readonly BoardCell[,] _grid = new BoardCell[Cols, Rows];

		// ─── Constructor ──────────────────────────────────────────
		public GameBoard()
		{
			InitialiseGrid();
		}

		/// <summary>Fill every cell with an empty Open-terrain BoardCell.</summary>
		private void InitialiseGrid()
		{
			for (int x = 0; x < Cols; x++)
				for (int y = 0; y < Rows; y++)
					_grid[x, y] = new BoardCell(x, y);
		}

		// ─── Cell Access ──────────────────────────────────────────

		/// <summary>
		/// Returns the cell at (x, y), or null if out of bounds.
		/// Always null-check the result before using it!
		/// </summary>
		public BoardCell GetCell(int x, int y)
		{
			if (!InBounds(x, y)) return null;
			return _grid[x, y];
		}

		/// <summary>True if (x, y) is inside the 12×8 grid.</summary>
		public bool InBounds(int x, int y)
			=> x >= 0 && x < Cols && y >= 0 && y < Rows;

		// ─── Unit Queries ─────────────────────────────────────────

		/// <summary>Returns the unit at (x, y), or null if the cell is empty/OOB.</summary>
		public UnitInstance GetUnitAt(int x, int y)
			=> GetCell(x, y)?.OccupyingUnit;

		/// <summary>True if the cell at (x, y) is in bounds, passable, and unoccupied.</summary>
		public bool IsCellFree(int x, int y)
		{
			BoardCell cell = GetCell(x, y);
			return cell != null
				&& cell.Terrain != TerrainType.Impassable
				&& !cell.IsOccupied;
		}

		// ─── Unit Placement ───────────────────────────────────────

		/// <summary>
		/// Place a unit on the board at (x, y).
		/// Updates both the cell reference and the unit's GridX/GridY.
		/// Returns false if the cell is occupied or out of bounds.
		/// </summary>
		public bool PlaceUnit(UnitInstance unit, int x, int y)
		{
			if (!IsCellFree(x, y)) return false;

			_grid[x, y].OccupyingUnit = unit;
			unit.GridX = x;
			unit.GridY = y;
			return true;
		}

		/// <summary>
		/// Move a unit from its current position to (toX, toY).
		/// Clears the old cell and occupies the new one.
		/// Returns false if the destination is blocked.
		/// </summary>
		public bool MoveUnit(UnitInstance unit, int toX, int toY)
		{
			if (!IsCellFree(toX, toY)) return false;

			// Clear old cell
			BoardCell oldCell = GetCell(unit.GridX, unit.GridY);
			if (oldCell != null) oldCell.OccupyingUnit = null;

			// Occupy new cell
			_grid[toX, toY].OccupyingUnit = unit;
			unit.GridX = toX;
			unit.GridY = toY;
			return true;
		}

		/// <summary>Remove a unit from the board (e.g., when it dies).</summary>
		public void RemoveUnit(UnitInstance unit)
		{
			BoardCell cell = GetCell(unit.GridX, unit.GridY);
			if (cell != null && cell.OccupyingUnit == unit)
				cell.OccupyingUnit = null;
		}

		// ─── Neighbour Helpers ────────────────────────────────────

		/// <summary>
		/// Returns the four cardinal neighbours of (x, y) that are in bounds.
		/// Diagonals are NOT included (grid is cardinal-movement only).
		/// </summary>
		public List<BoardCell> GetCardinalNeighbours(int x, int y)
		{
			var neighbours = new List<BoardCell>(4);
			int[] dx = {  0,  0, -1, 1 };
			int[] dy = { -1,  1,  0, 0 };

			for (int i = 0; i < 4; i++)
			{
				BoardCell c = GetCell(x + dx[i], y + dy[i]);
				if (c != null) neighbours.Add(c);
			}
			return neighbours;
		}

		/// <summary>
		/// Flood-fill BFS — returns all cells reachable within [range] steps
		/// from (startX, startY), ignoring impassable terrain and occupied cells.
		/// Great for highlighting movement options in the UI.
		/// </summary>
		public List<BoardCell> GetReachableCells(int startX, int startY, int range)
		{
			var visited = new HashSet<(int, int)>();
			var result  = new List<BoardCell>();
			var queue   = new Queue<(int x, int y, int steps)>();

			queue.Enqueue((startX, startY, 0));
			visited.Add((startX, startY));

			while (queue.Count > 0)
			{
				var (cx, cy, steps) = queue.Dequeue();

				if (steps > 0) // don't include the starting cell itself
					result.Add(_grid[cx, cy]);

				if (steps >= range) continue;

				foreach (BoardCell neighbour in GetCardinalNeighbours(cx, cy))
				{
					if (visited.Contains((neighbour.X, neighbour.Y))) continue;
					if (neighbour.Terrain == TerrainType.Impassable)  continue;
					if (neighbour.IsOccupied)                          continue;

					visited.Add((neighbour.X, neighbour.Y));
					queue.Enqueue((neighbour.X, neighbour.Y, steps + 1));
				}
			}

			return result;
		}

		/// <summary>
		/// Returns all cells within [range] of (x, y) that contain an enemy unit
		/// (i.e., a unit NOT owned by [attacker]).
		/// Used to highlight attack targets.
		/// </summary>
		public List<UnitInstance> GetEnemiesInRange(int x, int y, int range, PlayerData attacker)
		{
			var enemies = new List<UnitInstance>();

			for (int dx = -range; dx <= range; dx++)
			{
				for (int dy = -range; dy <= range; dy++)
				{
					// Manhattan distance check (no diagonal cheating!)
					if (System.Math.Abs(dx) + System.Math.Abs(dy) > range) continue;
					if (dx == 0 && dy == 0) continue;

					UnitInstance u = GetUnitAt(x + dx, y + dy);
					if (u != null && u.Owner != attacker)
						enemies.Add(u);
				}
			}

			return enemies;
		}

		// ─── Terrain Helpers ──────────────────────────────────────

		/// <summary>Set the terrain type of a cell (used during map generation).</summary>
		public void SetTerrain(int x, int y, TerrainType terrain)
		{
			BoardCell cell = GetCell(x, y);
			if (cell != null) cell.Terrain = terrain;
		}

		/// <summary>
		/// Apply a cover defense bonus based on terrain.
		/// Cover = +1 defense. Hazard = -1 defense (min 0). Open = no change.
		/// </summary>
		public int GetTerrainDefenseModifier(int x, int y)
		{
			BoardCell cell = GetCell(x, y);
			if (cell == null) return 0;
			return cell.Terrain switch
			{
				TerrainType.Cover  =>  1,
				TerrainType.Hazard => -1,
				_                  =>  0,
			};
		}

		// ─── Debug ────────────────────────────────────────────────

		/// <summary>
		/// Dump the board state to a string grid. Useful for console debugging.
		/// '.' = empty open, '#' = impassable, '~' = hazard, 'C' = cover, 'U' = unit.
		/// </summary>
		public string DebugPrint()
		{
			var sb = new System.Text.StringBuilder();
			for (int y = 0; y < Rows; y++)
			{
				for (int x = 0; x < Cols; x++)
				{
					BoardCell cell = _grid[x, y];
					if (cell.IsOccupied)
						sb.Append('U');
					else
						sb.Append(cell.Terrain switch
						{
							TerrainType.Impassable => '#',
							TerrainType.Hazard     => '~',
							TerrainType.Cover      => 'C',
							_                      => '.',
						});
				}
				sb.AppendLine();
			}
			return sb.ToString();
		}
	}
}
