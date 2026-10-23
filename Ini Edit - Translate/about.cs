using System;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Forms;

namespace Ini_Edit___Translate
    {
    public partial class about : Form
        {
        public about()
            {
            InitializeComponent();
            this.Text = Ui.Text("aboutTitle");
            this.labelProductName.Text = AssemblyProduct;
            this.labelVersion.Text = String.Format("Version {0}", AssemblyVersion);
            //this.labelCopyright.Text = AssemblyCopyright;
            this.labelCompanyName.Text = AssemblyCompany;
        }

        private void button1_Click(object sender, EventArgs e)
            {
            Close();
            }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            {
            Process.Start(e.Link.LinkData as string);
            }

        private void about_Load(object sender, EventArgs e)
            {
            LinkLabel.Link link1 = new LinkLabel.Link
                {
                LinkData = "https://eepshopping.de"
                };
            }

        private void linkLabel3_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            {
            System.Diagnostics.Process.Start("mailto:rockbaer2007@live.de?subject=ini-Translator");
            }

        private void LinkLabel2_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
            {
            System.Diagnostics.Process.Start("skype:rockbaer2010?chat");
            }

        public string AssemblyTitle
            {
            get
                {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                if (attributes.Length > 0)
                    {
                    AssemblyTitleAttribute titleAttribute = (AssemblyTitleAttribute)attributes[0];
                    if (titleAttribute.Title != "")
                        {
                        return titleAttribute.Title;
                        }
                    }
                return System.IO.Path.GetFileNameWithoutExtension(Assembly.GetExecutingAssembly().CodeBase);
                }
            }

        public string AssemblyVersion
            {
            get
                {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString();
                }
            }

        public string AssemblyDescription
            {
            get
                {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                if (attributes.Length == 0)
                    {
                    return "";
                    }
                return ((AssemblyDescriptionAttribute)attributes[0]).Description;
                }
            }

        public string AssemblyProduct
            {
            get
                {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (attributes.Length == 0)
                    {
                    return "";
                    }
                return ((AssemblyProductAttribute)attributes[0]).Product;
                }
            }

        public string AssemblyCopyright
            {
            get
                {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                if (attributes.Length == 0)
                    {
                    return "";
                    }
                return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
                }
            }

        public string AssemblyCompany
            {
            get
                {
                object[] attributes = Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCompanyAttribute), false);
                if (attributes.Length == 0)
                    {
                    return "";
                    }
                return ((AssemblyCompanyAttribute)attributes[0]).Company;
                }
            }

        private void Button1_Click_1(object sender, EventArgs e)
            {
            this.Close();
            }

        private void labelCompanyName_Click(object sender, EventArgs e)
        {

        }

    }
    }
