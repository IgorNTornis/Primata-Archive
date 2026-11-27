/* PRIMATA ARCHIVE - CLIENTE HTTP DO BACKEND */
(function () {
    "use strict";

    var configuredUrl = window.PRIMATA_API_URL || "http://localhost:5199/api";
    var API_BASE_URL = configuredUrl.replace(/\/+$/, "");

    async function request(endpoint, options) {
        options = options || {};
        var config = {
            method: options.method || "GET",
            headers: Object.assign({}, options.headers || {})
        };

        var isFormData = typeof FormData !== "undefined" && options.body instanceof FormData;
        if (options.body !== undefined && !isFormData)
            config.headers["Content-Type"] = "application/json";

        var token = localStorage.getItem("primataToken");
        if (token) config.headers.Authorization = "Bearer " + token;

        if (options.body !== undefined)
            config.body = isFormData ? options.body : JSON.stringify(options.body);

        var response;
        var url = API_BASE_URL + (endpoint.charAt(0) === "/" ? endpoint : "/" + endpoint);

        try {
            response = await fetch(url, config);
        } catch (error) {
            throw new Error("Não foi possível conectar ao backend em " + API_BASE_URL + ". Confira se a API está em execução.");
        }

        var data = null;
        if (response.status !== 204) {
            var responseText = await response.text();
            if (responseText) {
                var contentType = response.headers.get("content-type") || "";
                if (contentType.includes("application/json")) {
                    try { data = JSON.parse(responseText); }
                    catch (_) { data = responseText; }
                } else {
                    data = responseText;
                }
            }
        }

        if (response.status === 401) {
            localStorage.removeItem("primataToken");
            localStorage.removeItem("primataUsuario");
        }

        if (!response.ok) {
            var message = data && (data.message || data.title || data.detail)
                ? (data.message || data.title || data.detail)
                : (typeof data === "string" && data ? data : "Erro na API (HTTP " + response.status + ").");
            throw new Error(message);
        }

        return data;
    }

    function mediaUrl(value) {
        if (!value) return "";
        var url = String(value);
        if (/^https?:\/\//i.test(url) || url.startsWith("data:")) return url;
        try { return new URL(url, new URL(API_BASE_URL).origin).href; }
        catch (_) { return url; }
    }

    window.PrimataAPI = {
        request: request,
        mediaUrl: mediaUrl,
        get: function (endpoint) { return request(endpoint); },
        post: function (endpoint, body) { return request(endpoint, { method: "POST", body: body }); },
        put: function (endpoint, body) { return request(endpoint, { method: "PUT", body: body }); },
        patch: function (endpoint, body) { return request(endpoint, { method: "PATCH", body: body }); },
        delete: function (endpoint) { return request(endpoint, { method: "DELETE" }); },
        upload: function (endpoint, formData, method) { return request(endpoint, { method: method || "POST", body: formData }); },
        baseUrl: API_BASE_URL
    };

    window.mostrarErroAPI = function (error) {
        console.error(error);
        alert(error && error.message ? error.message : "Ocorreu um erro ao comunicar com o servidor.");
    };
})();
