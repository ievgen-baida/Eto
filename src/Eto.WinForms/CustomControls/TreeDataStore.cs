using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Eto.Forms;

namespace Eto.CustomControls
{
	public class TreeDataStore : ITreeGridStore<ITreeGridItem>, IList, INotifyCollectionChanged
	{
		private readonly ObservableCollection<ITreeGridItem> cache = new ObservableCollection<ITreeGridItem>();
		private readonly TreeController rootTreeController;

		public TreeDataStore(ITreeHandler handler)
		{
			rootTreeController = new TreeController(this, handler);
			cache.CollectionChanged += (s, e) => OnTriggerCollectionChanged(e);
		}

		internal void UpdateCache(ITreeGridStore<ITreeGridItem> source, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					var index = e.NewStartingIndex;
					foreach (var item in e.NewItems.Cast<ITreeGridItem>())
					{
						cache.Insert(index++, item);
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					// TODO use OldStartingIndex? from the end
					foreach (var item in e.OldItems.Cast<ITreeGridItem>())
					{
						cache.Remove(item);
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					OnTriggerCollectionChanged(e);
					break;
				case NotifyCollectionChangedAction.Replace:
					for (int i = 0; i < e.NewItems.Count; i++)
					{
						int row = IndexOf(e.OldItems[i]);
						cache.RemoveAt(row);
						cache.Insert(row, (ITreeGridItem)e.NewItems[i]);
					}
					break;
				case NotifyCollectionChangedAction.Move:
					OnTriggerCollectionChanged(e);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		/*
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
		*/

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

		public ITreeGridItem this[int row] => cache[row];

		public int Count => cache.Count;

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
			cache.Clear();
		}

		public bool Contains(object value)
		{
			return true;
		}

		public int IndexOf(object value)
		{
			var item = value as ITreeGridItem;

			var index = cache.IndexOf(item);
			if (index >= 0)
			{
				return index;
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
			Clear();
			rootTreeController.InitializeItems(value);
			///rootTreeController.CollectionChanged += UpdateCache;
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
