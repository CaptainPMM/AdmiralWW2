using UnityEngine;

public static class SafeRandom {
    public static int Seed { get; set; }

    public static float Range(float min, float max) {
        Random.InitState(Seed);
        return Random.Range(min, max);
    }
}