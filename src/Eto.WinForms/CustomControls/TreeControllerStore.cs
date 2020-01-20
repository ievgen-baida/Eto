using System;
using System.Collections.Specialized;
using System.Linq;
using Eto.Forms;

namespace Eto.CustomControls
{
	public class TreeControllerStore : TreeController
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

	}
}
