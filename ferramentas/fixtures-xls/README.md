# Geração independente de amostras XLS

`node gerar-amostras.mjs` gera arquivos BIFF8 sintéticos para os testes de catálogo/cabos em `app/amostras-testes/xls`. Todos os valores são apenas dados de ensaio.

SheetJS CE 0.20.3 foi obtido do distribuidor oficial em https://cdn.sheetjs.com/xlsx-0.20.3/package/xlsx.mjs. O texto legal acompanha `LICENSE.txt`. Este componente é usado somente para gerar as amostras; não é incorporado nem necessário para executar o Circuito Studio ou rodar os testes com as amostras existentes.
