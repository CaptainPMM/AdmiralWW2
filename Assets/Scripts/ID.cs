[System.Serializable]
public class ID {
    [UnityEngine.SerializeField] private string id;

    public ID(string fromString) {
        id = fromString;
    }

    public override bool Equals(object obj) {
        ID otherID = obj as ID;
        return otherID != null && otherID.id == id;
    }

    public static bool operator ==(ID lhs, ID rhs) {
        return lhs.id == rhs.id;
    }

    public static bool operator !=(ID lhs, ID rhs) {
        return lhs.id != rhs.id;
    }

    public override int GetHashCode() {
        return base.GetHashCode();
    }

    public override string ToString() {
        return id;
    }
}