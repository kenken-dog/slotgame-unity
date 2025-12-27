using System.Collections.Generic;

public class ItemInventory
{
    private readonly HashSet<ItemId> _owned = new HashSet<ItemId>();

    public bool Has(ItemId id) => _owned.Contains(id);

    public bool TryAdd(ItemId id)
    {
        return _owned.Add(id); // すでに所持なら false
    }

    public IEnumerable<ItemId> GetAllOwned() => _owned;
}
