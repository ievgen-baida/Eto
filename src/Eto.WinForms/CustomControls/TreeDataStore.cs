using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using Eto.Forms;

namespace Eto.CustomControls
{
	public class TreeDataStore : ITreeGridStore<ITreeGridItem>, IList, INotifyCollectionChanged
	{
		private readonly ObservableCollection<ITreeGridItem> cache;
		private readonly TreeController controller;

		public TreeDataStore(ITreeHandler handler)
		{
			controller = new TreeController(this, handler);
			cache = new ObservableCollection<ITreeGridItem>();
			cache.CollectionChanged += (s, e) => OnTriggerCollectionChanged(e);
		}

		internal void UpdateCache(NotifyCollectionChangedEventArgs e)
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

		public event EventHandler<TreeGridViewItemCancelEventArgs> Expanding;
		public event EventHandler<TreeGridViewItemCancelEventArgs> Collapsing;
		public event EventHandler<TreeGridViewItemEventArgs> Expanded;
		public event EventHandler<TreeGridViewItemEventArgs> Collapsed;

		// TODO access modifier
		internal void OnExpanding(TreeGridViewItemCancelEventArgs e)
		{
			Expanding?.Invoke(this, e);
		}
	
		internal void OnCollapsing(TreeGridViewItemCancelEventArgs e)
		{
			Collapsing?.Invoke(this, e);
		}

		internal void OnExpanded(TreeGridViewItemEventArgs e)
		{
			Expanded?.Invoke(this, e);
		}

		internal void OnCollapsed(TreeGridViewItemEventArgs e)
		{
			Collapsed?.Invoke(this, e);
		}

		#region INotifyCollectionChanged implementation

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		protected void OnTriggerCollectionChanged(NotifyCollectionChangedEventArgs args) => CollectionChanged?.Invoke(this, args);

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
			// TODO Clear tree controllers recursively, then clearing the cache is not required
			// NOTE It is actually used?
			//rootTreeController.Clear();
			cache.Clear();
		}

		public bool Contains(object value)
		{
			return cache.Contains(value);
		}

		public int IndexOf(object value)
		{
			return cache.IndexOf(value as ITreeGridItem);
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
			get { return true; }
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
				yield return this[i];
			}
		}

		#endregion

		#region Facade methods

		public void InitializeItems(ITreeGridStore<ITreeGridItem> value)
		{
			cache.Clear();
			controller.InitializeItems(value, true);
		}

		public TreeController.TreeNode GetNodeAtRow(int row) => controller.GetNodeAtRow(row);

		public void ExpandToItem(ITreeGridItem value) => controller.ExpandToItem(value);

		public void ReloadData() => controller.ReloadData();

		public int LevelAtRow(int row) => controller.LevelAtRow(row);

		public bool CollapseRow(int row) => controller.CollapseRow(row);

		public bool IsExpanded(int row) => controller.IsExpanded(row);

		public bool ExpandRow(int row) => controller.ExpandRow(row);

		#endregion

		public void RebuildRows()
		{
			controller.ResetSections(false);
		}
	}
}
