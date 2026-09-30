let usersConfig = [];
let displayConfig = [];

let currentPage = 1;
let pageSize = 10;

// 当前正在修改的配置对应的用户 ID
let editingUserId = null;


// 公共模块统一处理 API 状态、错误窗口和成功窗口。
const { showApiError, showApiSuccess, handleApiResult } = AdminApp;


// ==================== 获取配置 ====================

async function loadConfig() {

    const token =
        sessionStorage.getItem("token");


    if (!token) {

        window.location.href = "/admin_login";

        return;
    }


    try {

        const response =
            await fetch(
                "/api/getusersconfig",
                {
                    method: "POST",

                    headers: {
                        "Content-Type":
                            "application/json"
                    },

                    body:
                        JSON.stringify({
                            RequestType:
                                "requestuser_config",

                            token:
                                token
                        })
                }
            );


        const result =
            await response.json();


        if (!handleApiResult(
            result,
            "获取用户配置失败",
            response.ok,
            response.status
        )) {

            return;
        }


        usersConfig =
            result.usersconfig || [];


        displayConfig =
            usersConfig;


        currentPage = 1;


        renderConfig();

    }
    catch (error) {

        showApiError(
            "Network",
            "无法连接到服务器"
        );
    }
}


// ==================== 渲染配置列表 ====================

function renderConfig() {

    const tbody =
        document.getElementById(
            "ConfigTableBody"
        );


    tbody.innerHTML = "";


    const startIndex =
        (currentPage - 1) * pageSize;


    const endIndex =
        startIndex + pageSize;


    const pageConfig =
        displayConfig.slice(
            startIndex,
            endIndex
        );


    for (const config of pageConfig) {

        const tr =
            document.createElement("tr");


        // 用户 ID

        const userIdTd =
            document.createElement("td");

        userIdTd.textContent =
            config.user_id;

        tr.appendChild(userIdTd);


        // Supernode

        const supernodeTd =
            document.createElement("td");

        supernodeTd.textContent =
            `${config.supernode_ip}:${config.supernode_port}`;

        tr.appendChild(supernodeTd);


        // Community

        const communityTd =
            document.createElement("td");

        communityTd.textContent =
            config.community_name;

        tr.appendChild(communityTd);


        // Device

        const deviceTd =
            document.createElement("td");

        deviceTd.textContent =
            config.device_name;

        tr.appendChild(deviceTd);


        // 加密算法

        const encryptTd =
            document.createElement("td");

        encryptTd.textContent =
            config.encrypt_algorithm;

        tr.appendChild(encryptTd);


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


        // 修改配置

        const reviseOption =
            document.createElement("option");

        reviseOption.value = "revise";

        reviseOption.textContent =
            "修改配置";

        select.appendChild(reviseOption);


        // 删除配置

        const deleteOption =
            document.createElement("option");

        deleteOption.value = "delete";

        deleteOption.textContent =
            "删除配置";

        select.appendChild(deleteOption);


        select.addEventListener(
            "change",
            function () {

                if (
                    this.value ===
                    "revise"
                ) {

                    openReviseConfigDialog(
                        config
                    );
                }


                if (
                    this.value ===
                    "delete"
                ) {

                    showDeleteWarning();
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
        document.getElementById(
            "Pagination"
        );


    pagination.innerHTML = "";


    const totalPages =
        Math.ceil(
            displayConfig.length /
            pageSize
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

                renderConfig();
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

                renderConfig();
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

                renderConfig();
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

            renderConfig();
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

                displayConfig =
                    usersConfig;
            }
            else {

                displayConfig =
                    usersConfig.filter(
                        function (config) {

                            return (
                                String(
                                    config.user_id
                                )
                                .toLowerCase()
                                .includes(keyword)
                                ||

                                String(
                                    config.community_name
                                )
                                .toLowerCase()
                                .includes(keyword)
                                ||

                                String(
                                    config.device_name
                                )
                                .toLowerCase()
                                .includes(keyword)
                            );
                        }
                    );
            }


            currentPage = 1;

            renderConfig();
        }
    );


// ==================== 创建配置 ====================

document
    .getElementById(
        "CreateConfigButton"
    )
    .addEventListener(
        "click",
        function () {

            document
                .getElementById(
                    "CreateUserId"
                )
                .value = "";

            document
                .getElementById(
                    "CreateSupernodeIp"
                )
                .value = "";

            document
                .getElementById(
                    "CreateSupernodePort"
                )
                .value = "";

            document
                .getElementById(
                    "CreateCommunityName"
                )
                .value = "";

            document
                .getElementById(
                    "CreateDeviceName"
                )
                .value = "";

            document
                .getElementById(
                    "CreatePassword"
                )
                .value = "";

            document
                .getElementById(
                    "CreateCommunityKey"
                )
                .value = "";

            document
                .getElementById(
                    "CreateEncryptAlgorithm"
                )
                .value = "";


            document
                .getElementById(
                    "CreateConfigDialog"
                )
                .showModal();
        }
    );


// 取消创建

document
    .getElementById(
        "CancelCreateConfigButton"
    )
    .addEventListener(
        "click",
        function () {

            document
                .getElementById(
                    "CreateConfigDialog"
                )
                .close();
        }
    );


// 提交创建

document
    .getElementById(
        "CreateConfigForm"
    )
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


            const userId =
                Number(
                    document
                        .getElementById(
                            "CreateUserId"
                        )
                        .value
                );


            const supernodeIp =
                document
                    .getElementById(
                        "CreateSupernodeIp"
                    )
                    .value
                    .trim();


            const supernodePort =
                Number(
                    document
                        .getElementById(
                            "CreateSupernodePort"
                        )
                        .value
                );


            const communityName =
                document
                    .getElementById(
                        "CreateCommunityName"
                    )
                    .value
                    .trim();


            const deviceName =
                document
                    .getElementById(
                        "CreateDeviceName"
                    )
                    .value
                    .trim();


            const password =
                document
                    .getElementById(
                        "CreatePassword"
                    )
                    .value;


            const communityKey =
                document
                    .getElementById(
                        "CreateCommunityKey"
                    )
                    .value;


            const encryptAlgorithm =
                Number(
                    document
                        .getElementById(
                            "CreateEncryptAlgorithm"
                        )
                        .value
                );


            if (
                !Number.isInteger(userId) ||
                userId <= 0
            ) {

                showApiError(
                    "InvalidID",
                    "无效的用户 ID"
                );

                return;
            }


            if (
                !Number.isInteger(
                    supernodePort
                ) ||
                supernodePort < 1 ||
                supernodePort > 65535
            ) {

                showApiError(
                    "InvalidPort",
                    "Supernode 端口无效"
                );

                return;
            }


            if (
                !Number.isInteger(
                    encryptAlgorithm
                ) ||
                encryptAlgorithm < 0
            ) {

                showApiError(
                    "InvalidAlgorithm",
                    "加密算法无效"
                );

                return;
            }


            const request = {

                token: token,

                user_id: userId,

                supernode_ip:
                    supernodeIp,

                supernode_port:
                    supernodePort,

                community_name:
                    communityName,

                device_name:
                    deviceName,

                password:
                    password,

                community_key:
                    communityKey,

                encrypt_algorithm:
                    encryptAlgorithm
            };


            try {

                const response =
                    await fetch(
                        "/api/create_config",
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
                    "创建用户配置失败",
                    response.ok,
                    response.status
                )) {

                    return;
                }


                document
                    .getElementById(
                        "CreateConfigDialog"
                    )
                    .close();

                showApiSuccess("用户配置创建成功");


                await loadConfig();

            }
            catch (error) {

                showApiError(
                    "Network",
                    "无法连接到服务器"
                );
            }
        }
    );


// ==================== 修改配置 ====================

function openReviseConfigDialog(config) {

    /*
     * 保存原来的用户 ID。
     *
     * 修改配置时不能修改 user_id。
     */

    editingUserId =
        config.user_id;


    document
        .getElementById(
            "ReviseUserId"
        )
        .value =
        config.user_id;


    document
        .getElementById(
            "ReviseSupernodeIp"
        )
        .value =
        config.supernode_ip;


    document
        .getElementById(
            "ReviseSupernodePort"
        )
        .value =
        config.supernode_port;


    document
        .getElementById(
            "ReviseCommunityName"
        )
        .value =
        config.community_name;


    document
        .getElementById(
            "ReviseDeviceName"
        )
        .value =
        config.device_name;


    document
        .getElementById(
            "RevisePassword"
        )
        .value =
        config.password;


    document
        .getElementById(
            "ReviseCommunityKey"
        )
        .value =
        config.community_key;


    document
        .getElementById(
            "ReviseEncryptAlgorithm"
        )
        .value =
        config.encrypt_algorithm;


    document
        .getElementById(
            "ReviseConfigDialog"
        )
        .showModal();
}


// 取消修改

document
    .getElementById(
        "CancelReviseConfigButton"
    )
    .addEventListener(
        "click",
        function () {

            document
                .getElementById(
                    "ReviseConfigDialog"
                )
                .close();


            editingUserId = null;
        }
    );


// 提交修改

document
    .getElementById(
        "ReviseConfigForm"
    )
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
             * 不从 ReviseUserId 输入框取 ID。
             *
             * editingUserId 是原配置的 ID。
             */

            const userId =
                editingUserId;


            if (
                !Number.isInteger(
                    userId
                ) ||
                userId <= 0
            ) {

                showApiError(
                    "InvalidID",
                    "无效的用户 ID"
                );

                return;
            }


            const supernodeIp =
                document
                    .getElementById(
                        "ReviseSupernodeIp"
                    )
                    .value
                    .trim();


            const supernodePort =
                Number(
                    document
                        .getElementById(
                            "ReviseSupernodePort"
                        )
                        .value
                );


            const communityName =
                document
                    .getElementById(
                        "ReviseCommunityName"
                    )
                    .value
                    .trim();


            const deviceName =
                document
                    .getElementById(
                        "ReviseDeviceName"
                    )
                    .value
                    .trim();


            const password =
                document
                    .getElementById(
                        "RevisePassword"
                    )
                    .value;


            const communityKey =
                document
                    .getElementById(
                        "ReviseCommunityKey"
                    )
                    .value;


            const encryptAlgorithm =
                Number(
                    document
                        .getElementById(
                            "ReviseEncryptAlgorithm"
                        )
                        .value
                );


            if (
                !Number.isInteger(
                    supernodePort
                ) ||
                supernodePort < 1 ||
                supernodePort > 65535
            ) {

                showApiError(
                    "InvalidPort",
                    "Supernode 端口无效"
                );

                return;
            }


            if (
                !Number.isInteger(
                    encryptAlgorithm
                ) ||
                encryptAlgorithm < 0
            ) {

                showApiError(
                    "InvalidAlgorithm",
                    "加密算法无效"
                );

                return;
            }


            const request = {

                token: token,

                user_id: userId,

                supernode_ip:
                    supernodeIp,

                supernode_port:
                    supernodePort,

                community_name:
                    communityName,

                device_name:
                    deviceName,

                password:
                    password,

                community_key:
                    communityKey,

                encrypt_algorithm:
                    encryptAlgorithm
            };


            try {

                const response =
                    await fetch(
                        "/api/revise_config",
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
                    "修改用户配置失败",
                    response.ok,
                    response.status
                )) {

                    return;
                }


                document
                    .getElementById(
                        "ReviseConfigDialog"
                    )
                    .close();


                editingUserId = null;

                showApiSuccess("用户配置修改成功");


                await loadConfig();

            }
            catch (error) {

                showApiError(
                    "Network",
                    "无法连接到服务器"
                );
            }
        }
    );


// ==================== 删除警告 ====================

function showDeleteWarning() {

    const confirmed =
        window.confirm(
            "用户配置不应该从这里删除。\n\n" +
            "如需删除用户配置，请前往“用户管理”删除对应用户。\n\n" +
            "删除用户时，后端会同时处理对应的用户配置。\n\n" +
            "是否现在前往用户管理？"
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


    window.location.href =
        "/users?token=" +
        encodeURIComponent(token);
}

// ==================== 页面初始化 ====================

loadConfig();
