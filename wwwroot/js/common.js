/* Shared administration layout, dialogs, and API response handling. */
window.AdminApp = (() => {
    const TOKEN_TIMEOUT_RECODE = 2000;

    function showDialog(id) {
        const dialog = document.getElementById(id);
        if (dialog && !dialog.open) {
            dialog.showModal();
        }
    }

    function showApiError(code, hint) {
        const codeElement = document.getElementById("ErrorCode");
        const hintElement = document.getElementById("ErrorHint");
        if (codeElement) codeElement.textContent = code;
        if (hintElement) hintElement.textContent = hint;
        showDialog("ErrorDialog");
    }

    function showApiSuccess(hint, onClose) {
        const hintElement = document.getElementById("SuccessHint");
        const dialog = document.getElementById("SuccessDialog");
        if (hintElement) hintElement.textContent = hint;
        if (dialog && onClose) dialog.addEventListener("close", onClose, { once: true });
        showDialog("SuccessDialog");
    }

    function handleApiResult(result, defaultHint, responseOk = true, status) {
        if (result?.recode === TOKEN_TIMEOUT_RECODE) {
            sessionStorage.removeItem("token");
            window.location.href = "/timeout.html";
            return false;
        }

        if (!responseOk || result?.success !== true) {
            showApiError(result?.recode ?? status ?? "Unknown", result?.hint || defaultHint);
            return false;
        }

        return true;
    }

    async function post(route, payload, defaultHint) {
        let response;
        let result;
        try {
            response = await fetch(route, {
                method: "POST",
                headers: { "Content-Type": "application/json" },
                body: JSON.stringify(payload)
            });
            result = await response.json();
        } catch (error) {
            showApiError("Network", "无法连接到服务器或服务器返回了无效数据");
            return null;
        }

        return handleApiResult(result, defaultHint, response.ok, response.status) ? result : null;
    }

    function bindDialogControls() {
        document.querySelectorAll("[data-dialog-close]").forEach((button) => {
            button.addEventListener("click", () => document.getElementById(button.dataset.dialogClose)?.close());
        });
    }

    function bindAdminMenu() {
        const button = document.getElementById("AdminButton");
        const menu = document.getElementById("AdminMenu");
        if (!button || !menu) return;
        button.addEventListener("click", (event) => {
            event.stopPropagation();
            menu.classList.toggle("is-open");
        });
        document.addEventListener("click", (event) => {
            if (!button.contains(event.target) && !menu.contains(event.target)) menu.classList.remove("is-open");
        });
    }

    function bindLogout() {
        const button = document.getElementById("LogoutButton");
        if (!button) return;
        button.addEventListener("click", async () => {
            button.disabled = true;
            const result = await post("/api/logout", {
                RequestType: "logout",
                token: sessionStorage.getItem("token") || ""
            }, "退出登录失败");
            button.disabled = false;
            if (!result) return;
            sessionStorage.removeItem("token");
            showApiSuccess("退出登录成功", () => { window.location.href = "/admin_login"; });
        });
    }

    function bindNavigation() {
        document.querySelectorAll("[data-admin-nav]").forEach((link) => {
            link.addEventListener("click", (event) => {
                const token = sessionStorage.getItem("token");
                if (!token) {
                    event.preventDefault();
                    window.location.href = "/admin_login";
                }
            });
        });
    }

    function initialise() {
        bindDialogControls();
        bindAdminMenu();
        bindLogout();
        bindNavigation();
    }

    return { handleApiResult, post, showApiError, showApiSuccess, initialise };
})();

document.addEventListener("DOMContentLoaded", () => AdminApp.initialise());

// ==================== 后台页面导航 ====================

document.addEventListener("click", function (event) {

    const link = event.target.closest("a[data-admin-nav]");

    if (!link) {
        return;
    }


    const token = sessionStorage.getItem("token");


    // 没有 Token，返回登录页面
    if (!token) {

        event.preventDefault();

        window.location.href = "/timeout.html";

        return;
    }


    // 阻止浏览器直接跳转
    event.preventDefault();


    // 根据原来的 href 创建 URL
    const url = new URL(
        link.href,
        window.location.origin
    );


    // 添加 / 覆盖 token 查询参数
    url.searchParams.set(
        "token",
        token
    );


    // 按修改后的 URL 跳转
    window.location.href =
        url.pathname +
        url.search +
        url.hash;
});
