using System;
using Eto.Forms;
using Eto.Drawing;

namespace TestEtoApp
{
    public class MainForm : Form
    {
        static TreeGridItemCollection treeItems;
        TreeGridView treeControl;
        private int counter;

        public MainForm()
        {
            ClientSize = new Size(400, 350);

            Content = CreateContent();

            CreateMenu();
        }

        Control CreateContent()
        {
            var layout = new StackLayout();

            layout.Items.Add(new Button(AddSiblingAbove) { Text = "Add Sibling Above" });
            layout.Items.Add(new Button(AddSiblingBelow) { Text = "Add Sibling Below" });
            layout.Items.Add(new Button(AddChild) { Text = "Add Child" });
            layout.Items.Add(new Button(AddChildWithChildren) { Text = "Add Child With Children" });
            layout.Items.Add(new Button(Remove) { Text = "Remove" });
            layout.Items.Add(new Button(ReassignDataStore){ Text = "Reassign DataStore" });
           

            treeControl = new TreeGridView();
            treeControl.Columns.Add(new GridColumn
            {
                DataCell = new ImageTextCell(0, 1),
                AutoSize = true
            });
            treeControl.DataStore = GetItems();
            layout.Items.Add(treeControl);

            return layout;
        }

        private void ReassignDataStore(object sender, EventArgs e)
        {
            counter = 0;
            treeControl.DataStore = GetItems();
        }

        void AddSiblingAbove(object sender, EventArgs e)
        {
            var item = (TreeGridItem)treeControl.SelectedItem;
            if (item == null)
                return;

            var parent = (TreeGridItem)item.Parent;
            var collection = parent == null ? treeItems : parent.Children;

            var newItem = CreateItem();
            var i = collection.IndexOf(item);
            collection.Insert(i, newItem);
        }

        void AddSiblingBelow(object sender, EventArgs e)
        {
            var item = (TreeGridItem)treeControl.SelectedItem;
            if (item == null)
                return;

            var parent = (TreeGridItem)item.Parent;
            var collection = parent == null ? treeItems : parent.Children;

            var newItem = CreateItem();
            var i = collection.IndexOf(item);
            collection.Insert(i + 1, newItem);
        }

        void AddChild(object sender, EventArgs e)
        {
            var item = (TreeGridItem)treeControl.SelectedItem;
            if (item == null)
                return;

            var newItem = CreateItem();
            item.Children.Add(newItem);
        }

        private void AddChildWithChildren(object sender, EventArgs e)
        {
            var item = (TreeGridItem)treeControl.SelectedItem;
            if (item == null)
                return;

            var newItem = CreateItem();
            newItem.Children.Add(CreateItem());
            newItem.Children.Add(CreateItem());
            item.Children.Add(newItem);
        }

        void Remove(object sender, EventArgs e)
        {
            var item = (TreeGridItem)treeControl.SelectedItem;
            if (item == null)
                return;

            var parent = (TreeGridItem)item.Parent;
            var collection = parent == null ? treeItems : parent.Children;

            collection.Remove(item);
        }

        TreeGridItemCollection GetItems()
        {
            treeItems = new TreeGridItemCollection();

            for (var i = 0; i < 3; i++)
                treeItems.Add(CreateItem());

            return treeItems;
        }

        TreeGridItem CreateItem()
        {
            var item = new TreeGridItem(new object[] { null, $"Item{counter++}" });
            //{
            //    Expanded = true
            //};
            //for (var j = 0; j < 3; j++)
            //{
            //    var childItem = new TreeGridItem(null, $"Item{i}_{j}");
            //    item.Children.Add(childItem);
            //}
            return item;
        }

        void CreateMenu()
        {
            var quitCommand = new Command { MenuText = "Quit", Shortcut = Application.Instance.CommonModifier | Keys.Q };
            quitCommand.Executed += (sender, e) => Application.Instance.Quit();

            Menu = new MenuBar
            {
                QuitItem = quitCommand,
            };
        }
    }
}
