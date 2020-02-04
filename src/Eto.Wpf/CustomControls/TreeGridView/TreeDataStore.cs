using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Diagnostics;
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

	public class TreeDataStore : ITreeGridStore<ITreeGridItem>, IList, INotifyCollectionChanged
	{
		private readonly ITreeHandler handler;
		private readonly ObservableCollection<ITreeGridItem> cache;
		private readonly TreeController controller;

		public TreeDataStore(ITreeHandler handler)
		{
			this.handler = handler;
			controller = new TreeController(this);
			cache = new ObservableCollection<ITreeGridItem>();
			cache.CollectionChanged += (s, e) => OnTriggerCollectionChanged(e);
		}

		internal void Refresh(bool force = false)
		{
			// Reset/Clear
			if (force)
			{
				Debug.WriteLine("Clear");
				cache.Clear();
			}

			controller.ResetSections();

			int count = controller.Count;
			var items = new ITreeGridItem[count];
			for (int index = 0; index < count; index++)
			{
				items[index] = controller[index];
			}

			for (int index = 0; index < items.Length; index++)
			{
				var item = items[index];

				// Add
				if (index == cache.Count)
				{
					Debug.WriteLine($"Add at {index}");
					cache.Insert(index, item);
					continue;
				}

				var cachedItemAtIndex = cache[index];
				if (ReferenceEquals(item, cachedItemAtIndex))
				{
					continue;
				}

				// Remove
				if (!items.Contains(cachedItemAtIndex))
				{
					Debug.WriteLine($"Remove at {index}");
					cache.RemoveAt(index);
				}

				int itemIndexInCache = cache.IndexOf(item);

				// Insert
				if (itemIndexInCache == -1)
				{
					Debug.WriteLine($"Insert at {index}");
					cache.Insert(index, item);
					continue;
				}

				// Move
				if (itemIndexInCache != index)
				{
					Debug.WriteLine($"Move from {itemIndexInCache} to {index}");
					cache.Move(itemIndexInCache, index);
				}
			}

			// Trim
			while (cache.Count > count)
			{
				Debug.WriteLine($"Trim at {cache.Count - 1}");
				cache.RemoveAt(cache.Count - 1);
			}

			// Temp: Consistency check
			for (int index = 0; index < items.Length; index++)
			{
				if (!ReferenceEquals(cache[index], items[index]))
					throw new ApplicationException($"Mismatch at index {index}.");
			}

			Debug.WriteLine("Refresh completed.");
		}

		internal void RefreshItem(ITreeGridItem item)
		{
			if (item == null) return;

			int row = IndexOf(item);
			if (row == -1) return;

			cache.RemoveAt(row);
			cache.Insert(row, item);
		}

		public event EventHandler<TreeGridViewItemCancelEventArgs> Expanding;
		public event EventHandler<TreeGridViewItemCancelEventArgs> Collapsing;
		public event EventHandler<TreeGridViewItemEventArgs> Expanded;
		public event EventHandler<TreeGridViewItemEventArgs> Collapsed;

		void OnExpanding(TreeGridViewItemCancelEventArgs e)
		{
			Expanding?.Invoke(this, e);
		}

		void OnCollapsing(TreeGridViewItemCancelEventArgs e)
		{
			Collapsing?.Invoke(this, e);
		}

		void OnExpanded(TreeGridViewItemEventArgs e)
		{
			Expanded?.Invoke(this, e);
		}

		void OnCollapsed(TreeGridViewItemEventArgs e)
		{
			Collapsed?.Invoke(this, e);
		}

		#region INotifyCollectionChanged implementation

		public event NotifyCollectionChangedEventHandler CollectionChanged;

		void OnTriggerCollectionChanged(NotifyCollectionChangedEventArgs args) => CollectionChanged?.Invoke(this, args);

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
			controller.InitializeItems(value);
			Refresh(force: true);
		}

		public TreeController.TreeNode GetNodeAtRow(int row) => controller.GetNodeAtRow(row);

		public void ExpandParentsOf(ITreeGridItem item)
		{
			var parents = item.GetParents().Reverse();

			foreach (var parent in parents) {
				var row = IndexOf(parent);
				if (row >= 0 && !parent.Expanded)
					ExpandRow (row);
			}
		}

		public void ReloadData()
		{
			handler.PreResetTree();
			Refresh(force: true);
			handler.PostResetTree();
		}

		public int LevelAtRow(int row)
		{
			if (row < cache.Count && cache[row] != null)
				return cache[row].GetParents().Count();

			return 0;
		}

		public bool CollapseRow(int row)
		{
			var item = (TreeGridItem) controller.GetItemAtRow(row);
			var args = new TreeGridViewItemCancelEventArgs(item);
			OnCollapsing(args);

			if (args.Cancel)
				return false;
			var shouldSelect = !handler.AllowMultipleSelection && item.IsChildOf(handler.SelectedItem);
			item.Expanded = false;

			OnCollapsed(new TreeGridViewItemEventArgs(item));
			Refresh();

			if (shouldSelect)
				handler.SelectRow(row);

			return true;
		}

		public bool IsExpanded(int row)
		{
			var item = this[row];
			return item.Expandable && item.Expanded;
		}

		public bool ExpandRow(int row)
		{
			var item = controller.GetItemAtRow(row);
			var cancellableEvent = new TreeGridViewItemCancelEventArgs(item);
			OnExpanding(cancellableEvent);

			if (cancellableEvent.Cancel)
				return false;
			item.Expanded = true;
			
			Refresh();
			OnExpanded(new TreeGridViewItemEventArgs(item));
			
			return true;
		}

		#endregion
	}
}
