using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;

namespace Ini_Edit___Translate
{
    public class DirectoryTreeViewOnDemand : LoadOnDemandTreeView, ISupportInitialize
    {
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public new TreeNodeCollection Nodes => base.Nodes;

        protected override void OnLoadData(TreeViewCancelEventArgs e)
        {
            base.OnLoadData(e);
            if (e.Action != TreeViewAction.Expand)
            {
                return;
            }

            try
            {
                DirectoryInfo directoryInfo = new DirectoryInfo(e.Node.FullPath);
                if (directoryInfo == null)
                {
                    return;
                }

                foreach (FileSystemInfo fileSystemInfo in directoryInfo.GetFileSystemInfos())
                {
                    Type type = fileSystemInfo.GetType();
                    if (type == typeof(DirectoryInfo))
                    {
                        TreeNode treeNode = new TreeNode(fileSystemInfo.Name);
                        if (ImageList != null && ImageList.Images.Count > 0)
                        {
                            treeNode.ImageIndex = 0;
                            treeNode.SelectedImageIndex = 1;
                        }
                        FakeNode(treeNode);
                        e.Node.Nodes.Add(treeNode);
                    }
                    else if (type == typeof(FileInfo))
                    {
                        FileInfo fileInfo = (FileInfo)fileSystemInfo;
                        string we = Path.GetExtension(fileInfo.FullName);
                        if (we == ".ini")
                        {
                            {
                                if (fileInfo.Name == "install.ini" || fileInfo.Name == "Install.ini")
                                {

                                }
                                else
                                {
                                    TreeNode node = new TreeNode(fileInfo.Name);
                                    if (ImageList != null && ImageList.Images.Count > 2)
                                    {
                                        node.ImageIndex = 2;
                                        node.SelectedImageIndex = 2;
                                    }
                                    e.Node.Nodes.Add(node);
                                }

                            }

                        }
                    }
                }
            }
            catch
            {
                e.Cancel = true;
                FakeNode(e.Node);
            }
        }

        public void BeginInit()
        {
            Nodes.Clear();
        }

        public void EndInit()
        {
            TreeNode treeNode = new TreeNode(Properties.Settings.Default.pathdir);
            if (ImageList != null && ImageList.Images.Count > 1)
            {
                treeNode.ImageIndex = 1;
                treeNode.SelectedImageIndex = 1;
            }
            FakeNode(treeNode);
            Nodes.Add(treeNode);
        }
    }
}
