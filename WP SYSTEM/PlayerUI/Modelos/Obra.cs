using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WP_Inventario.Modelos
{
    public class Obra
    {
        public Obra() { }
        public int id { get; set; }
        public string nombre { get; set; }
        public bool activa { get; set; }
    }
}
