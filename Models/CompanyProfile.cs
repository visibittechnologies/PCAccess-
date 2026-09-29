using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace PCAccess.Models
{
    public class CompanyProfile
    {
        public int ID { get; set; }
        public string company_name { get; set; }
        public string email { get; set; }
        public string phone_no { get; set; }
        public string whats_app_number { get; set; }
        public string address { get; set; }
        public string coprate_address { get; set; }
        public string title { get; set; }
        public string google_map_url { get; set; }
        public string facebook_url { get; set; }
        public string twitter_url { get; set; }
        public string linkedin_url { get; set; }
        public string instagram_url { get; set; }
        public string youtube_url { get; set; }
        public string contact_person { get; set; }
        public string secondary_phone { get; set; }
        public string fax_no { get; set; }
        public string pinterest_url { get; set; }
        public string reddit_url { get; set; }
        public string tumblr_url { get; set; }
        public string meta_keywords { get; set; }
        public int show_in_common { get; set; }
        public int enable_public_links { get; set; }
        public int require_manual_review { get; set; }
        public string company_logo { get; set; }
        public string privacy_policy_file { get; set; }
        public string terms_conditions_file { get; set; }

        // Aliases with both getter and setter to satisfy the DataRowMapper
        public string CompanyName { get { return company_name; } set { company_name = value; } }
        public string Email { get { return email; } set { email = value; } }
        public string PhoneNo { get { return phone_no; } set { phone_no = value; } }
        public string Pr { get { return phone_no; } set { phone_no = value; } }
        public string Title { get { return title; } set { title = value; } }
        public string Address { get { return address; } set { address = value; } }
        public string CompanyLogo { get { return company_logo; } set { company_logo = value; } }
    }
}