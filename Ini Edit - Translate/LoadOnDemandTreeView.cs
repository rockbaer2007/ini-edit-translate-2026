using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    public class LoadOnDemandTreeView : TreeView
    {
        private bool _blnFakeNodes = true;

        public bool FakeNodes
        {
            get => _blnFakeNodes;
            set => _blnFakeNodes = value;
        }

        public event TreeViewCancelEventHandler LoadData;

        public bool FakeNode(TreeNode pTreeNodeToFake)
        {
            bool flag = false;
            if (pTreeNodeToFake != null && !pTreeNodeToFake.IsExpanded && pTreeNodeToFake.Nodes.Count == 0)
            {
                pTreeNodeToFake.Nodes.Add(new TreeNode("fake")
                {
                    Tag = "fake"
                });
                flag = true;
            }
            return flag;
        }

        protected override void OnBeforeExpand(TreeViewCancelEventArgs e)
        {
            if (_blnFakeNodes && e.Node.Nodes.Count > 0 && (e.Node.Nodes[0].Tag != null && e.Node.Nodes[0].Tag.ToString() == "fake"))
            {
                e.Node.Nodes.Clear();
                OnLoadData(e);
            }
            base.OnBeforeExpand(e);
        }

        protected override void OnAfterCollapse(TreeViewEventArgs e)
        {
            base.OnAfterCollapse(e);
            if (!_blnFakeNodes)
            {
                return;
            }

            e.Node.Nodes.Clear();
            e.Node.Nodes.Add(new TreeNode("fake")
            {
                Tag = "fake"
            });
        }

        protected virtual void OnLoadData(TreeViewCancelEventArgs e)
        {
            if (LoadData == null)
            {
                return;
            }

            LoadData(this, e);
        }
    }
}
