USE primata_archive;

DROP PROCEDURE IF EXISTS primata_archive_aplicar_atualizacoes;
DELIMITER $$
CREATE PROCEDURE primata_archive_aplicar_atualizacoes()
BEGIN

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'comunidade' AND column_name = 'identificador'
    ) THEN
        ALTER TABLE comunidade ADD COLUMN identificador VARCHAR(100) NULL AFTER nome;
        UPDATE comunidade SET identificador = CONCAT('comunidade-', id_comunidade) WHERE identificador IS NULL OR identificador = '';
        ALTER TABLE comunidade MODIFY COLUMN identificador VARCHAR(100) NOT NULL;
        ALTER TABLE comunidade ADD UNIQUE KEY uq_comunidade_identificador (identificador);
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'comunidade' AND column_name = 'categoria'
    ) THEN
        ALTER TABLE comunidade ADD COLUMN categoria VARCHAR(50) NULL AFTER identificador;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'comunidade' AND column_name = 'privacidade'
    ) THEN
        ALTER TABLE comunidade ADD COLUMN privacidade ENUM('publica', 'privada') NOT NULL DEFAULT 'publica' AFTER categoria;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'album' AND column_name = 'genero'
    ) THEN
        ALTER TABLE album ADD COLUMN genero VARCHAR(50) NULL AFTER id_genero;
    END IF;


    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'album' AND column_name = 'status'
    ) THEN
        ALTER TABLE album ADD COLUMN status ENUM('rascunho', 'publicado') NOT NULL DEFAULT 'publicado' AFTER tipo;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'musica' AND column_name = 'genero'
    ) THEN
        ALTER TABLE musica ADD COLUMN genero VARCHAR(50) NULL AFTER duracao;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'musica' AND column_name = 'audio_url'
    ) THEN
        ALTER TABLE musica ADD COLUMN audio_url VARCHAR(255) NULL AFTER capa;
    END IF;

    IF NOT EXISTS (
        SELECT 1 FROM information_schema.columns
        WHERE table_schema = DATABASE() AND table_name = 'post' AND column_name = 'status'
    ) THEN
        ALTER TABLE post ADD COLUMN status ENUM('rascunho', 'publicado') NOT NULL DEFAULT 'publicado' AFTER imagem;
    END IF;
END$$
DELIMITER ;

CALL primata_archive_aplicar_atualizacoes();
DROP PROCEDURE IF EXISTS primata_archive_aplicar_atualizacoes;

ALTER TABLE artista MODIFY COLUMN email_artista VARCHAR(150) NOT NULL;

INSERT IGNORE INTO usuario (nome, email, senha, tipo_usuario)
VALUES ('Administrador', 'admin@primataarchive.com', '$2y$10$o7Vg9PNay5H3YIVW1I.jA.CJFSxF5xQ5q5w8X0U2Rvnznd.JGtopq', 'admin');

-- Corrige a senha em texto puro criada pelo script antigo (caso ainda exista).
UPDATE usuario
SET senha = '$2y$10$o7Vg9PNay5H3YIVW1I.jA.CJFSxF5xQ5q5w8X0U2Rvnznd.JGtopq'
WHERE email = 'admin@primataarchive.com' AND senha = '123456';
