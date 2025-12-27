using UnityEngine;

[CreateAssetMenu(menuName = "Slot/Symbol Sprite Set")]
public class SymbolSpriteSet : ScriptableObject
{
    [System.Serializable]
    public struct Entry
    {
        public SymbolId id;
        public Sprite sprite;
    }

    public Entry[] entries;

    public Sprite Get(SymbolId id)
    {
        for (int i = 0; i < entries.Length; i++)
        {
            if (entries[i].id == id) return entries[i].sprite;
        }
        return null;
    }
}
