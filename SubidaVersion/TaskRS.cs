using System;
using System.Collections.Generic;
using System.Text;

namespace SubidaVersion
{
    // solo están los campos necesarios, la API nos devuelve mas campos, si se quieren parsear solo hace falta meterlos en la clase y listos
    public class TaskRS
    {
        public string expand { get; set; }
        public int startAt { get; set; }
        public int maxResults { get; set; }
        public int total { get; set; }
        public List<Issue> issues { get; set; }
    }

    public class Issue
    {
        public string id { get; set; }
        public string key { get; set; }
        public Field fields { get; set; }
    }

    public class Field
    {
        public string customfield_12500 { get; set; }
    }
}
