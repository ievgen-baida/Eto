using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using Eto.Forms;

namespace Eto.CustomControls
{
	public class TreeControllerStore : TreeController, IList
	{
		public TreeControllerStore()
		{
			RootTreeController = this;
		}
		
		internal void OnStoreCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			switch (e.Action)
			{
				case NotifyCollectionChangedAction.Add:
					foreach (var newItem in e.NewItems.Cast<ITreeGridItem>())
					{
						var row = e.NewStartingIndex; //.((TreeGridItemCollection)store).IndexOf(newItem); // TODO calculate using sections
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
				//break;
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
		internal virtual void OnExpanding(TreeGridViewItemCancelEventArgs e)
		{
			if (Expanding != null) Expanding(this, e);
		}

		internal virtual void OnCollapsing(TreeGridViewItemCancelEventArgs e)
		{
			if (Collapsing != null) Collapsing(this, e);
		}

		internal virtual void OnExpanded(TreeGridViewItemEventArgs e)
		{
			if (Expanded != null) Expanded(this, e);
		}

		internal virtual void OnCollapsed(TreeGridViewItemEventArgs e)
		{
			if (Collapsed != null) Collapsed(this, e);
		}

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

		int IList.IndexOf(object value)
		{
			return IndexOf(value as ITreeGridItem);
		}

		public void Insert(int index, object value)
		{

		}

		public bool IsFixedSize
		{
			get { return true; }
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
				yield return this[i];
			}
		}
	}
}
