using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AppCondivisione
{
    class TaskbarIcon
    {
        private NotifyIcon nIcon;

        public TaskbarIcon()
        {
            this.SetupTaskbarIcon();
        }

        private void SetupTaskbarIcon()
        {
            this.nIcon = new NotifyIcon();
            this.nIcon.Icon = new System.Drawing.Icon("C:\\Program Files (x86)\\Microsoft Visual Studio\\2017\\Community\\Common7\\IDE\\TextTemplate.ico");
            this.nIcon.Visible = true;
        }

        public void Remove()
        {
            this.nIcon.Visible = false;
            this.nIcon.Dispose();
        }
    }
}
