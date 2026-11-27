#!/usr/bin/env bash
set -Eeuo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
API_DIR="$ROOT_DIR/PrimataArchive.Api"
DB_NAME="primata_archive"

cd "$ROOT_DIR"

if ! command -v dotnet >/dev/null 2>&1; then
  echo "ERRO: .NET SDK não encontrado. Instale o SDK .NET 10 antes de executar."
  exit 1
fi
if ! dotnet --list-sdks | grep -q '^10\.'; then
  echo "ERRO: este projeto utiliza .NET 10. Instale o SDK .NET 10."
  exit 1
fi
if ! command -v mysql >/dev/null 2>&1; then
  echo "ERRO: cliente do MySQL não encontrado. No Linux Mint, instale-o com: sudo apt install mysql-client"
  exit 1
fi
if ! command -v python3 >/dev/null 2>&1; then
  echo "ERRO: Python 3 não encontrado. Ele é usado apenas para servir os arquivos do frontend."
  exit 1
fi
if ! command -v curl >/dev/null 2>&1; then
  echo "ERRO: curl não encontrado. Instale-o com: sudo apt install curl"
  exit 1
fi

read -r -p "Usuário do MySQL [root]: " DB_USER
DB_USER="${DB_USER:-root}"
read -r -s -p "Senha do MySQL (deixe vazia se não houver): " DB_PASSWORD
echo

export MYSQL_PWD="$DB_PASSWORD"
if ! mysql --protocol=TCP -h 127.0.0.1 -u "$DB_USER" -e "SELECT 1" >/dev/null 2>&1; then
  echo "ERRO: não consegui conectar ao MySQL. Confira se o serviço está iniciado e se usuário/senha estão corretos."
  unset MYSQL_PWD
  exit 1
fi

mysql --protocol=TCP -h 127.0.0.1 -u "$DB_USER" -e "CREATE DATABASE IF NOT EXISTS $DB_NAME CHARACTER SET utf8mb4;"
TABLE_EXISTS="$(mysql --protocol=TCP -h 127.0.0.1 -u "$DB_USER" -Nse "SELECT COUNT(*) FROM information_schema.tables WHERE table_schema='$DB_NAME' AND table_name='usuario';")"
if [[ "$TABLE_EXISTS" == "0" ]]; then
  echo "Criando as tabelas do Primata Archive..."
  mysql --protocol=TCP -h 127.0.0.1 -u "$DB_USER" < "$ROOT_DIR/Database/PrmtArchiveDB.sql"
else
  echo "Banco encontrado. Aplicando atualizações compatíveis..."
  mysql --protocol=TCP -h 127.0.0.1 -u "$DB_USER" < "$ROOT_DIR/Database/ATUALIZAR_BANCO_EXISTENTE.sql"
fi
unset MYSQL_PWD

export ASPNETCORE_ENVIRONMENT=Development
export ConnectionStrings__DefaultConnection="Server=127.0.0.1;Port=3306;Database=$DB_NAME;User=$DB_USER;Password=$DB_PASSWORD;"

mkdir -p "$API_DIR/wwwroot/uploads"

echo "Iniciando API em http://localhost:5199 ..."
dotnet run --project "$API_DIR/PrimataArchive.Api.csproj" --no-launch-profile --urls "http://localhost:5199" &
API_PID=$!

cleanup() {
  echo
  echo "Encerrando Primata Archive..."
  kill "$API_PID" 2>/dev/null || true
}
trap cleanup EXIT INT TERM

API_READY=0
for _ in $(seq 1 40); do
  if curl -fsS http://localhost:5199/health >/dev/null 2>&1; then
    API_READY=1
    break
  fi
  if ! kill -0 "$API_PID" 2>/dev/null; then
    echo "ERRO: a API encerrou durante a inicialização. Verifique a mensagem acima."
    exit 1
  fi
  sleep 1
done

if [[ "$API_READY" != "1" ]]; then
  echo "ERRO: a API não respondeu em http://localhost:5199/health."
  exit 1
fi

echo "API pronta: http://localhost:5199/swagger"
echo "Frontend: http://127.0.0.1:5500/"
echo "Pressione Ctrl+C para encerrar os dois servidores."
python3 -m http.server 5500 --bind 127.0.0.1 --directory "$ROOT_DIR/Front"
