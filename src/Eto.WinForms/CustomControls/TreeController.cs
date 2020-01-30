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
			treeDataStore.RebuildRows();

			var removedItems = new List<ITreeGridItem>();
			for (int row = 0; row < removedSection.Count; row++)
			{
				removedItems.Add(removedSection.GetItemAtRow(row));
			}

			ReloadItem(GetItemAtRow(removedSection.StartRow));
			treeDataStore.UpdateCache(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
		}

		ITreeGridStore<ITreeGridItem> Store
		{
			get => store;
			set
			{
                if (store is ObservableCollection<ITreeGridItem> oldCollection)
                {
	                oldCollection.CollectionChanged -= OnStoreCollectionChanged;
					foreach (var item in oldCollection.Cast<TreeGridItem>())
	                {
		                item.Children.CollectionChanged -= OnChildrenCollectionChanged;
	                }
				}
				if (value is ObservableCollection<ITreeGridItem> newCollection)
				{
					newCollection.CollectionChanged += OnStoreCollectionChanged;
					foreach (var item in newCollection.Cast<TreeGridItem>())
					{
						item.Children.CollectionChanged += OnChildrenCollectionChanged;
					}
				}
				store = value;
			}
		}

		void OnStoreCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					treeDataStore.RebuildRows();
					var newStartingIndex = IndexOf(e.NewItems.Cast<ITreeGridItem>().First());
					foreach (var newItem in e.NewItems.Cast<TreeGridItem>())
						newItem.Children.CollectionChanged += OnChildrenCollectionChanged;

					e = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems,
						newStartingIndex);
					break;
				case NotifyCollectionChangedAction.Remove:
					treeDataStore.RebuildRows();
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
			treeDataStore.RebuildRows();
			
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var newItem in e.NewItems.Cast<ITreeGridItem>().ToArray())
					{
						if ((newItem.Parent as TreeGridItem)?.Children?.Count == e.NewItems.Count)
							ReloadItem(newItem.Parent);
						if (GetParents(newItem).All(item => item.Expanded))
						{
							var row = IndexOf(newItem);
							treeDataStore.UpdateCache(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, row));
						}
					}

					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (var removedItem in e.OldItems.Cast<ITreeGridItem>().ToArray())
					{
						ReloadItem(removedItem.Parent);
						if (GetParents(removedItem).All(item => item.Expanded))
							treeDataStore.UpdateCache(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem));
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
			treeDataStore.UpdateCache (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
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

				if (addTop && row < Store.Count)
				{
					var section = Sections.Single(r => r.StartRow == row);
					Sections.Remove(section);
					OnSectionRemoved(section);
				}
			}
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
