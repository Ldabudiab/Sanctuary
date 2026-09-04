using Godot;

public static class WorldTransition
{
	private const ulong TransitionCooldownMilliseconds = 1000;
	private static string _pendingSpawnPoint = string.Empty;
	private static bool _playerWasCarryingFood;
	private static ulong _blockedUntil;

	public static bool TryTravel(Node context, string scenePath, string spawnPointName)
	{
		ulong now = Time.GetTicksMsec();
		if (now < _blockedUntil || string.IsNullOrEmpty(scenePath) || string.IsNullOrEmpty(spawnPointName))
			return false;

		Player player = context.GetTree().GetFirstNodeInGroup("player") as Player;
		_playerWasCarryingFood = player != null && player.IsCarryingFood;
		_pendingSpawnPoint = spawnPointName;
		_blockedUntil = now + TransitionCooldownMilliseconds;

		Error error = context.GetTree().ChangeSceneToFile(scenePath);
		if (error == Error.Ok)
			return true;

		GD.PushError($"Unable to travel to '{scenePath}': {error}");
		_pendingSpawnPoint = string.Empty;
		return false;
	}

	public static void PlacePlayerAtPendingSpawn(Node worldRoot)
	{
		if (string.IsNullOrEmpty(_pendingSpawnPoint))
			return;

		Player player = worldRoot.GetTree().GetFirstNodeInGroup("player") as Player;
		Marker2D spawnPoint = null;

		foreach (Node node in worldRoot.GetTree().GetNodesInGroup("spawn_points"))
		{
			if (node is Marker2D marker && marker.Name == _pendingSpawnPoint)
			{
				spawnPoint = marker;
				break;
			}
		}

		if (player == null || spawnPoint == null)
		{
			GD.PushError($"Could not place Player at spawn point '{_pendingSpawnPoint}'.");
			return;
		}

		player.GlobalPosition = spawnPoint.GlobalPosition;
		player.Velocity = Vector2.Zero;
		player.SetCarryingFood(_playerWasCarryingFood);
		_pendingSpawnPoint = string.Empty;
		_blockedUntil = Time.GetTicksMsec() + TransitionCooldownMilliseconds;
	}
}
