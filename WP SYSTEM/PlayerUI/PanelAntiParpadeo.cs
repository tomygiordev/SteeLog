using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;

namespace WP_Inventario
{
    public class PanelAntiParpadeo : TableLayoutPanel
    {
        public PanelAntiParpadeo()
        {
            this.DoubleBuffered = true;
        }
    }
}
