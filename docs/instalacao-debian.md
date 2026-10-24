# OpenEletric no Debian

A edição é destinada ao **Debian 13 amd64**. Usa o aplicativo WinForms em Wine64, com Wine Mono incluído no instalador; não é uma interface GTK/Qt nativa.

## Instalação

1. Baixe `openeletric_0.2.0-1_amd64.deb` na [última versão](https://github.com/poajefersonld-1983/OpenEletric/releases/latest).
2. Na pasta do download, execute:

```bash
sudo apt install ./openeletric_0.2.0-1_amd64.deb
```

3. Abra **OpenEletric** no menu de aplicativos. Para abrir pelo terminal:

```bash
openeletric
```

O pacote é maior porque inclui o runtime e o código-fonte correspondente das dependências. O Wine64 e as demais dependências do Debian são resolvidos pelo `apt`. A primeira abertura prepara o ambiente privado do usuário, sem baixar o runtime novamente.

![Interface da aplicação](images/visao-geral.png)

## Projetos e exemplos

Os projetos ficam na pasta de documentos do usuário, em **OpenEletric**. Os dez exemplos são copiados para **OpenEletric/Exemplos** sem substituir arquivos existentes. Um arquivo `.circuito` também pode ser aberto pelo gerenciador de arquivos.

Não execute o aplicativo com `sudo`.

## Remoção

```bash
sudo apt remove openeletric
```

A remoção do aplicativo preserva os projetos e o perfil do usuário. O ambiente do programa fica em `~/.local/share/openeletric`, e o registro da última execução em `~/.local/state/openeletric/ultima-execucao.log`, respeitando as variáveis XDG quando definidas.

## Para relatar um problema

Informe a versão do Debian, a versão do OpenEletric e a mensagem apresentada. O registro pode ajudar, mas revise e remova caminhos ou dados pessoais antes de compartilhar.
