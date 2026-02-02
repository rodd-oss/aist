using System.Collections.ObjectModel;

namespace Aist.Tuist;

public class TuiElementCollection : Collection<TuiElement>
{
    private readonly TuiElement _owner;

    public TuiElementCollection(TuiElement owner)
    {
        _owner = owner;
    }

    protected override void InsertItem(int index, TuiElement item)
    {
        ArgumentNullException.ThrowIfNull(item);
        item.Parent = _owner;
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, TuiElement item)
    {
        ArgumentNullException.ThrowIfNull(item);
        Items[index].Parent = null;
        item.Parent = _owner;
        base.SetItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        Items[index].Parent = null;
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        foreach (var item in Items)
        {
            item.Parent = null;
        }
        base.ClearItems();
    }
}
