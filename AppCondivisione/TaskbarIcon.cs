using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace AppCondivisione
{
    class TaskbarIcon
    {
        private NotifyIcon nIcon;
        private System.Windows.Forms.ContextMenu nIconMenu;

        public TaskbarIcon()
        {
            this.SetupTaskbarIcon();
        }

        private void SetupTaskbarIcon()
        {
            this.SetupContextMenu();
            this.nIcon = new NotifyIcon();
            this.nIcon.Icon = new System.Drawing.Icon("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\Common7\\IDE\\TextTemplate.ico");
            this.nIcon.Visible = true;

            this.nIcon.Click += new EventHandler(Notifier_Click);
            this.nIcon.ContextMenu = this.nIconMenu;
        }

        private void SetupContextMenu()
        {
            this.nIconMenu = new System.Windows.Forms.ContextMenu();
            this.nIconMenu.MenuItems.Add(new System.Windows.Forms.MenuItem("Primo", new EventHandler(Primo)));
        }

        public void Remove()
        {
            this.nIcon.Visible = false;
            this.nIcon.Dispose();
        }

        public void Primo(object sender, EventArgs e)
        {
            System.Windows.MessageBox.Show("primo");
        }

        public void Notifier_Click(object sender, EventArgs e)
        {
            
        }
    }
}
