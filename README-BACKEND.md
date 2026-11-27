# Primata Archive — execução local

O projeto utiliza frontend HTML/CSS/JavaScript, API ASP.NET Core (.NET 10) e banco de dados MySQL.

## Requisitos

- Linux Mint ou outra distribuição Linux;
- .NET SDK 10;
- MySQL Server em execução;
- cliente de terminal do MySQL (`mysql`);
- Python 3 (para servir o frontend localmente).

## Forma recomendada de iniciar

Na pasta raiz do projeto, execute:

```bash
./iniciar.sh
```

O script:

1. solicita usuário e senha do MySQL;
2. cria o banco `primata_archive` se ele ainda não existir;
3. importa o esquema SQL na primeira execução ou aplica as atualizações compatíveis em um banco já criado;
4. inicia a API em `http://localhost:5199`;
5. inicia o frontend em `http://127.0.0.1:5500`.

Acesse:

- Frontend: http://127.0.0.1:5500/
- Swagger: http://localhost:5199/swagger
- Verificação da API: http://localhost:5199/health

Mantenha o terminal aberto enquanto estiver usando o sistema. Pressione `Ctrl+C` para encerrar os dois servidores.

## Execução manual

Terminal 1 — API:

```bash
cd PrimataArchive.Api
export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection='Server=127.0.0.1;Port=3306;Database=primata_archive;User=root;Password=SUA_SENHA;'
dotnet restore
dotnet run --no-launch-profile --urls http://localhost:5199
```

Terminal 2 — frontend:

```bash
python3 -m http.server 5500 --bind 127.0.0.1 --directory Front
```

Depois acesse http://127.0.0.1:5500/.

## Banco de dados

O esquema novo está em `Database/PrmtArchiveDB.sql`. Para uma instalação existente, o script `Database/ATUALIZAR_BANCO_EXISTENTE.sql` adiciona as colunas novas sem apagar registros.

O banco de desenvolvimento cria uma conta administrativa inicial:

- E-mail: `admin@primataarchive.com`
- Senha: `Primata@2026`

Essa senha é apenas para demonstração local. Para conceder acesso administrativo a outra conta, execute no MySQL (substituindo o e-mail):

```sql
UPDATE primata_archive.usuario
SET tipo_usuario = 'admin'
WHERE email = 'seu-email@exemplo.com';
```

Depois de alterar o tipo da conta, saia e entre novamente para receber um token JWT com a permissão atualizada.

## Configuração da API

- Base URL usada pelo frontend: `http://localhost:5199/api`;
- a URL pode ser substituída antes dos scripts da página com `window.PRIMATA_API_URL`;
- CORS permite origens HTTP locais em `localhost` e `127.0.0.1`, incluindo portas do Live Server;
- a chave JWT de desenvolvimento fica em `appsettings.Development.json`. Não reutilize essa chave em produção;
- a senha do MySQL não deve ser commitada. O script `iniciar.sh` solicita a senha localmente.

## Observações

- Arquivos de capa e áudio enviados pelo formulário de lançamento são gravados em `PrimataArchive.Api/wwwroot/uploads` e servidos pela API.
- O cadastro de lançamento cria/associa um perfil de artista à conta autenticada quando ainda não existe.
- O projeto não inclui dados musicais fictícios. As telas de catálogo exibem os dados presentes no banco.
