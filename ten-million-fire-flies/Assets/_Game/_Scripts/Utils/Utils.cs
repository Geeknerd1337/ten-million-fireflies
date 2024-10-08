using UnityEngine;
public class FireFlyUtils

{
	public static float Remap(float value, float from1 = 0f, float to1 = 1f, float from2 = -1f, float to2 = 1f)
	{
		return (value - from1) / (to1 - from1) * (to2 - from2) + from2;
	}

	public static Vector3 RandomPointInSphereSmoothedGaussian(int count, float radius, float kernel = 1.0f, int smoothingSamples = 3)
	{
		// Generate smoothed Gaussian-distributed points for x, y, z
		float x = SmoothedGaussianRandom(kernel, smoothingSamples);
		float y = SmoothedGaussianRandom(kernel, smoothingSamples);
		float z = SmoothedGaussianRandom(kernel, smoothingSamples);

		// Normalize the point to bring it within a sphere
		Vector3 point = new Vector3(x, y, z).normalized;

		// Scale by a Gaussian-distributed distance from the center
		float gaussianRadius = Mathf.Abs(SmoothedGaussianRandom(kernel, smoothingSamples)) * radius;

		return point * gaussianRadius;
	}

	public static Vector3 RandomPointInSphereWithCurve(int count, float radius, AnimationCurve distributionCurve)
	{
		// Generate random direction using spherical coordinates
		float theta = UnityEngine.Random.Range(0f, 2.0f * Mathf.PI); // Random angle between 0 and 2π
		float phi = Mathf.Acos(2.0f * UnityEngine.Random.Range(0f, 1f) - 1.0f); // Random angle for latitude

		// Convert spherical coordinates to Cartesian coordinates
		float x = Mathf.Sin(phi) * Mathf.Cos(theta);
		float y = Mathf.Sin(phi) * Mathf.Sin(theta);
		float z = Mathf.Cos(phi);

		// Generate a random value between 0 and 1, then use the animation curve to modify it
		float randomValue = UnityEngine.Random.Range(0f, 1f);
		float scaledValue = distributionCurve.Evaluate(randomValue);  // Use the curve to modify the random value

		// Scale the random value by the radius
		float randomRadius = radius * Mathf.Pow(scaledValue, 1.0f / 3.0f);  // Cube root to maintain uniform distribution in volume

		return new Vector3(x, y, z) * randomRadius; // Scale by the random radius
	}

	// Helper function to generate a smoothed Gaussian-distributed random value using Box-Muller transform, with kernel and smoothing
	private static float SmoothedGaussianRandom(float kernel = 1.0f, int samples = 3)
	{
		float sum = 0f;

		// Take the average of multiple Gaussian samples to smooth the result
		for (int i = 0; i < samples; i++)
		{
			float u1 = UnityEngine.Random.Range(0f, 1f);
			float u2 = UnityEngine.Random.Range(0f, 1f);

			// Box-Muller transform to get Gaussian distribution
			float randStdNormal = Mathf.Sqrt(-2.0f * Mathf.Log(u1)) * Mathf.Sin(2.0f * Mathf.PI * u2);
			sum += randStdNormal;
		}

		// Return the average of the samples, scaled by the kernel
		return (sum / samples) * kernel;
	}

}