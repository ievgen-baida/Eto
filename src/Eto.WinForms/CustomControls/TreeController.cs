using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.Specialized;
using Eto.Forms;
using System.Collections;

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

		//int? countCache;
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
				if (sections == null) sections = new List<TreeController> ();
				return sections;
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
					//var previousItem = e.NewStartingIndex > 0 ? Store[e.NewStartingIndex - 1] : null;

					//if (previousItem != null)
					//{
					//	if (Sections[previousItem.startrow])
					//		e = new NotifyCollectionChangedEventArgs(e.Action, e.NewItems,
					//			previousSection.StartRow + previousSection.Count + 1);
					//}

					break;
				case NotifyCollectionChangedAction.Remove:
					//e = new NotifyCollectionChangedEventArgs(e.Action, e.OldItems, StartRow + e.OldStartingIndex);
					throw new Exception("OOps");
					break;
			}

			OnStoreCollectionChangedInternal(sender, e);
		}

		void OnStoreCollectionChangedInternal(object sender, NotifyCollectionChangedEventArgs e)
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

			OnTriggerCollectionChanged(e, (ITreeGridStore<ITreeGridItem>)sender);
		}

		void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			var childrenCollection = (ITreeGridStore<ITreeGridItem>)sender;

			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					var parentItem = childrenCollection[0].Parent;
					if (childrenCollection.Count == e.NewItems?.Count)
						ReloadItem(parentItem);
					if (parentItem.Expanded)
					{
						var addedChildren = e.NewItems.Cast<TreeGridItem>().ToArray();
						var startingIndex = treeDataStore.IndexOf(parentItem) + e.NewStartingIndex + 1;
						OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(e.Action, addedChildren, startingIndex), childrenCollection);
					}

					break;
				case NotifyCollectionChangedAction.Remove:
					var oldItemParent = ((ITreeGridItem) e.OldItems[0]).Parent;
					if (childrenCollection.Count == 0)
					{
						//oldParentItem.Expanded = false;
						RemoveSection(treeDataStore.IndexOf(oldItemParent));
						ReloadItem(oldItemParent);
					}

					if (oldItemParent.Expanded)
					{
						var removedChildren = e.OldItems.Cast<TreeGridItem>().ToArray();
						//var startingIndex = treeDataStore.IndexOf(parentItem) + e.NewStartingIndex + 1;
						OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(e.Action, removedChildren), childrenCollection);
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

		private void RemoveSection(int startRow)
		{
			var section = Sections.Single(s => s.StartRow == startRow);

			// TODO Check sub-sections?
			//for (int i = 0; i < section.Count; i++)
			//{
			//	var childTreeGridItem = section[i];

			//	if (childTreeGridItem is TreeGridItem treeGridItem)
			//		treeGridItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
			//}

			Sections.Remove(section);
		}

		void ReloadItem(ITreeGridItem item)
		{
			OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item));
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
				OnTriggerCollectionChanged(
					new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, list, StartRow));
			}

			//ResetCollection();
		}

		void ResetSections()
		{
			foreach (var treeController in Sections)
			{
				for (int i = 0; i < treeController.Count; i++)
				{
					var childTreeGridItem = treeController[i];

					if (childTreeGridItem is TreeGridItem treeGridItem)
						treeGridItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
				}
			}
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

		public ITreeGridItem this[int row] => treeDataStore[row];

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
				// TODO notify rows moved and added
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
			Sections.Sort ((x, y) => x.StartRow.CompareTo (y.StartRow));
			if (childController != null) {
				//ClearCache ();
				NotifySectionExpanded(childController);
				//ResetCollection ();
			}
			return children;
		}

		void NotifySectionExpanded(TreeController childController)
		{
			//var parentRow = childController.StartRow;
			var firstChildRow = childController.StartRow + 1;
			//OnStoreCollectionChanged(treeDataStore, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, this[parentRow], this[parentRow], parentRow));
			ReloadItem(childController.Store as TreeGridItem);

			OnStoreCollectionChangedInternal(treeDataStore, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (childController.Store as TreeGridItem).Children, firstChildRow));
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
			OnTriggerCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
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

			//ResetCollection ();

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
					foreach (var treeController in Sections.Where(r => r.StartRow == row).ToArray())
					{
						Sections.Remove(treeController);

                        NotifySectionCollapsed(treeController);
					}
				}
			}
			//treeDataStore.ClearCache();
		}

		void NotifySectionCollapsed(TreeController treeController)
		{
			var parentRow = treeController.StartRow;
			var firstChildRow = parentRow + 1;
			OnStoreCollectionChangedInternal(treeDataStore, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, this[parentRow], this[parentRow], parentRow));
			
			var childItems = new List<ITreeGridItem>();
			for (int i = firstChildRow; i < firstChildRow + treeController.Count; i++)
			{
				childItems.Add(this[i]);
			}
			OnStoreCollectionChangedInternal(treeDataStore, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, childItems, firstChildRow));
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

		protected virtual void OnTriggerCollectionChanged(NotifyCollectionChangedEventArgs args, ITreeGridStore<ITreeGridItem> source = null)
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
	}
}
