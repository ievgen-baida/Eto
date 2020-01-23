using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Eto.Forms;

namespace Eto.CustomControls
{
	public class TreeDataStore : ITreeGridStore<ITreeGridItem>, IList, INotifyCollectionChanged
	{
		private readonly Dictionary<int, ITreeGridItem> cache = new Dictionary<int, ITreeGridItem>();
		private readonly TreeController rootTreeController;

		public TreeDataStore(ITreeHandler handler)
		{
			rootTreeController = new TreeController(this, handler);
		}

		internal void ClearCache()
		{
			cache.Clear();
			rootTreeController.ClearCache();
		}

		internal void OnStoreCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					NotifyCollectionChanged(e.Action, e.NewItems.Cast<ITreeGridItem>().ToArray());
					break;
				case NotifyCollectionChangedAction.Remove:
					NotifyCollectionChanged(e.Action, e.OldItems.Cast<ITreeGridItem>().ToArray());
					break;
				case NotifyCollectionChangedAction.Reset:
					break;
				case NotifyCollectionChangedAction.Replace:
					// Hack to avoid clearing the cache
					int row = IndexOf(e.NewItems.Cast<ITreeGridItem>().Single());
					OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, e.OldItems.Cast<ITreeGridItem>().Single(), row));
					OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, e.NewItems.Cast<ITreeGridItem>().Single(), row));
					break;
				case NotifyCollectionChangedAction.Move:
					//break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		void NotifyCollectionChanged(NotifyCollectionChangedAction action, ITreeGridItem[] items)
		{
			if (action == NotifyCollectionChangedAction.Add)
				ClearCache();

			var rows = new int[items.Length];
			for (int i = 0; i < items.Length; i++)
			{
				rows[i] = IndexOf(items[i]);
			}

			if (action == NotifyCollectionChangedAction.Remove)
				ClearCache();

			for (int i = 0; i < items.Length; i++)
			{
				if (rows[i] < 0)
					continue;
				OnTriggerCollectionChanged(new NotifyCollectionChangedEventArgs(action, items[i], rows[i]));
			}
		}

		public event EventHandler<TreeGridViewItemCancelEventArgs> Expanding;
		public event EventHandler<TreeGridViewItemCancelEventArgs> Collapsing;
		public event EventHandler<TreeGridViewItemEventArgs> Expanded;
		public event EventHandler<TreeGridViewItemEventArgs> Collapsed;

		// TODO access modifier
		internal void OnExpanding(TreeGridViewItemCancelEventArgs e)
		{
			if (Expanding != null) Expanding(this, e);
		}

		internal void OnCollapsing(TreeGridViewItemCancelEventArgs e)
		{
			if (Collapsing != null) Collapsing(this, e);
		}

		internal void OnExpanded(TreeGridViewItemEventArgs e)
		{
			if (Expanded != null) Expanded(this, e);
		}

		internal void OnCollapsed(TreeGridViewItemEventArgs e)
		{
			if (Collapsed != null) Collapsed(this, e);
		}


		#region NotifyColllectionChanged implementation

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		protected virtual void OnTriggerCollectionChanged(NotifyCollectionChangedEventArgs args) => CollectionChanged?.Invoke(this, args);

		#endregion


		#region ITreeGridStore<ITreeGridItem> implementation

		public ITreeGridItem this[int row]
		{
			get
			{
				ITreeGridItem item;
				if (!cache.TryGetValue(row, out item))
				{
					item = rootTreeController.GetItemAtRow(row);
					if (item != null)
						cache[row] = item;
				}
				return item;
			}
		}

		public int Count => rootTreeController.Count;

		#endregion


		#region IList implementation

		object IList.this[int index]
		{
			get { return this[index]; }
			set
			{

			}
		}

		public int Add(object value)
		{
			return 0;
		}

		public void Clear()
		{

			ClearCache();
		}

		public bool Contains(object value)
		{
			return true;
		}

		public int IndexOf(object value)
		{
			var item = value as ITreeGridItem;

			if (cache.ContainsValue(item))
			{
				var found = cache.First(r => ReferenceEquals(item, r.Value));
				return found.Key;
			}
			for (int i = 0; i < Count; i++)
			{
				if (ReferenceEquals(this[i], item))
					return i;
			}
			return -1;
		}

		public void Insert(int index, object value)
		{

		}

		public bool IsFixedSize
		{
			get { return false; }
		}

		public bool IsReadOnly
		{
			get { return false; }
		}

		public void Remove(object value)
		{

		}

		public void RemoveAt(int index)
		{

		}

		public void CopyTo(Array array, int index)
		{
			throw new NotImplementedException();
		}

		public bool IsSynchronized
		{
			get { return false; }
		}

		public object SyncRoot
		{
			get { return this; }
		}

		public IEnumerator GetEnumerator()
		{
			for (int i = 0; i < Count; i++)
			{
				yield return rootTreeController[i];
			}
		}

		#endregion


		#region Facade methods

		public void InitializeItems(ITreeGridStore<ITreeGridItem> value)
		{
			rootTreeController.InitializeItems(value);
			rootTreeController.CollectionChanged += OnStoreCollectionChanged;
		}

		public TreeController.TreeNode GetNodeAtRow(int row) => rootTreeController.GetNodeAtRow(row);

		public void ExpandToItem(ITreeGridItem value) => rootTreeController.ExpandToItem(value);

		public void ReloadData() => rootTreeController.ReloadData();

		public int LevelAtRow(int row) => rootTreeController.LevelAtRow(row);

		public bool CollapseRow(int row) => rootTreeController.CollapseRow(row);

		public bool IsExpanded(int row) => rootTreeController.IsExpanded(row);

		public bool ExpandRow(int row) => rootTreeController.ExpandRow(row);

		#endregion
	}
}
