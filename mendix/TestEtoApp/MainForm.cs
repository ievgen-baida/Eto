using System;
using Eto.Forms;
using Eto.Drawing;

namespace TestEtoApp
{
    public class MainForm : Form
    {
        static TreeGridItemCollection treeItems;
        TreeGridView treeControl;

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
            layout.Items.Add(new Button(Remove) { Text = "Remove" });

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

        void AddSiblingAbove(object sender, EventArgs e)
        {
            var item = (TreeGridItem)treeControl.SelectedItem;
            if (item == null)
                return;

            var parent = (TreeGridItem)item.Parent;
            var collection = parent == null ? treeItems : parent.Children;

            var newItem = CreateItem(123);
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

            var newItem = CreateItem(123);
            var i = collection.IndexOf(item);
            collection.Insert(i + 1, newItem);
        }

        void AddChild(object sender, EventArgs e)
        {
            var item = (TreeGridItem)treeControl.SelectedItem;
            if (item == null)
                return;

            var newItem = CreateItem(888);
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

        static TreeGridItemCollection GetItems()
        {
            treeItems = new TreeGridItemCollection();

            for (var i = 0; i < 3; i++)
                treeItems.Add(CreateItem(i));

            return treeItems;
        }

        static TreeGridItem CreateItem(int i)
        {
            var item = new TreeGridItem(new object[] { null, $"Item{i}" });
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
