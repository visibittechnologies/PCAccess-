using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Linq;
using System.Web;

namespace PCAccess.Models
{
    public class LoginModel
    {
        public int? userId { get; set; }

        [Display(Name = "User Name")]
        [Required(ErrorMessage = "Please fill valid username !!!")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Please fill Password !!!")]
        [Display(Name = "Password")]
        [DataType(DataType.Password)]
        public string PassWord { get; set; }
        public int user_role_id { get; set; }

        public bool RememberMe { get; set; }
    }
    public class LoginWithOtp
    {
        public int userId { get; set; }

        [Display(Name = "Mobile Number")]
        [Required(ErrorMessage = "Required!")]
        [Range(6000000000,9999999999,ErrorMessage = "Invalid mobile number!")]
        public string UserName { get; set; }
        public string token { get; set; }
        public string PassWord { get; set; }
        public string app_type { get; set; }
        public int user_role_id { get; set; }
        public string referral_id { get; set; }
        public string flag { get; set; }
    }

    public enum LogType
    {
        [Description("Log in fail")]
        LoginFail = 0,
        [Description("Log in")]
        Login = 1,
        [Description("Log out")]
        Exit = 2,
        [Description("Access")]
        Visit = 3,
        [Description("Added")]
        Create = 4,
        [Description("Delete")]
        Delete = 5,
        [Description("Modify")]
        Update = 6,
        [Description("Submit")]
        Submit = 7,
        [Description("Exception")]
        Exception = 8,
        [Description("View")]
        View = 9,
        [Description("Other")]
        Other = 100,
    }
    public class VerifyUserResult
    {
        public DataTable Data { get; set; }
        public string Status { get; set; }
        public string Message { get; set; }
    }

}