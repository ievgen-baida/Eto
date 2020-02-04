using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Eto.Forms;

namespace Eto.Wpf.CustomControls.TreeGridView
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

		ITreeGridStore<ITreeGridItem> Store
		{
			get => store;
			set
			{
				DetachEvents(force: true);
				store = value;
				AttachEvents(value);
			}
		}

		void OnStoreCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var newItem in e.NewItems.Cast<TreeGridItem>())
						newItem.Children.CollectionChanged += OnChildrenCollectionChanged;
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (var oldItem in e.OldItems.Cast<TreeGridItem>())
						oldItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
					break;
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
					break;
				case NotifyCollectionChangedAction.Reset:
					treeDataStore.Refresh(force: true);
					return;
			}

			treeDataStore.Refresh();
		}
		void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					var addedItem = e.NewItems.Cast<TreeGridItem>().Single();
					if ((addedItem.Parent as TreeGridItem)?.Children?.Count == 1)
						ReloadItem(addedItem.Parent);
					break;
				case NotifyCollectionChangedAction.Remove: // Remove or Collapse
					var removedItem = e.OldItems.Cast<TreeGridItem>().Single();
					if ((removedItem.Parent as TreeGridItem)?.Children?.Count == 0)
						ReloadItem(removedItem.Parent);
					break;
				case NotifyCollectionChangedAction.Replace:
				case NotifyCollectionChangedAction.Move:
				case NotifyCollectionChangedAction.Reset: // Clear
					// No need to refresh TreeDataStore here.
					// If children are visible they will be the part of the other TreeController's Store and
					// will be processed in its OnStoreCollectionChanged handler
					break;
			}
		}

		void ReloadItem(ITreeGridItem item)
		{
			treeDataStore.RefreshItem(item);
		}

		public void InitializeItems(ITreeGridStore<ITreeGridItem> items)
		{
			Store = items;
			if (parent != null) ResetSections();
		}

		internal void ResetSections()
		{
			DetachEvents(force: false);
			sections?.Clear();
			if (Store != null)
			{
				for (int row = 0; row < Store.Count; row++)
				{
					var item = Store[row];
					if (item.Expanded)
					{
						var children = (ITreeGridStore<ITreeGridItem>)item;
						var section = new TreeController(treeDataStore, handler) { StartRow = row, parent = this };
						section.InitializeItems(children);
						Sections.Add(section);
					}
				}
			}
		}

		private void DetachEvents(bool force)
		{
			// Keep events only on Store and only in the root tree controller, unless Store is reassigned
			if (force)
			{
				ObservableCollection<ITreeGridItem> storeCollection = null;
				if (store is TreeGridItemCollection collection) storeCollection = collection;
				if (store is TreeGridItem treeGridItem) storeCollection = treeGridItem.Children;

				if (storeCollection != null)
				{
					storeCollection.CollectionChanged -= OnStoreCollectionChanged;
					foreach (var item in storeCollection.Cast<TreeGridItem>())
						item.Children.CollectionChanged -= OnChildrenCollectionChanged;
				}
			}

			if (sections != null && sections.Count > 0)
			{
				foreach (var section in sections)
				{
					section.DetachEvents(force: true);
				}
			}
		}

		private void AttachEvents(IDataStore<ITreeGridItem> value)
		{
			ObservableCollection<ITreeGridItem> storeCollection = null;
			if (value is TreeGridItemCollection collection) storeCollection = collection;
			if (value is TreeGridItem treeGridItem) storeCollection = treeGridItem.Children;

			if (storeCollection != null)
			{
				storeCollection.CollectionChanged += OnStoreCollectionChanged;
				foreach (var item in storeCollection.Cast<TreeGridItem>())
					item.Children.CollectionChanged += OnChildrenCollectionChanged;
			}
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
			if (item == null)
			{
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

		public bool ExpandRow(int row)
		{
			var args = new TreeGridViewItemCancelEventArgs(GetItemAtRow(row));
			treeDataStore.OnExpanding(args);
			if (args.Cancel)
				return false;
			args.Item.Expanded = true;
			treeDataStore.Refresh();
			treeDataStore.OnExpanded(new TreeGridViewItemEventArgs(args.Item));
			return true;
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
	}
}
