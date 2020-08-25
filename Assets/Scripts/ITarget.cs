using UnityEngine;

public interface ITarget {
    GameObject GameObject { get; }
    Vector3 WorldPos { get; }
    Vector3 Velocity { get; }
}