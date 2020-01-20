using System;
using System.Linq;
using Eto.Forms;
using Eto.Drawing;

namespace TestEtoApp
{
    public class MainForm : Form
    {
        private static TreeGridItemCollection treeItems;
        private TreeGridView treeControl;

        public MainForm()
        {
            ClientSize = new Size(400, 350);

            Content = CreateContent();

            CreateMenu();
        }

        Control CreateContent()
        {
            var layout = new StackLayout();

            layout.Items.Add(new Button(Add) { Text = "Add Parent" });
            layout.Items.Add(new Button(AddChild) { Text = "Add Child" });
            layout.Items.Add(new Button(Remove) { Text = "Remove" });

            treeControl = new TreeGridView();
            treeControl.Columns.Add(GetColumn());
            treeControl.DataStore = GetItems();
            layout.Items.Add(treeControl);

            return layout;
        }

        private void Add(object sender, EventArgs e)
        {
            var item = CreateItem(999);
            treeItems.Add(item);
            //treeControl.ReloadItem(item);
            //treeControl.ReloadData();
        }

        private void AddChild(object sender, EventArgs e)
        {
            var item = CreateItem(888);
            ((TreeGridItem)treeItems.Last()).Children.Add(item);
            ((TreeGridItem) treeItems.Last()).Expanded = true;
            //treeControl.ReloadItem(item);
            //treeControl.ReloadData();
        }

        private void Remove(object sender, EventArgs e)
        {
            treeItems.RemoveAt(5);
            //treeControl.ReloadData();
        }

        GridColumn GetColumn()
        {
            return new GridColumn
            {
                DataCell = new ImageTextCell(0, 1),
                AutoSize = true
            };
        }

        private static TreeGridItemCollection GetItems()
        {
            treeItems = new TreeGridItemCollection();

            for (var i = 0; i < 3; i++)
                treeItems.Add(CreateItem(i));

            return treeItems;
        }

        private static TreeGridItem CreateItem(int i)
        {
            var item = new TreeGridItem(new object[] { null, $"Item{i}" })
            {
                Expanded = true
            };
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
