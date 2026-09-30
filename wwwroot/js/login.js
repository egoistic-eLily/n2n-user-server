const loginForm = document.getElementById("LoginForm");
const loginButton = document.getElementById("LoginButton");

loginForm.addEventListener("submit", async (event) => {
    event.preventDefault();
    const username = document.getElementById("username").value.trim();
    const password = document.getElementById("password").value;

    if (!username || !password) {
        AdminApp.showApiError("InvalidInput", "请输入用户名和密码");
        return;
    }

    loginButton.disabled = true;
    loginButton.textContent = "登录中…";
    const result = await AdminApp.post("/api/login", {
        userid: username,
        password
    }, "登录失败");
    loginButton.disabled = false;
    loginButton.textContent = "登录";

    if (!result) return;
    if (!result.token) {
        AdminApp.showApiError(result.recode ?? "InvalidResponse", result.hint || "服务器未返回登录凭据");
        return;
    }

    sessionStorage.setItem("token", result.token);
    AdminApp.showApiSuccess("登录成功，确认后进入管理控制台。", () => {
        window.location.href = "/admin?token=" + encodeURIComponent(result.token);
    });
});
