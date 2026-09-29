var initial_username = "";
var initial_password = "";
$(document).ready(function () {
    GetUserProfileDetails();
});
function GetUserProfileDetails() {
    var user_id = parseInt($("#user_id").val().trim()) || 0;
    $.ajax({
        url: '/Common/GetUserDetailsForEdit',
        type: "POST",
        data: JSON.stringify({ user_id: user_id }),
        dataType: 'json',
        contentType: "application/json; charset=utf-8",
        beforeSend: function () {
            $("#loading-div-background").css("display", "flex");
        },
        success: function (data) {
            $("#loading-div-background").hide();
            data = JSON.parse(data);
            if (Array.isArray(data) && data.length > 0) {
                let row = data[0];
                $("#name").val(row.name);
                $("#email").val(row.email);
                $("#username").val(row.username);
                $("#password").val(row.password);
                initial_username = row.username;
                initial_password = row.password;
                if (row.profile_photo && row.profile_photo !== "" && row.profile_photo !== "null") {
                    $("#profile_img_preview").attr("src", row.profile_photo);
                    $("#header_profile_img").attr("src", row.profile_photo);
                } else {
                    $("#profile_img_preview").attr("src", "/Content/assets/images/user.png");
                    $("#header_profile_img").attr("src", "/Content/assets/images/user.png");
                }
            }
        },
        error: function () {
            $("#loading-div-background").hide();
            Swal.fire("Something went wrong loading user details.");
        }
    });
}
$("#ProfilePhotoImage").on("change", function () {
    let user_id = parseInt($("#user_id").val()) || 0;
    let file = this.files[0];
    if (!file) return;
    let reader = new FileReader();
    reader.onload = function (e) {
        $("#profile_img_preview").attr("src", e.target.result);
    };
    reader.readAsDataURL(file);
    let formData = new FormData();
    formData.append("ProfilePhotoImage", file);
    formData.append("user_id", user_id);
    formData.append("ChildPath", "profile");
    $.ajax({
        url: "/Common/ProfileImageUpdate",
        type: "POST",
        data: formData,
        contentType: false,
        processData: false,
        beforeSend: function () {
            $("#loading-div-background").css("display", "flex");
        },
        success: function (data) {
            $("#loading-div-background").hide();
            if (data.status === "success") {
                Swal.fire("Success", data.message, "success");
                $("#profile_img_preview").attr("src", data.image_url);
                $("#header_profile_img").attr("src", data.image_url);
            } else {
                Swal.fire("Error", data.message, "error");
            }
        },
        error: function () {
            $("#loading-div-background").hide();
            Swal.fire("Error", "Image upload failed", "error");
        }
    });
});
function removeProfileImage() {
    let user_id = parseInt($("#user_id").val()) || 0;
    Swal.fire({
        title: "Remove photo?",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Yes, remove"
    }).then((res) => {
        if (!res.isConfirmed) return;
        $.ajax({
            url: "/Common/CommonDelete",
            type: "POST",
            data: JSON.stringify({ Flag: "remove_profile_image", Id: user_id }),
            contentType: "application/json; charset=utf-8",
            dataType: 'json',
            success: function (data) {
                if (data.status === "success") {
                    $("#profile_img_preview").attr("src", "/Content/assets/images/user.png");
                    $("#header_profile_img").attr("src", "/Content/assets/images/user.png");
                    Swal.fire("Removed", data.message, "success");
                }
                else {
                    Swal.fire("Error", data.message, "error");
                }
            },
            error: function () {
                Swal.fire("Error", "Something went wrong", "error");
            }
        });
    });
}
function scrollToAndFocus(selector) {
    let el = $(selector);
    $("html, body").animate({
        scrollTop: el.offset().top - 80
    }, 300);
    setTimeout(() => {
        el.focus();
    }, 300);
}
$("#BtnUpdateProfile").on("click", function (e) {
    e.preventDefault();
    var user_id = parseInt($("#user_id").val().trim()) || 0;
    var name = $("#name").val().trim();
    var email = $("#email").val().trim();
    var username = $("#username").val().trim();
    var password = $("#password").val().trim();
    var new_password = $("#new_password").val().trim();
    var confirm_password = $("#confirm_password").val().trim();
    if (name === "") {
        Swal.fire({ icon: "warning", title: "Required!", text: "Please enter name!", confirmButtonColor: "#4556b7" })
            .then(() => scrollToAndFocus("#name"));
        return;
    }
    if (email === "") {
        Swal.fire({ icon: "warning", title: "Required!", text: "Please enter email!", confirmButtonColor: "#4556b7" })
            .then(() => scrollToAndFocus("#email"));
        return;
    }
    if (username === "") {
        Swal.fire({ icon: "warning", title: "Required!", text: "Please enter username!", confirmButtonColor: "#4556b7" })
            .then(() => scrollToAndFocus("#username"));
        return;
    }
    if (new_password !== "" || confirm_password !== "") {
        if (new_password !== confirm_password) {
            Swal.fire({ icon: "warning", title: "Mismatch!", text: "Passwords do not match!", confirmButtonColor: "#4556b7" })
                .then(() => scrollToAndFocus("#confirm_password"));
            return;
        }
        password = new_password;
    }
    if (password === "") {
        Swal.fire({ icon: "warning", title: "Required!", text: "Please enter password!", confirmButtonColor: "#4556b7" })
            .then(() => scrollToAndFocus("#password"));
        return;
    }
    var formData = new FormData();
    formData.append("user_id", user_id);
    formData.append("name", name);
    formData.append("email", email);
    formData.append("username", username);
    formData.append("password", password);
    formData.append("isAuthChanged", (username !== initial_username || password !== initial_password));
    $.ajax({
        url: '/Common/UpdateProfile',
        type: 'POST',
        data: formData,
        contentType: false,
        processData: false,
        beforeSend: function () {
            $("#BtnUpdateProfile").prop("disabled", true);
            $("#loading-div-background").css("display", "flex");
        },
        success: function (data) {
            $("#loading-div-background").hide();
            $("#BtnUpdateProfile").prop("disabled", false);
            if (data.status === "logout") {
                Swal.fire({ icon: "success", title: "Success", text: data.message, confirmButtonColor: "#4556b7" })
                    .then(() => { window.location.href = data.url; });
                return;
            }
            if (data.status === "success") {
                Swal.fire({ icon: "success", title: "Success", text: data.message, confirmButtonColor: "#4556b7" })
                    .then(() => { if (data.url) window.location.reload(); });
            } else {
                Swal.fire({ icon: "error", title: "Oops!", text: data.message, confirmButtonColor: "#4556b7" });
            }
        },
        error: function () {
            $("#loading-div-background").hide();
            $("#BtnUpdateProfile").prop("disabled", false);
            Swal.fire("Error", "Something went wrong", "error");
        }
    });
});
