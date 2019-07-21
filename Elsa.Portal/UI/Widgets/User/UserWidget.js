console.log("uwjs");

lt.element("userWidget").withModel("app.user.vm").attach(function (loginPanel, userDetail, passChange) {

    var self = this;

    var detail = document.getElementById("loggedUserLabel");

    var detailVisible = false;
    var changePasswordVisible = false;
    var isLoggedIn = false;

    var updateView = function() {

        if (!isLoggedIn) {
            loginPanel.style.display = 'block';
            detail.style.display = 'none';
            userDetail.style.display = 'none';
            detailVisible = false;
            changePasswordVisible = false;
        } else {
            loginPanel.style.display = 'none';
            detail.style.display = 'block';
        }

        if (detailVisible) {
            userDetail.style.display = 'block';
        } else {
            userDetail.style.display = 'none';
            passChange.style.display = 'none';
            changePasswordVisible = false;
        }

        if (changePasswordVisible) {
            passChange.style.display = 'block';
        } else {
            passChange.style.display = 'none';
        }

        if (isLoggedIn && (!detailVisible) && (!changePasswordVisible)) {
            self.style.paddingTop = 0;
            self.style.paddingBottom = 0;
        } else {
            self.style.paddingTop = "6px";
            self.style.paddingBottom = "6px";
        }
    };

    detail.addEventListener("click",
        function() {
            detailVisible = !detailVisible;
            updateView();
        });

    this.bind(function(currentSession) {

        isLoggedIn = ((!!currentSession) && (!!currentSession.User));

        if (isLoggedIn) {
            detail.innerHTML = currentSession.User.EMail;
        } else {
            detail.innerHTML = "";
        }

        updateView();

    }).currentSessionCanBeNull();

    this.onLoginClick = function(user, pwd) {
        app.user.vm.login(user, pwd);
    };

    this.onLogoutClick = function() {
        app.user.vm.logout();
    };

    this.onDisplayPasswordChangeClick = function() {
        changePasswordVisible = !changePasswordVisible;

        updateView();
        
    };

    this.onChangePasswordClick = function(oldPass, newPass, newPassConfirm) {
        if (newPass !== newPassConfirm) {
            throw new Error("Chybné potvrzení nového hesla");
        }

        app.user.vm.changePassword(oldPass, newPass);
        
    };

    try {
        if (document.location.hostname === "localhost") {
            var userPanelHead = document.getElementById("userPanelHead");
            userPanelHead.style.backgroundColor = 'green';
        }
    } catch (e) {} 

});