using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using Eto.Forms;

namespace Eto.CustomControls
{
	public class TreeControllerStore : ITreeGridStore<ITreeGridItem>, INotifyCollectionChanged, IList
	{
		internal Dictionary<int, ITreeGridItem> Cache { get; } = new Dictionary<int, ITreeGridItem>();

		public TreeControllerStore(ITreeHandler handler)
		{
			Handler = handler;
			RootTreeController = new TreeController(this) { Handler = handler };
		}

		ITreeHandler Handler { get; }

		TreeController RootTreeController { get; }

		internal void OnStoreCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var newItem in e.NewItems.Cast<ITreeGridItem>())
					{
						var row = IndexOf(newItem);
						//var row = e.NewStartingIndex; //.((TreeGridItemCollection)store).IndexOf(newItem); // TODO calculate using sections
						if (row < 0)
							return;
						OnTriggerCollectionChanged(
							new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItem, row)); // TODO use overload with collection
					}
					break;
				case NotifyCollectionChangedAction.Remove:
					foreach (var oldItem in e.OldItems.Cast<ITreeGridItem>())
					{
						var row = e.OldStartingIndex; //((TreeGridItemCollection)store).IndexOf(oldItem);
						if (row < 0)
							return;
						OnTriggerCollectionChanged(
							new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItem, row));
					}
					break;
				case NotifyCollectionChangedAction.Reset:
					break;
				case NotifyCollectionChangedAction.Replace:
					break;
				case NotifyCollectionChangedAction.Move:
				//break;
				default:
					throw new ArgumentOutOfRangeException();
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
				if (!Cache.TryGetValue(row, out item))
				{
					item = RootTreeController.GetItemAtRow(row);
					if (item != null)
						Cache[row] = item;
				}
				return item;
			}
		}

		public int Count => RootTreeController.Count;

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
		}

		public bool Contains(object value)
		{
			return true;
		}

		public int IndexOf(object value)
		{
			return RootTreeController.IndexOf(value as ITreeGridItem);
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
				yield return RootTreeController[i];
			}
		}

		#endregion


		#region Facade methods

		public void InitializeItems(ITreeGridStore<ITreeGridItem> value)
		{
			RootTreeController.InitializeItems(value);
			RootTreeController.CollectionChanged += OnStoreCollectionChanged;
		}

		public TreeController.TreeNode GetNodeAtRow(int row) => RootTreeController.GetNodeAtRow(row);

		public void ExpandToItem(ITreeGridItem value) => RootTreeController.ExpandToItem(value);

		public void ReloadData() => RootTreeController.ReloadData();

		public int LevelAtRow(int row) => RootTreeController.LevelAtRow(row);

		public bool CollapseRow(int row) => RootTreeController.CollapseRow(row);

		public bool IsExpanded(int row) => RootTreeController.IsExpanded(row);

		public bool ExpandRow(int row) => RootTreeController.ExpandRow(row);

		#endregion
	}
}
