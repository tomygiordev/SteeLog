using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SteeLog.Modelos
{
    public class Herramienta
    {
        public Herramienta() { }
        public int id { get; set; }
        public string nombre { get; set; }
        public string estado { get; set; }
        public int? id_obra { get; set; }
        public string nombre_obra { get; set; }
        public string observaciones { get; set; }
    }
}
