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

	public class TreeController : ITreeGridStore<ITreeGridItem>, INotifyCollectionChanged
	{
		int? countCache;
		List<TreeController> sections;
		TreeController parent;
		ITreeGridStore<ITreeGridItem> store;

		internal TreeController(TreeControllerStore rootTreeController)
		{
			RootTreeController = rootTreeController;
		}

		TreeControllerStore RootTreeController { get; }

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

		private void OnStoreCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var newItem in e.NewItems.Cast<TreeGridItem>()) 
						newItem.Children.CollectionChanged += OnChildrenCollectionChanged;
					ClearCache();
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (var oldItem in e.OldItems.Cast<TreeGridItem>()) 
						oldItem.Children.CollectionChanged -= OnChildrenCollectionChanged;
					ClearCache();
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

			RootTreeController.OnStoreCollectionChanged(sender, e);
		}

		private void OnChildrenCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			//var section = GetSectionOfChild(e.NewStartingIndex);
			ReloadItem(((TreeGridItemCollection)sender)[0].Parent);
			// TODO fix indices
			//var newE = new NotifyCollectionChangedEventArgs(e.Action, e.NewItems[0], countCache.Value);

			//RootTreeController.OnStoreCollectionChanged(sender, newE);
		}

		void ReloadItem(ITreeGridItem item)
		{
			var row = IndexOf(item);
			if (row < 0)
				return;
			OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, row));
			OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, row));
		}

		public ITreeHandler Handler { get; set; }

		public void InitializeItems (ITreeGridStore<ITreeGridItem> store)
		{
			ClearCache ();
			if (sections != null)
				sections.Clear ();
			Store = store;

			ResetSections();
			ResetCollection ();
		}

		void ResetSections()
		{
			foreach (var treeController in Sections)
			{
				treeController.CollectionChanged -= OnStoreCollectionChanged;
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
						var section = new TreeController(RootTreeController) { StartRow = row, Handler = Handler, parent = this };
						section.InitializeItems(children);
						Sections.Add(section);
					}
				}
			}
		}

		public void ReloadData()
		{
			ClearCache();
			ResetSections();
			ResetCollection();
		}

		void ClearCache ()
		{
			countCache = null;
			RootTreeController.Cache.Clear ();
		}

		public int IndexOf (ITreeGridItem item)
		{
			if (RootTreeController.Cache.ContainsValue(item))
			{
				var found = RootTreeController.Cache.First(r => ReferenceEquals(item, r.Value));
				return found.Key;
			}
			for (int i = 0; i < Count; i++)
			{
				if (ReferenceEquals(this[i], item))
					return i;
			}
			return -1;
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

		public ITreeGridItem this[int row] => RootTreeController[row];

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
			RootTreeController.OnExpanding(args);
			if (args.Cancel)
				return false;
			args.Item.Expanded = true;
			ExpandRowInternal (row);
			RootTreeController.OnExpanded(new TreeGridViewItemEventArgs(args.Item));
			return true;
		}

		ITreeGridStore<ITreeGridItem> ExpandRowInternal (int row)
		{
			ITreeGridStore<ITreeGridItem> children = null;
			TreeController childController = null;
			if (sections == null || sections.Count == 0) {
				children = (ITreeGridStore<ITreeGridItem>)Store [row];
				childController = new TreeController(RootTreeController) { StartRow = row, Handler = Handler, parent = this };
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
					childController = new TreeController(RootTreeController) { StartRow = row, Handler = Handler, parent = this };
					childController.InitializeItems(children);
					Sections.Add (childController);
					// TODO notify rows moved and added
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
			var parentRow = childController.StartRow;
			var firstChildRow = parentRow + 1;
			OnStoreCollectionChanged(RootTreeController, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, this[parentRow], this[parentRow], parentRow));

			var childItems = new List<ITreeGridItem>();
			for (int i = firstChildRow; i < firstChildRow + childController.Count; i++)
			{
				childItems.Add(this[i]);
			}
			OnStoreCollectionChanged(RootTreeController, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, childItems, firstChildRow));
		}

		bool ChildIsSelected (ITreeGridItem item)
		{
			var node = Handler.SelectedItem;

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
				Handler.PreResetTree ();
			OnTriggerCollectionChanged (new NotifyCollectionChangedEventArgs (NotifyCollectionChangedAction.Reset));
			if (parent == null)
				Handler.PostResetTree ();
		}

		public bool CollapseRow (int row)
		{
			var args = new TreeGridViewItemCancelEventArgs (GetItemAtRow (row));
			RootTreeController.OnCollapsing(args);
			if (args.Cancel)
				return false;
			var shouldSelect = !Handler.AllowMultipleSelection && ChildIsSelected (args.Item);
			args.Item.Expanded = false;
			RootTreeController.OnCollapsed(new TreeGridViewItemEventArgs(args.Item));
			CollapseSection (row);

			//ResetCollection ();

			if (shouldSelect)
				Handler.SelectRow (row);

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
						treeController.CollectionChanged -= OnStoreCollectionChanged;
						Sections.Remove(treeController);

                        NotifySectionCollapsed(treeController);
					}
				}
			}
			ClearCache ();
		}

		void NotifySectionCollapsed(TreeController treeController)
		{
			var parentRow = treeController.StartRow;
			var firstChildRow = parentRow + 1;
			OnStoreCollectionChanged(RootTreeController, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, this[parentRow], this[parentRow], parentRow));

			var childItems = new List<ITreeGridItem>();
			for (int i = firstChildRow; i < firstChildRow + treeController.Count; i++)
			{
				childItems.Add(this[i]);
			}
			OnStoreCollectionChanged(RootTreeController, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, childItems, firstChildRow));
		}

		public int Count
		{
			get
			{
				if (Store == null)
					return 0;
				if (countCache != null)
					return countCache.Value;
				if (sections != null)
					countCache = Store.Count + sections.Sum (r => r.Count);
				else
					countCache = Store.Count;
				return countCache.Value;
			}
		}

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		protected virtual void OnTriggerCollectionChanged (NotifyCollectionChangedEventArgs args)
		{
			if (CollectionChanged != null)
				CollectionChanged (this, args);
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
				var row = IndexOf (item);
				if (row >= 0 && !IsExpanded(row))
					ExpandRow (row);
			}
		}
	}
}
