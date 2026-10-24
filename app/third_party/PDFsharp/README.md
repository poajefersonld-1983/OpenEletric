# PDFsharp incorporado

PDFsharp-GDI 6.2.4 e dependências foram obtidos dos pacotes oficiais em `https://api.nuget.org/v3-flatcontainer/`. Os nupkg originais, nuspec e DLLs utilizados estão nesta pasta. `manifest.json` registra pacote, versão, framework e SHA256; `dependencias.ps1` verifica os hashes das DLLs antes de compilar.

As bibliotecas do núcleo utilizadas são PdfSharp-gdi, PdfSharp.Shared e PdfSharp.System. Dependências declaradas pelos pacotes também são incorporadas para execução isolada. Componentes de exemplos, gráficos, códigos de barras, qualidade e assinatura PDF não são utilizados pelo aplicativo. A presença dos nupkg preserva a origem; não é requisito para executar o EXE.

A licença MIT do PDFsharp foi obtida da tag oficial `v6.2.4`: https://raw.githubusercontent.com/empira/PDFsharp/v6.2.4/LICENSE . `LICENSES.txt` agrega as licenças/avisos dos componentes incorporados e é incluído no EXE com hash verificado na compilação.

O código do aplicativo contém a correção pós-gravação dos mapas ToUnicode para caracteres fora do BMP, sem alterar as DLLs distribuídas. O aplicativo usa .NET Framework 4.8; Python/PyMuPDF são usados somente na validação independente dos PDFs gerados e não são incorporados ao programa.
