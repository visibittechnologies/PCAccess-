using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace PCAccess.Models
{
    public class ExceptionLoggerModel
    {
        public int Id { get; set; }
        public string ExceptionMessage { get; set; }
        public string ControllerName { get; set; }
        public string ExceptionStackTrace { get; set; }
        public string modelJson { get; set; }
        public DateTime LogTime { get; set; }
        public List<ExceptionLoggerModel> LST { get; set; }
    }
}