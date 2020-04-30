using System;
using System.Collections.Generic;
using System.Text;

namespace SubidaVersion
{
    // solo están los campos necesarios, la API nos devuelve mas campos, si se quieren parsear solo hace falta meterlos en la clase y listos
    public class CommitRS
    {
        public int pagelen { get; set; }
        public List<Values> values { get; set; }
        public string next { get; set; }
    }

    public class Values
    {
        public Rendered rendered { get; set; }

        public string hash { get; set; }
        public Repository repository { get; set; }
        public string date { get; set; }
        public string message { get; set; }
        public string type { get; set; }
    }

    public class Rendered
    {
        public Message message { get; set; }
    }

    public class Message
    {
        public string raw { get; set; }
        public string markup { get; set; }
        public string html { get; set; }
        public string type { get; set; }
    }

    public class Repository
    {
        public string type { get; set; }
        public string name { get; set; }
        public string full_name { get; set; }
        public string uuid { get; set; }
    }
}
