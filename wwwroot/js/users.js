let users = [];
let displayUsers = [];

let currentPage = 1;
let pageSize = 10;

// 当前正在修改的用户 ID
let editingUserId = null;


// 公共模块统一处理 API 状态、错误窗口和成功窗口。
const { showApiError, showApiSuccess, handleApiResult } = AdminApp;


// ==================== 获取用户列表 ====================

async function loadUsers() {

    const token = sessionStorage.getItem("token");

    if (!token) {

        window.location.href = "/admin_login";

        return;
    }


    try {

        const response = await fetch(
            "/api/getuserdata",
            {
                method: "POST",

                headers: {
                    "Content-Type": "application/json"
                },

                body: JSON.stringify({
                    RequestType: "requestuser_data",
                    token: token
                })
            }
        );


        const result = await response.json();


        if (!handleApiResult(
            result,
            "获取用户列表失败",
            response.ok,
            response.status
        )) {

            return;
        }


        users = result.usersdata || [];

        displayUsers = users;

        currentPage = 1;

        renderUsers();

    }
    catch (error) {

        showApiError(
            "Network",
            "无法连接到服务器"
        );

    }
}


// ==================== 渲染用户 ====================

function renderUsers() {

    const tbody =
        document.getElementById("UserTableBody");

    tbody.innerHTML = "";


    const startIndex =
        (currentPage - 1) * pageSize;

    const endIndex =
        startIndex + pageSize;


    const pageUsers =
        displayUsers.slice(
            startIndex,
            endIndex
        );


    for (const user of pageUsers) {

        const tr =
            document.createElement("tr");


        // ID

        const idTd =
            document.createElement("td");

        idTd.textContent = user.id;

        tr.appendChild(idTd);


        // 用户名

        const usernameTd =
            document.createElement("td");

        usernameTd.textContent =
            user.username;

        tr.appendChild(usernameTd);


        // 状态

        const statusTd =
            document.createElement("td");

        statusTd.textContent =
            user.enabled
                ? "启用"
                : "禁用";

        tr.appendChild(statusTd);


        // 操作

        const actionTd =
            document.createElement("td");


        const select =
            document.createElement("select");


        // 默认选项

        const defaultOption =
            document.createElement("option");

        defaultOption.value = "";

        defaultOption.textContent =
            "选择操作";

        select.appendChild(defaultOption);


        // 修改

        const reviseOption =
            document.createElement("option");

        reviseOption.value = "revise";

        reviseOption.textContent =
            "修改用户";

        select.appendChild(reviseOption);


        // 删除

        const deleteOption =
            document.createElement("option");

        deleteOption.value = "delete";

        deleteOption.textContent =
            "删除用户";

        select.appendChild(deleteOption);


        select.addEventListener(
            "change",
            function () {

                if (this.value === "revise") {

                    openReviseUserDialog(user);
                }


                if (this.value === "delete") {

                    deleteUser(user);
                }


                this.value = "";
            }
        );


        actionTd.appendChild(select);

        tr.appendChild(actionTd);

        tbody.appendChild(tr);
    }


    renderPagination();
}


// ==================== 分页 ====================

function renderPagination() {

    const pagination =
        document.getElementById("Pagination");

    pagination.innerHTML = "";


    const totalPages =
        Math.ceil(
            displayUsers.length / pageSize
        );


    if (totalPages <= 1) {
        return;
    }


    // 上一页

    const previousButton =
        document.createElement("button");

    previousButton.textContent =
        "上一页";

    previousButton.disabled =
        currentPage === 1;


    previousButton.addEventListener(
        "click",
        function () {

            if (currentPage > 1) {

                currentPage--;

                renderUsers();
            }
        }
    );


    pagination.appendChild(
        previousButton
    );


    // 页码

    for (
        let i = 1;
        i <= totalPages;
        i++
    ) {

        const pageButton =
            document.createElement("button");

        pageButton.textContent = i;


        if (i === currentPage) {

            pageButton.disabled = true;
        }


        pageButton.addEventListener(
            "click",
            function () {

                currentPage = i;

                renderUsers();
            }
        );


        pagination.appendChild(
            pageButton
        );
    }


    // 下一页

    const nextButton =
        document.createElement("button");

    nextButton.textContent =
        "下一页";

    nextButton.disabled =
        currentPage === totalPages;


    nextButton.addEventListener(
        "click",
        function () {

            if (
                currentPage <
                totalPages
            ) {

                currentPage++;

                renderUsers();
            }
        }
    );


    pagination.appendChild(
        nextButton
    );
}


// ==================== 每页数量 ====================

document
    .getElementById("PageSize")
    .addEventListener(
        "change",
        function () {

            pageSize =
                Number(this.value);

            currentPage = 1;

            renderUsers();
        }
    );


// ==================== 搜索 ====================

document
    .getElementById("SearchInput")
    .addEventListener(
        "input",
        function () {

            const keyword =
                this.value
                    .trim()
                    .toLowerCase();


            if (keyword === "") {

                displayUsers = users;
            }
            else {

                displayUsers =
                    users.filter(
                        function (user) {

                            return user.username
                                .toLowerCase()
                                .includes(keyword);
                        }
                    );
            }


            currentPage = 1;

            renderUsers();
        }
    );


// ==================== 创建用户 ====================

document
    .getElementById("CreateUserButton")
    .addEventListener(
        "click",
        function () {

            document
                .getElementById(
                    "CreateUsername"
                )
                .value = "";

            document
                .getElementById(
                    "CreatePassword"
                )
                .value = "";

            document
                .getElementById(
                    "CreateEnabled"
                )
                .checked = true;


            document
                .getElementById(
                    "CreateUserDialog"
                )
                .showModal();
        }
    );


document
    .getElementById(
        "CancelCreateUserButton"
    )
    .addEventListener(
        "click",
        function () {

            document
                .getElementById(
                    "CreateUserDialog"
                )
                .close();
        }
    );


document
    .getElementById("CreateUserForm")
    .addEventListener(
        "submit",
        async function (event) {

            event.preventDefault();


            const token =
                sessionStorage.getItem(
                    "token"
                );


            if (!token) {

                window.location.href =
                    "/admin_login";

                return;
            }


            const username =
                document
                    .getElementById(
                        "CreateUsername"
                    )
                    .value
                    .trim();


            const password =
                document
                    .getElementById(
                        "CreatePassword"
                    )
                    .value;


            const enabled =
                document
                    .getElementById(
                        "CreateEnabled"
                    )
                    .checked;


            try {

                const response =
                    await fetch(
                        "/api/create_user",
                        {
                            method: "POST",

                            headers: {
                                "Content-Type":
                                    "application/json"
                            },

                            body:
                                JSON.stringify({
                                    token: token,
                                    username: username,
                                    password: password,
                                    enabled: enabled
                                })
                        }
                    );


                const result =
                    await response.json();


                if (!handleApiResult(
                    result,
                    "创建用户失败",
                    response.ok,
                    response.status
                )) {

                    return;
                }


                document
                    .getElementById(
                        "CreateUserDialog"
                    )
                    .close();

                showApiSuccess("用户创建成功");


                await loadUsers();

            }
            catch (error) {

                showApiError(
                    "Network",
                    "无法连接到服务器"
                );

            }
        }
    );


// ==================== 修改用户 ====================

function openReviseUserDialog(user) {

    /*
     * 保存原用户 ID。
     *
     * 这个值才是后端定位用户时使用的 ID。
     */

    editingUserId = user.id;


    /*
     * ID 只显示。
     *
     * 这个输入框不会参与提交。
     */

    document
        .getElementById(
            "ReviseUserId"
        )
        .value = user.id;


    document
        .getElementById(
            "ReviseUsername"
        )
        .value = user.username;


    document
        .getElementById(
            "RevisePassword"
        )
        .value = "";


    document
        .getElementById(
            "ReviseEnabled"
        )
        .checked = user.enabled;


    document
        .getElementById(
            "ReviseUserDialog"
        )
        .showModal();
}


// 取消修改

document
    .getElementById(
        "CancelReviseUserButton"
    )
    .addEventListener(
        "click",
        function () {

            document
                .getElementById(
                    "ReviseUserDialog"
                )
                .close();

            editingUserId = null;
        }
    );


// 提交修改

document
    .getElementById("ReviseUserForm")
    .addEventListener(
        "submit",
        async function (event) {

            event.preventDefault();


            const token =
                sessionStorage.getItem(
                    "token"
                );


            if (!token) {

                window.location.href =
                    "/admin_login";

                return;
            }


            /*
             * 注意：
             *
             * 这里绝对不从 ReviseUserId
             * 这个输入框读取 ID。
             *
             * editingUserId 才是原用户 ID。
             */

            const id =
                editingUserId;


            if (
                !Number.isInteger(id) ||
                id <= 0
            ) {

                showApiError(
                    "InvalidID",
                    "无效的用户 ID"
                );

                return;
            }


            const username =
                document
                    .getElementById(
                        "ReviseUsername"
                    )
                    .value
                    .trim();


            const password =
                document
                    .getElementById(
                        "RevisePassword"
                    )
                    .value;
                    

            const NO_PASSWORD_CHANGE = "null";

            const passwordToSend =
                password === ""
                    ? NO_PASSWORD_CHANGE
                    : password;



            const enabled =
                document
                    .getElementById(
                        "ReviseEnabled"
                    )
                    .checked;


            const request = {

                id: id,

                username: username,

                /*
                 * 字段名虽然是 password_hash，
                 * 但这里发送的是明文密码。
                 *
                 * 后端负责 BCrypt。
                 *
                 * 留空表示不修改密码。
                 */

                password_hash: passwordToSend,

                enabled: enabled,

                token: token
            };


            try {

                const response =
                    await fetch(
                        "/api/revise_user",
                        {
                            method: "POST",

                            headers: {
                                "Content-Type":
                                    "application/json"
                            },

                            body:
                                JSON.stringify(
                                    request
                                )
                        }
                    );


                const result =
                    await response.json();


                if (!handleApiResult(
                    result,
                    "修改用户失败",
                    response.ok,
                    response.status
                )) {

                    return;
                }


                document
                    .getElementById(
                        "ReviseUserDialog"
                    )
                    .close();


                editingUserId = null;

                showApiSuccess("用户修改成功");


                await loadUsers();

            }
            catch (error) {

                showApiError(
                    "Network",
                    "无法连接到服务器"
                );

            }
        }
    );


// ==================== 删除用户 ====================

async function deleteUser(user) {

    const confirmed =
        window.confirm(
            `确定要删除用户吗？\n\nID：${user.id}\n用户名：${user.username}`
        );


    if (!confirmed) {
        return;
    }


    const token =
        sessionStorage.getItem(
            "token"
        );


    if (!token) {

        window.location.href =
            "/admin_login";

        return;
    }


    const request = {

        id: user.id,

        username: user.username,

        password_hash: "",

        enabled: user.enabled,

        token: token
    };


    try {

        const response =
            await fetch(
                "/api/delete_user",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json"
                    },

                    body:
                        JSON.stringify(
                            request
                        )
                }
            );


        const result =
            await response.json();


        if (!handleApiResult(
            result,
            "删除用户失败",
            response.ok,
            response.status
        )) {

            return;
        }


        showApiSuccess("用户删除成功");

        await loadUsers();

    }
    catch (error) {

        showApiError(
            "Network",
            "无法连接到服务器"
        );

    }
}


// ==================== 页面初始化 ====================

loadUsers();
