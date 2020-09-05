using UnityEngine;

[System.Serializable]
public class Player {
    [SerializeField] private PlayerTag tag = PlayerTag.Player0;
    [SerializeField] private string name = "Player 0";
    [SerializeField] private Color color = Color.blue;

    public PlayerTag Tag => tag;
    public string Name => name;
    public Color Color => color;
}