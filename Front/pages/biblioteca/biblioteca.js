document.addEventListener("DOMContentLoaded", async function () {
    var lista = document.getElementById("library-list");
    if (!lista) return;
    try {
        var dados = await PrimataAPI.get("/biblioteca");
        var musicas = Array.isArray(dados) ? dados : (dados.musicas || []);
        if (!musicas.length) return;
        lista.innerHTML = "";
        musicas.forEach(function (musica, index) {
            lista.innerHTML += '<div class="track" data-audio-url="' + (musica.audioUrl || musica.AudioUrl || "") + '" data-cover-url="' + (musica.capa || musica.Capa || "") + '"><span class="track-number">' + String(index + 1).padStart(2, "0") + '</span><div class="track-cover">' + (index + 1) + '</div><div class="track-data"><strong>' + (musica.titulo || musica.Titulo || musica.nome || "") + '</strong><span>' + (musica.artista || musica.Artista || musica.nomeArtista || "Artista") + '</span></div><span class="track-album">' + (musica.album || musica.Album || "-") + '</span><span class="track-time">' + (musica.duracao || "--:--") + '</span><button class="track-play">▶</button></div>';
        });
        if (window.PrimataPlayer) window.PrimataPlayer.refresh();
    } catch (error) {
        console.log("Biblioteca aguardando backend.");
    }
});
