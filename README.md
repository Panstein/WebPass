# Controle de Viagens

Aplicação Blazor WebAssembly PWA com API ASP.NET Core e acesso ao PostgreSQL mantido no servidor.

## Executar localmente

1. A API carrega a connection string de `ControleViagens.Api/appsettings.Development.json`.
2. Inicie a API com `dotnet run --project ControleViagens.Api --launch-profile http`.
3. Em outro terminal, inicie a PWA com `dotnet run --project ControleViagens.Client --launch-profile http`.
4. Acesse `http://localhost:5200`. O endpoint `http://localhost:5102/api/health` testa a conexão ao banco.

O JSON de desenvolvimento contém credenciais e está excluído do Git. Não mova a connection string para o cliente Blazor nem publique esse arquivo.

Se o health check responder `503` com `no pg_hba.conf entry ... no encryption`, o PostgreSQL recebeu a tentativa, mas não permite a origem do cliente sem TLS. O administrador do banco precisa autorizar o IP de origem no `pg_hba.conf` ou orientar uma configuração TLS aprovada.

## Publicar para testar fora do VS Code

1. Execute `Publicar-ControleDeViagens.bat`. O script publica API e PWA em `dist/ControleViagens` como um pacote Windows x64 autocontido.
2. Abra `dist/ControleViagens/Executar-ControleDeViagens.bat`.
3. Acesse `http://localhost:5102` ou execute `dist/ControleViagens/Abrir-ControleDeViagens-TelaCheia.bat` para abrir no Edge em modo quiosque usando a resolução atual do monitor. Nesta estação, ela é `1920×1080`.
4. Para encerrar o Edge em modo quiosque, pressione `Alt+F4`; na janela do aplicativo, pressione `Ctrl+C` para parar a API.

A publicação usa a configuração local `ControleViagens.Api/appsettings.Development.json`, que não é versionada, para manter a connection string fora do cliente. Distribua a pasta `dist/ControleViagens` somente em ambiente confiável. A publicação Windows x64 é independente do VS Code e do .NET instalado, mas exige acesso de rede ao PostgreSQL configurado.

## Modelo conhecido

As tabelas informadas são `passageiro`, `trechos` e `viagem`. Colunas, chaves e schema ainda precisam ser confirmados no PostgreSQL antes de implementar consultas e gravações.

## Deploy no Render

O `Dockerfile` publica API e PWA numa única imagem (a API serve os arquivos da PWA na mesma origem). O `render.yaml` define o serviço web.

1. Envie o repositório para o GitHub/GitLab.
2. No Render: **New > Blueprint** e selecione o repositório (ou **New > Web Service** com runtime Docker).
3. Defina a variável secreta `ConnectionStrings__WebPass` com a connection string do Neon (a URI `postgresql://...` é aceita).
4. O health check do Render usa `/api/health`.
