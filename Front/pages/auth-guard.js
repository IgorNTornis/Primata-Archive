(function () {
    "use strict";

    var path = window.location.pathname.toLowerCase();
    var isLoginPage = path.indexOf("/login/") !== -1 || path.endsWith("/login/index.html");
    if (isLoginPage) return;

    var protectedPage = /\/(perfil|editar-perfil|notificacoes|configuracoes|biblioteca|biblioteca-musicas|biblioteca-artistas|biblioteca-albuns|biblioteca-playlists|playlist-usuario|dashboard-artista|estatisticas-artista|gerenciar-musicas|gerenciar-conteudo|gerenciar-usuarios|gerenciar-comunidades|dashboard-administrativo|denuncias|criar-lancamento)\//.test(path);
    if (!protectedPage) return;

    var token = localStorage.getItem("primataToken");
    var adminPage = /\/(dashboard-administrativo|gerenciar-usuarios|gerenciar-conteudo|gerenciar-comunidades|denuncias)\//.test(path);

    function showMessage(title, message, linkText, href) {
        document.addEventListener("DOMContentLoaded", function () {
            var main = document.querySelector("main");
            if (!main) return;
            main.innerHTML = '<section class="auth-required"><span class="mini-title">PRIMATA ARCHIVE // ACCESS CONTROL</span><h1>' + title + '</h1><p>' + message + '</p><a class="pixel-button" href="' + href + '">' + linkText + '</a></section>';
            document.body.classList.add("auth-required-page");
        });
    }

    if (!token) {
        showMessage("VOCÊ NÃO<br>ESTÁ LOGADO", "Entre na sua conta para acessar esta área.", "ENTRAR NA CONTA", "../login/index.html");
        return;
    }

    if (adminPage) {
        var usuario = null;
        try { usuario = JSON.parse(localStorage.getItem("primataUsuario") || "null"); } catch (_) {}
        if (!usuario || String(usuario.tipoUsuario || "").toLowerCase() !== "admin") {
            showMessage("ACESSO<br>RESTRITO", "Esta área é exclusiva para contas administradoras.", "VOLTAR À HOME", "../landing/index.html");
        }
    }
})();
