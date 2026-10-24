# Compilar e contribuir

## Estrutura

- `app/`: código C# 5, WinForms e testes.
- `examples/industriais/`: dez projetos sintéticos `.circuito`.
- `scripts/`: instalador Windows e preparação das fontes de teste.
- `ferramentas/`: compilação Linux/Wine, validadores independentes e receita Debian.
- `docs/`: guias e imagens reais da interface.
- `analise/`: matriz e alcance dos modelos.

O namespace interno `CircuitoStudio` foi mantido por compatibilidade do código e das verificações anteriores. O nome público é **OpenEletric**. Alterar apenas a identificação visual não altera os modelos de cálculo.

## Windows

Com .NET Framework 4.8 disponível, abra o PowerShell na pasta do repositório:

```powershell
.\app\compilar.ps1 -Platform x64 -OutputPath "$pwd\OpenEletric.exe"
.\app\testar.ps1
```

A política de scripts da sua máquina pode exigir uma configuração administrada pela sua organização. Os testes Unicode de PDF precisam de fontes com os caracteres usados; o workflow prepara fontes de teste com licenças preservadas em `test-resources/fonts`.

## Linux

O aplicativo continua sendo WinForms/.NET Framework. O caminho validado usa Wine64 e Wine Mono. `ferramentas/build-linux.py` permite selecionar `CIRCUITO_WINEPREFIX`, `CIRCUITO_WINE_MONO`, `CIRCUITO_QA_OUT` e `CIRCUITO_QA_PLATFORM=x64`. Os testes gráficos devem usar um display isolado.

A receita `ferramentas/debian/build-deb.py --help` recebe o executável PE32+, o runtime Wine Mono e seu arquivo completo de fontes correspondente. O runtime não é mantido no Git; a receita verifica o hash da versão prevista e inclui as licenças/fontes no pacote. A instalação não inicializa Wine como root e a remoção não apaga dados pessoais.

## Alterações e contribuições

1. Abra uma issue com um exemplo reproduzível.
2. Explique a hipótese e o resultado esperado, incluindo unidades.
3. Preserve os critérios da matriz e a leitura de projetos existentes.
4. Teste os módulos afetados e acrescente uma referência independente quando mudar uma fórmula.
5. Não transforme dados desconhecidos em zero ou aprovação silenciosa.

Não envie senhas, tokens, projetos reais de clientes ou binários/documentos proprietários sem permissão. Dependências mantêm suas próprias licenças; veja `THIRD_PARTY_NOTICES.md`.
