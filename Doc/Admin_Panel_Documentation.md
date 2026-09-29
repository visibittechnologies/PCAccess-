# Nkosh Blog — Admin Panel Documentation

**Project:** Nkosh Blog  
**Prepared for:** Project Manager  
**Date:** April 28, 2026

---

## Admin Panel Pages — Summary

| # | Admin Page | URL | Purpose |
|---|-----------|-----|---------|
| 1 | Dashboard | `/Admin/DashboardOverview` | Summary of all modules |
| 2 | Company Profile | `/Admin/AddCompany` | Company info, logo, social links |
| 3 | Slider | `/Admin/SliderList` | Homepage banner sliders |
| 4 | Gallery | `/Admin/GallaryList` | Photo gallery |
| 5 | Blog / News | `/Admin/BlogList` | News & Events posts |
| 6 | Testimonials | `/Admin/TestimonialsList` | Client testimonials |
| 7 | Careers (Jobs) | `/Admin/CareerMaster` | Job openings |
| 8 | Career Requests | `/Admin/CareerRequestList` | Job applications from public |
| 9 | Contact Requests | `/Admin/Contact` | Contact form submissions |
| 10 | Referral Requests | `/Admin/ReferralRequestList` | Referral form submissions |
| 11 | Assessment Requests | `/Admin/AssessmentRequestList` | Assessment form submissions |
| 12 | Page Meta Tags (SEO) | `/Admin/PageMetaTagsList` | SEO title/description per page |
| 13 | Google Scripts | `/Admin/GoogleScriptList` | Analytics / GTM scripts |
| 14 | Mail Settings | `/Admin/MailSettings` | SMTP email configuration |
| 15 | User Profile | `/Admin/Profile` | Admin user profile |

---

## Detailed Page-wise Breakdown

---

### 1. Company Profile
**Admin Page:** `AddCompany.cshtml`  
**API Method:** `AdminAPI/AddCompanyProfile`, `AdminAPI/GetCompanyProfileDetails`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_company_profile` |
| **Stored Procedure** | `add_company_details` |
| **Operations** | Add / Update company name, phone, email, address, logo, social media URLs |

---

### 2. Slider (Homepage Banner)
**Admin Page:** `SliderList.cshtml`  
**API Methods:** `AdminAPI/AddSlider`, `AdminAPI/GetSliderList`, `AdminAPI/GetSliderDetails`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_slider_master` |
| **Stored Procedure** | `add_company_details` (slider flag) |
| **Operations** | Add / Edit / Delete sliders — title, content, image, button link |

---

### 3. Gallery
**Admin Page:** `GallaryList.cshtml`  
**API Method:** `AdminAPI/AddGallery`, `AdminAPI/GetGalleryList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_gallery` |
| **Stored Procedure** | `add_gallery` |
| **Operations** | Upload / manage gallery images, set sequence order |

---

### 4. Blog / News & Events
**Admin Page:** `BlogList.cshtml`  
**API Method:** `AdminAPI/AddBlog`, `AdminAPI/GetBlogList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_blog` |
| **Stored Procedure** | `add_blog` |
| **Operations** | Create / Edit / Delete blog posts and news articles |

---

### 5. Testimonials
**Admin Page:** `TestimonialsList.cshtml`  
**API Methods:** `AdminAPI/AddTestimonial`, `AdminAPI/GetTestimonialList`, `AdminAPI/GetTestimonialDetails`, `AdminAPI/ToggleTestimonialHome`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_testimonial` |
| **Stored Procedure** | `add_company_details` (testimonial flag) |
| **Operations** | Add / Edit testimonials, toggle display on homepage |

---

### 6. Careers (Job Openings)
**Admin Page:** `CareerMaster.cshtml`  
**API Methods:** `AdminAPI/AddCareerMaster`, `AdminAPI/GetCareerList`, `AdminAPI/CareerDetails`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_career_master` |
| **Stored Procedure** | `proc_career_master` |
| **Operations** | Post / Edit / Delete job openings (title, description, type) |

---

### 7. Career Requests (Job Applications)
**Admin Page:** `CareerRequestList.cshtml`  
**API Method:** `AdminAPI/GetCareerRequestList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_job_application` |
| **Stored Procedure** | `proc_career_master` |
| **Operations** | View job applications submitted from the public website Careers page |
| **Data Captured** | Name, Email, Phone, Position Applied, Message, Resume |

---

### 8. Contact Requests
**Admin Page:** `Contact.cshtml`  
**API Method:** `AdminAPI/GetContactRequestList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_contact_request` |
| **Stored Procedure** | `proc_contact_request` |
| **Operations** | View contact form submissions from public website |
| **Data Captured** | Full Name, Email, Phone, Subject, Message |

---

### 9. Referral Requests
**Admin Page:** `ReferralRequestList.cshtml`  
**API Method:** `AdminAPI/GetReferralRequestList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_referral_request` |
| **Stored Procedure** | `proc_referral_request` |
| **Operations** | View referral form submissions from public website |
| **Data Captured** | Full Name, Email, Phone, Referral Type, Message |

---

### 10. Assessment Requests
**Admin Page:** `AssessmentRequestList.cshtml`  
**API Methods:** `AdminAPI/AddAssessmentRequest`, `AdminAPI/GetAssessmentRequestList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_assessment_request` |
| **Stored Procedure** | `proc_assessment_request` |
| **Operations** | View assessment form submissions from public website |
| **Data Captured** | Patient info, care needs, preferred date/time |

---

### 11. Page Meta Tags (SEO)
**Admin Page:** `PageMetaTagsList.cshtml`  
**API Method:** `AdminAPI/AddPageMetaTag`, `AdminAPI/GetPageMetaTagList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_page_seo_meta_tags` |
| **Stored Procedure** | `proc_add_page_meta_tags` |
| **Operations** | Set SEO title, meta description, keywords per page URL |

---

### 12. Google Scripts
**Admin Page:** `GoogleScriptList.cshtml`  
**API Method:** `AdminAPI/AddGoogleScript`, `AdminAPI/GetGoogleScriptList`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_google_script` |
| **Stored Procedure** | `proc_add_seo_tool` |
| **Operations** | Add / manage Google Analytics, GTM, or other head/body scripts |

---

### 13. Mail Settings (SMTP)
**Admin Page:** `MailSettings.cshtml`  
**API Method:** `CommonAPI/SaveMailSettings`, `CommonAPI/GetMailSettings`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_mail_settings` |
| **Stored Procedure** | `proc_add_send_mail_settings_by_admin` |
| **Operations** | Configure SMTP host, port, email, password for sending emails |

---

### 14. User Profile
**Admin Page:** `Profile.cshtml`  
**API Method:** `AdminAPI/EditUserProfile`

| Item | Detail |
|------|--------|
| **Database Table** | `tbl_user` |
| **Stored Procedure** | `edit_user_profile` |
| **Operations** | Update admin name, email, profile photo, password |

---

### 15. Login
**Admin Page:** Login screen  
**API Method:** Authentication middleware

| Item | Detail |
|------|--------|
| **Database Tables** | `tbl_user`, `tbl_user_loginfo` |
| **Stored Procedures** | `user_login`, `ua_user_loginfo_iud` |
| **Operations** | Admin login, session tracking |

---

## Master Table Reference

| Table Name | Used In |
|-----------|---------|
| `tbl_company_profile` | Company Profile |
| `tbl_slider_master` | Slider / Banner |
| `tbl_gallery` | Gallery |
| `tbl_blog` | Blog / News & Events |
| `tbl_testimonial` | Testimonials |
| `tbl_career_master` | Job Openings |
| `tbl_job_application` | Career / Job Requests |
| `tbl_contact_request` | Contact Requests |
| `tbl_referral_request` | Referral Requests |
| `tbl_assessment_request` | Assessment Requests |
| `tbl_page_seo_meta_tags` | SEO Meta Tags |
| `tbl_google_script` | Google Scripts |
| `tbl_mail_settings` | Mail / SMTP Settings |
| `tbl_user` | Admin Users |
| `tbl_user_loginfo` | Login Session Logs |
| `m_references` | Dropdown Master Values |

---

## Master Stored Procedure Reference

| Stored Procedure | Used For |
|-----------------|---------|
| `add_company_details` | Company Profile, Slider, Testimonials |
| `add_blog` | Blog / News |
| `add_gallery` | Gallery |
| `proc_career_master` | Job Openings + Applications |
| `proc_contact_request` | Contact Form |
| `proc_referral_request` | Referral Form |
| `proc_assessment_request` | Assessment Form |
| `proc_add_page_meta_tags` | SEO Meta Tags |
| `proc_add_seo_tool` | Google Scripts |
| `proc_add_send_mail_settings_by_admin` | Mail Settings |
| `edit_user_profile` | User Profile Update |
| `user_login` | Admin Login |
| `ua_user_loginfo_iud` | Login Session Tracking |
| `proc_get_dropdown_value` | All Dropdown Master Lists |
| `proc_project_remove` | Data Cleanup / Remove Records |
