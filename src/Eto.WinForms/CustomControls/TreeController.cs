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

		ExtendedObservableCollection<TreeController> sections;
		TreeController parent;
		ITreeGridStore<ITreeGridItem> store;

		internal TreeController(TreeDataStore treeDataStore, ITreeHandler handler)
		{
			this.treeDataStore = treeDataStore;
			this.handler = handler;
		}

		int StartRow { get; set; }

		ExtendedObservableCollection<TreeController> Sections
		{
			get
			{
				if (sections == null)
				{
					sections = new ExtendedObservableCollection<TreeController>();
					sections.CollectionChanged += SectionsCollectionChanged;
				}
				return sections;
			}
		}

		private void SectionsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					Sections.Sort((x, y) => x.StartRow.CompareTo(y.StartRow));
					TreeController newSection = e.NewItems.Cast<TreeController>().Single();
					var addedItems = new List<ITreeGridItem>();
					// TODO uncomment for indexer
					//for (int i = newSection.StartRow + 1; i < newSection.StartRow + 1 + newSection.Count; i++)
					for (int i = 0; i < newSection.Count; i++)
					{
						addedItems.Add(newSection.Store[i]); // TODO newSection[i] => fix indexer should work on controller again
					}
					ReloadItem(GetItemAtRow(newSection.StartRow));
					UpdateDataStore(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, addedItems, newSection.StartRow + 1));
					//treeDataStore.FixRowNumbers();
					break;
				case NotifyCollectionChangedAction.Remove:
					treeDataStore.FixRowNumbers();

					TreeController removedSection = e.OldItems.Cast<TreeController>().Single();
					var removedItems = new List<ITreeGridItem>();
					for (int i = removedSection.StartRow + 1; i < removedSection.StartRow + 1 + removedSection.Count; i++)
					{
						removedItems.Add(removedSection[i]); // for this indexer should work on controller again
					}
					ReloadItem(GetItemAtRow(removedSection.StartRow));
					UpdateDataStore(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
					break;
				case NotifyCollectionChangedAction.Reset: // Clear
					//foreach (var treeController in e.OldItems.Cast<TreeController>())
					//{
					//	if (childTreeGridItem is TreeGridItem treeGridItem)
					//		treeGridItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
					//}
					break;
			}
		}

		ITreeGridStore<ITreeGridItem> Store
		{
			get => store;
			set
			{
                if (store is TreeGridItemCollection oldCollection)
                {
	                oldCollection.CollectionChanged -= OnStoreCollectionChanged;
					// TODO Do in recursively
	                foreach (var childTreeGridItem in oldCollection)
	                {
		                if (childTreeGridItem is TreeGridItem treeGridItem)
			                treeGridItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
	                }
				}
				if (value is TreeGridItemCollection newCollection)
				{
					newCollection.CollectionChanged += OnStoreCollectionChanged;
					foreach (var childTreeGridItem in newCollection)
					{
						if (childTreeGridItem is TreeGridItem treeGridItem)
							treeGridItem.Children.CollectionChanged += OnChildrenCollectionChanged;
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
					treeDataStore.FixRowNumbers();
					foreach (var newItem in e.NewItems.Cast<TreeGridItem>())
						newItem.Children.CollectionChanged += OnChildrenCollectionChanged;
					break;
				case NotifyCollectionChangedAction.Remove:
					treeDataStore.FixRowNumbers();
					foreach (var oldItem in e.OldItems.Cast<TreeGridItem>())
						oldItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
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

			UpdateDataStore(e, (ITreeGridStore<ITreeGridItem>)sender);
		}

		void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			treeDataStore.FixRowNumbers();
			
			var childrenCollection = (ITreeGridStore<ITreeGridItem>)sender;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var newItem in e.NewItems.Cast<ITreeGridItem>().ToArray())
					{
						ReloadItem(newItem.Parent);
						if (GetParents(newItem).All(item => item.Expanded))
						{
							var row = IndexOf(newItem); // we need to lookup in the tree controller as store is obsolete at this point
							UpdateDataStore(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, row), childrenCollection); // WHY childrenCollection
						}
					}

					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (var removedItem in e.OldItems.Cast<ITreeGridItem>().ToArray())
					{
						ReloadItem(removedItem.Parent);
						if (GetParents(removedItem).All(item => item.Expanded))
							UpdateDataStore(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItem));
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
		
		private int IndexOf(ITreeGridItem item)
		{
			throw new NotImplementedException();
		}

		void ReloadItem(ITreeGridItem item)
		{
			UpdateDataStore(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item));
		}

		public void InitializeItems (ITreeGridStore<ITreeGridItem> store)
		{
			if (sections != null)
				sections.Clear ();
			Store = store;

			ResetSections();

			if (store is IList list)
			{
				//var list = store as IList ?? ((TreeGridItem) store).Children;
				//var index = store is IList ? StartRow : StartRow + 1;
				UpdateDataStore(
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, StartRow));
			}

			//ResetCollection();
		}

		void ResetSections()
		{
			Sections.Clear();
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

		public void ReloadData()
		{
			treeDataStore.Clear();
			ResetSections();
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
						return section.LevelAtRow(row - section.StartRow - 1) + 1;
					}
					row -= count;
				}
			}
			return 0;
		}

		public ITreeGridItem this[int row] => treeDataStore[row]; // TODO do not look at the store?

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
			ExpandRowInternal (row);
			treeDataStore.OnExpanded(new TreeGridViewItemEventArgs(args.Item));
			return true;
		}

		ITreeGridStore<ITreeGridItem> ExpandRowInternal (int row)
		{
			ITreeGridStore<ITreeGridItem> children = null;
			TreeController childController = null;
			if (sections == null || sections.Count == 0) {
				children = (ITreeGridStore<ITreeGridItem>)Store [row];
				childController = new TreeController(treeDataStore, handler) { StartRow = row, parent = this };
				childController.InitializeItems(children);
				Sections.Add (childController);
			}
			else {
				bool addTop = true;
				foreach (var section in sections)
				{
					if (row <= section.StartRow)
					{
						break;
					}
					if (row <= section.StartRow + section.Count)
					{
						children = section.ExpandRowInternal(row - section.StartRow - 1);
						addTop = false;
						break;
					}
					row -= section.Count;
				}
				if (addTop && row < Store.Count) {
					children = (ITreeGridStore<ITreeGridItem>)Store [row];
					childController = new TreeController(treeDataStore, handler) { StartRow = row, parent = this };
					childController.InitializeItems(children);
					Sections.Add (childController);
				}
			}
			return children;
		}

		//void NotifySectionExpanded(TreeController childController)
		//{
		//	treeDataStore.RefreshRowNumbers();

		//	//var parentRow = childController.StartRow;
		//	var firstChildRow = childController.StartRow + 1;
		//	//OnStoreCollectionChanged(treeDataStore, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, this[parentRow], this[parentRow], parentRow));
		//	ReloadItem(childController.Store as TreeGridItem);

		//	OnStoreCollectionChangedInternal(treeDataStore, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (childController.Store as TreeGridItem).Children, firstChildRow));
		//}
		
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
			UpdateDataStore (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
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
						section.CollapseSection(row - section.StartRow - 1);
						break;
					}
					row -= section.Count;
				}

				if (addTop && row < Store.Count)
				{
					var treeController = Sections.Single(r => r.StartRow == row);
					Sections.Remove(treeController);
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

		void UpdateDataStore(NotifyCollectionChangedEventArgs args, ITreeGridStore<ITreeGridItem> source = null)
		{
			treeDataStore.UpdateCache(source ?? this, args);
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

		public void RefreshStartRow(int startRow)
		{
			StartRow = startRow;
			ResetSections();

			//for (int i = startRow; i < Count; i++)
			//{
			//	if (Store != null)
			//	{
			//		for (int row = 0; row < Store.Count; row++)
			//		{
			//			var item = Store[row];
			//			if (item.Expanded)
			//			{
			//				var section = Sections.SingleOrDefault(s => s.Item == item);

			//				var children = (ITreeGridStore<ITreeGridItem>)item;
			//				var section = new TreeController(treeDataStore, handler) { StartRow = row, parent = this };
			//				section.InitializeItems(children);
			//				Sections.Add(section);
			//			}
			//		}
			//	}


			//	foreach (var section in sections)
			//	{
			//	}
			//}
		}
	}
}
