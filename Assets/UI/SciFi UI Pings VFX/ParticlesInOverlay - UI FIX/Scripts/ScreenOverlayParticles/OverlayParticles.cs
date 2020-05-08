using UnityEngine;
using System.Collections;

public static class OverlayParticles 
{
	private static ParticlesPlayer player;
	private static ParticlesDisplayer displayer {
        get {
            return Script.Get<ParticlesDisplayer>();
        }
    }


	public static void ShowParticles(int count)
	{
		displayer.ResetPosition();
		player.ShowParticles(count);
	}

	public static void ShowParticles(int count, Vector3 pos)
	{
		displayer.MoveToPosition(pos);
		player.ShowParticles(count);
	}

	public static void ShowContinuousParticles()
	{
		displayer.ResetPosition();
		player.StartContinuousEmission();
	}


	public static void ShowContinuousParticles(Vector3 pos)
	{
		displayer.MoveToPosition(pos);
		player.StartContinuousEmission();
	}

	public static void StopContinuousParticles()
	{
		player.StopEmission();
		displayer.ResetPosition();
	}
}
