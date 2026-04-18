using SmartSolutionsLab.Yumney.Shared.Common;

namespace SmartSolutionsLab.Yumney.Users.Domain.StaplesList;

#pragma warning disable SA1311
public sealed class StaplesList : AggregateRoot<StaplesListIdentifier>
{
	private static readonly StapleItem[] defaultItems =
	[
		StapleItem.From("salt"),
		StapleItem.From("pepper"),
		StapleItem.From("olive oil"),
		StapleItem.From("vegetable oil"),
		StapleItem.From("flour"),
		StapleItem.From("sugar"),
		StapleItem.From("butter"),
		StapleItem.From("eggs"),
		StapleItem.From("garlic"),
		StapleItem.From("onion"),
	];

	private readonly List<StapleItem> items = [];

	public OwnerIdentifier Owner { get; private set; } = default!;

	public IReadOnlyList<StapleItem> Items => items.AsReadOnly();

	private StaplesList()
	{
	}

	public static StaplesList Create(OwnerIdentifier owner)
	{
		return new StaplesList
		{
			Id = StaplesListIdentifier.New(),
			Owner = owner,
		};
	}

	public static StaplesList CreateWithDefaults(OwnerIdentifier owner)
	{
		var list = Create(owner);
		foreach (var item in defaultItems)
			list.items.Add(item);
		return list;
	}

	public StaplesList AddItem(StapleItem item)
	{
		if (!ContainsItem(item))
			items.Add(item);
		return this;
	}

	public StaplesList RemoveItem(StapleItem item)
	{
		items.Remove(item);
		return this;
	}

	public bool ContainsItem(StapleItem item) => items.Contains(item);

	public static IReadOnlyList<StapleItem> DefaultItems => defaultItems;
}
#pragma warning restore SA1311
