using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using Eto.Forms;
using System.Collections;
using System.Collections.ObjectModel;

namespace Eto.CustomControls
{
	public interface ITreeHandler
	{
		ITreeGridItem SelectedItem { get; }
		void SelectRow (int row);
		bool AllowMultipleSelection { get; }

		void PreResetTree ();
		void PostResetTree ();
	}

	public class TreeController : ITreeGridStore<ITreeGridItem>
	{
		readonly TreeDataStore treeDataStore;
		readonly ITreeHandler handler;

		List<TreeController> sections;
		TreeController parent;
		ITreeGridStore<ITreeGridItem> store;

		internal TreeController(TreeDataStore treeDataStore, ITreeHandler handler)
		{
			this.treeDataStore = treeDataStore;
			this.handler = handler;
		}

		int StartRow { get; set; }

		List<TreeController> Sections
		{
			get
			{
				if (sections == null) sections = new List<TreeController>();
				return sections;
			}
		}

		private void OnSectionRemoved(TreeController removedSection)
		{
			ReloadItem(GetItemAtRow(removedSection.StartRow));

			treeDataStore.RebuildSections();

			var removedItems = new List<ITreeGridItem>();
			for (int row = 0; row < removedSection.Count; row++)
			{
				removedItems.Add(removedSection.GetItemAtRow(row));
			}
			treeDataStore.UpdateCache(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
		}

		ITreeGridStore<ITreeGridItem> Store
		{
			get => store;
			set
			{
				DetachEvents();
				store = value;
				AttachEvents(value);
			}
		}

		void OnStoreCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					treeDataStore.RebuildSections();
					var newStartingIndex = IndexOf(e.NewItems.Cast<ITreeGridItem>().First());
					foreach (var newItem in e.NewItems.Cast<TreeGridItem>())
						newItem.Children.CollectionChanged += OnChildrenCollectionChanged;

					e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems,
						newStartingIndex);
					break;
				case NotifyCollectionChangedAction.Remove:
					treeDataStore.RebuildSections();
					var removedItems = new List<TreeGridItem>();
					foreach (var oldItem in e.OldItems.Cast<TreeGridItem>())
					{
						removedItems.Add(oldItem);
						removedItems.AddRange(GetGrandChildren(oldItem));
					}

					foreach (var item in removedItems)
						item.Children.CollectionChanged -= OnChildrenCollectionChanged;
					
					e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems);
					break;
				case NotifyCollectionChangedAction.Reset:
					ResetCollection();
					break;
				case NotifyCollectionChangedAction.Replace:
					break;
				case NotifyCollectionChangedAction.Move:
					//break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			treeDataStore.UpdateCache(e);
		}

		private IEnumerable<TreeGridItem> GetGrandChildren(TreeGridItem item)
		{
			foreach (var child in item.Children.Cast<TreeGridItem>())
			{
				yield return child;
				foreach (var grandChild in GetGrandChildren(child))
				{
					yield return grandChild;
				}
			}
		}

		void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					var addedItem = e.NewItems.Cast<TreeGridItem>().Single();
					addedItem.Children.CollectionChanged += OnChildrenCollectionChanged;
					if ((addedItem.Parent as TreeGridItem)?.Children?.Count == 1)
						ReloadItem(addedItem.Parent);
					if (IsExpanded(addedItem.Parent))
					{
						treeDataStore.RebuildSections();
						var row = IndexOf(addedItem);
						treeDataStore.UpdateCache(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItem, row));

						// Added child that contains own expanded children

						var addedChildrenItems = CollectVisibleChildren(addedItem).ToArray();
						treeDataStore.UpdateCache(
							new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedChildrenItems,
								row + 1));
						/*
						var section = FindSection(row);
						if (section != null)
						{
							IList addedChildren = new List<ITreeGridItem>();
							for (int i = 0; i < section.Count; i++)
							{
								addedChildren.Add(section[i]);
							}
							treeDataStore.UpdateCache(
								new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedChildren,
									row + 1));
						}
						*/
					}
					break;
				case NotifyCollectionChangedAction.Remove: // Remove or Collapse
					var removedItem = e.OldItems.Cast<TreeGridItem>().Single();
					removedItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
					if ((removedItem.Parent as TreeGridItem)?.Children?.Count == 0)
						ReloadItem(removedItem.Parent);
					if (IsExpanded(removedItem.Parent))
					{
						treeDataStore.UpdateCache(
							new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem));
						
						var removedChildrenItems = CollectVisibleChildren(removedItem).ToArray();
						if (removedChildrenItems.Any())
							treeDataStore.UpdateCache(
								new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedChildrenItems));
					}

					break;
				case NotifyCollectionChangedAction.Replace:
					break;
				case NotifyCollectionChangedAction.Move:
					break;
				case NotifyCollectionChangedAction.Reset:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		IEnumerable<ITreeGridItem> CollectVisibleChildren(ITreeGridItem parentItem)
		{
			if (parentItem.Expanded)
				foreach (var child in ((TreeGridItem)parentItem).Children)
				{
					yield return child;
					foreach (var child2 in CollectVisibleChildren(child))
					{
						yield return child2;
					}
				}
		}

		int IndexOf(object value)
		{
			var count = Count;
			for (int i = 0; i < count; i++)
			{
				if (ReferenceEquals(this[i], value))
					return i;
			}

			return -1;
		}

		void ReloadItem(ITreeGridItem item)
		{
			treeDataStore.UpdateCache(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item));
		}

		public void InitializeItems (ITreeGridStore<ITreeGridItem> store, bool notifyAll, int notifyRow = -1)
		{
			Store = store;

			if (notifyAll || StartRow == notifyRow)
			{
				IList list = new List<ITreeGridItem>();
				for (int i = 0; i < store.Count; i++)
				{
					list.Add(store[i]);
				}

				treeDataStore.UpdateCache(
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list,
						parent == null && StartRow == 0 ? StartRow : StartRow + 1));
			}

			ResetSections(notifyAll, notifyRow);
		}

		internal void ResetSections(bool notifyAll, int notifyRow = -1)
		{
			DetachEvents();
			Sections.Clear();
			if (Store != null)
			{
				var row = parent == null ? StartRow : StartRow + 1;
				for (int index = 0; index < Store.Count; index++, row++)
				{
					var item = Store[index];
					if (item.Expanded)
					{
						var children = (ITreeGridStore<ITreeGridItem>)item;
						var section = new TreeController(treeDataStore, handler) { StartRow = row, parent = this };
						section.InitializeItems(children, notifyAll, notifyRow);
						Sections.Add(section);
						row += section.Count;
					}
				}
			}
		}

		private void DetachEvents()
		{
			// Keep events only on Store and only in the root tree controller
			if (parent != null)
			{
				if (store is ObservableCollection<ITreeGridItem> observableStoreCollection)
				{
					observableStoreCollection.CollectionChanged -= OnStoreCollectionChanged;
					foreach (var item in observableStoreCollection.Cast<TreeGridItem>())
					{
						item.Children.CollectionChanged -= OnChildrenCollectionChanged;
					}
				}
			}

			if (sections != null && sections.Count > 0)
			{
				foreach (var section in sections)
				{
					section.DetachEvents();
				}
			}
		}

		private void AttachEvents(IDataStore<ITreeGridItem> value)
		{
			if (value is TreeGridItemCollection collection) AttachEventsInternal(collection);
			if (value is TreeGridItem treeGridItem) AttachEventsInternal(treeGridItem.Children);
		}

		private void AttachEventsInternal(ObservableCollection<ITreeGridItem> storeCollection)
		{
			storeCollection.CollectionChanged += OnStoreCollectionChanged;
			foreach (var item in storeCollection.Cast<TreeGridItem>())
				item.Children.CollectionChanged += OnChildrenCollectionChanged;
		}

		public void ReloadData()
		{
			treeDataStore.Clear();
			ResetSections(true);
			ResetCollection();
		}
		
		public int LevelAtRow (int row)
		{
			if (sections == null || sections.Count == 0)
				return 0;
			foreach (var section in sections) {
				if (row <= section.StartRow) {
					return 0;
				}
				else
				{
					var count = section.Count;
					if (row <= section.StartRow + count)
					{
						return section.LevelAtRow(row) + 1;
					}
				}
			}
			return 0;
		}

		public ITreeGridItem this[int row] => GetItemAtRow(row);

		public class TreeNode
		{
			public ITreeGridItem Item { get; set; }
			public int RowIndex { get; set; }
			public int Count { get; set; }
			public int Index { get; set; }
			public int Level { get; set; }
			public TreeNode Parent { get; set; }

			public bool IsFirstNode { get { return Index == 0; } }

			public bool IsLastNode { get { return Index == Count-1; } }
		}

		public TreeNode GetNodeAtRow (int row)
		{
			return GetNodeAtRow (row, null, 0);
		}

		TreeNode GetNodeAtRow (int row, TreeNode parent, int level)
		{
			var node = new TreeNode { RowIndex = row, Parent = parent, Count = Store.Count, Level = level };
			if (sections == null || sections.Count == 0) {
				node.Item = Store[row];
				node.Index = row;
			}
			else {
				foreach (var section in sections)
				{
					if (row <= section.StartRow)
					{
						node.Item = Store[row];
						node.Index = row;
						break;
					}
					if (row <= section.StartRow + section.Count)
					{
						node.Index = section.StartRow;
						node.Item = section.Store as ITreeGridItem;
						return section.GetNodeAtRow(row - section.StartRow - 1, node, level + 1);
					}
					row -= section.Count;
				}
			}
			if (node.Item == null && row < Store.Count) {
				node.Item = Store[row];
				node.Index = row;
			}
			return node;
		}

		internal ITreeGridItem GetItemAtRow(int row)
		{
			if (Store == null) return null;

			ITreeGridItem item = null;
			if (sections == null || sections.Count == 0)
				item = Store[row];
			if (item == null) {
				foreach (var section in sections)
				{
					if (row <= section.StartRow)
					{
						item = Store[row];
						break;
					}
					if (row <= section.StartRow + section.Count)
					{
						item = section.GetItemAtRow(row - section.StartRow - 1);
						break;
					}
					row -= section.Count;
				}
			}
			if (item == null && row < Store.Count)
				item = Store[row];
			return item;
		}

		public bool IsExpanded (int row)
		{
			if (sections == null) return false;
			foreach (var section in sections)
			{
				if (row < section.StartRow)
					return false;
				if (row == section.StartRow)
				{
					return true;
				}
				if (row <= section.StartRow + section.Count)
				{
					return section.IsExpanded(row - section.StartRow - 1);
				}
				row -= section.Count;
			}
			return false;
		}

		bool IsExpanded(ITreeGridItem value)
		{
			return value.Expanded && GetParents(value).All(item => item.Expanded);
		}

		public bool ExpandRow (int row)
		{
			var args = new TreeGridViewItemCancelEventArgs(GetItemAtRow(row));
			treeDataStore.OnExpanding(args);
			if (args.Cancel)
				return false;
			args.Item.Expanded = true;
			ResetSections(false, row);
			treeDataStore.OnExpanded(new TreeGridViewItemEventArgs(args.Item));
			return true;
		}

		bool ChildIsSelected (ITreeGridItem item)
		{
			var node = handler.SelectedItem;

			while (node != null) {
				node = node.Parent;

				if (object.ReferenceEquals (node, item))
					return true;
			}
			return false;
		}

		void ResetCollection ()
		{
			if (parent == null)
				handler.PreResetTree ();
			treeDataStore.UpdateCache(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
			if (parent == null)
				handler.PostResetTree ();
		}

		public bool CollapseRow (int row)
		{
			var args = new TreeGridViewItemCancelEventArgs (GetItemAtRow (row));
			treeDataStore.OnCollapsing(args);
			if (args.Cancel)
				return false;
			var shouldSelect = !handler.AllowMultipleSelection && ChildIsSelected (args.Item);
			args.Item.Expanded = false;
			treeDataStore.OnCollapsed(new TreeGridViewItemEventArgs(args.Item));
			CollapseSection (row);

			if (shouldSelect)
				handler.SelectRow (row);

			return true;
		}

		void CollapseSection (int row)
		{
			var section = FindSection(row);

			if (section != null)
			{
				Sections.Remove(section);
				OnSectionRemoved(section);
			}
			/*
			if (sections != null && sections.Count > 0)
			{
				bool addTop = true;
				foreach (var section in sections)
				{
					if (row <= section.StartRow)
					{
						break;
					}
					if (row <= section.StartRow + section.Count)
					{
						addTop = false;
						section.CollapseSection(row);
						break;
					}
				}

				if (addTop && row >= StartRow && row <= StartRow + Store.Count)
				{
					var section = Sections.Single(r => r.StartRow == row);
					Sections.Remove(section);
					OnSectionRemoved(section);
				}
			}
			*/
		}

		TreeController FindSection(int row)
		{
			if (sections != null && sections.Count > 0)
			{
				foreach (var section in sections)
				{
					if (row <= section.StartRow)
					{
						break;
					}
					if (row <= section.StartRow + section.Count)
					{
						return FindSection(row);
					}
				}

				if (row >= StartRow && row <= StartRow + Store.Count)
				{
					return Sections.SingleOrDefault(r => r.StartRow == row);
				}
			}

			return null;
		}

		public int Count
		{
			get
			{
				if (Store == null)
					return 0;
				if (sections != null)
					return Store.Count + sections.Sum(r => r.Count);
				return Store.Count;
			}
		}

		static IEnumerable<ITreeGridItem> GetParents (ITreeGridItem value)
		{
			ITreeGridItem parent = value.Parent;
			while (parent != null) {
				yield return parent;
				parent = parent.Parent;
			}
		}

		public void ExpandToItem (ITreeGridItem value)
		{
			var parents = GetParents (value).Reverse ();

			foreach (var item in parents) {
				var row = treeDataStore.IndexOf(item);
				if (row >= 0 && !IsExpanded(row))
					ExpandRow (row);
			}
		}
	}
}
