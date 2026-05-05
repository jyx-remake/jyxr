namespace JyGame
{
	public class LocationBlock
	{
		public int X;

		public int Y;

		public LocationBlock()
		{
		}

		public LocationBlock(int x, int y)
		{
			X = x;
			Y = y;
		}

		public override bool Equals(object obj)
		{
			return X == (obj as LocationBlock).X && Y == (obj as LocationBlock).Y;
		}

		public override int GetHashCode()
		{
			return X * 10000 + Y;
		}
	}
}
