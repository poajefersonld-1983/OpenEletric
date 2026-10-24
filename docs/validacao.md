# Escopo e validação

## O que os testes comprovam

A matriz de desenvolvimento contém **40 funções verificadas dentro de critérios delimitados**. O texto dos critérios está preservado em [MATRIZ-FUNCOES.csv](../analise/MATRIZ-FUNCOES.csv). Isso não significa que toda entrada possível foi provada, que o programa é certificado ou que há equivalência integral a outro software.

A base anterior de desenvolvimento teve 53 suítes automatizadas, conferências independentes e testes de recuperação com autosave real e término abrupto. O OpenEletric acrescentou testes de integração com o desktop e a execução no [GitHub Actions para Windows](https://github.com/poajefersonld-1983/OpenEletric/actions/workflows/validacao.yml). O resultado de cada execução está no workflow; um teste em Wine não deve ser apresentado como teste Windows nativo.

## Dez cenários industriais

As referências incluem bombeamento, transportadores com inversores, forno, oficina desequilibrada, compressores com e sem compensação, gerador, UPS em dois modos e uma indústria com três níveis de quadros.

- **563 comparações nominais:** P/Q/S/FP, demanda, rendimento, correntes, queda, ampacidade e equivalentes de fonte/curto.
- **760 comparações de fluxo:** solução nodal independente em Python/SciPy, separada das varreduras do motor C#.
- **114 verificações de saídas:** totais e desenhos em PDF/TXT/DXF/SVG.

Os arquivos são sintéticos, declarados e reproduzíveis. O forno é um teste negativo proposital. O fator de potência nominal dos compressores passa de 0,8414 a 0,9500 com a compensação teórica, sem alterar a potência ativa. O FP visto na fonte pode diferir por perdas e reativos da rede.

## Limites que devem orientar o uso

|Área|Alcance|
|---|---|
|Cargas|Modelo nominal senoidal fundamental, com demanda terminal aplicada uma vez.|
|Fluxo|Rede radial por fase, quatro fios e modelos PQ/Z declarados; sem análise harmônica geral.|
|Partida|Estimativa linear por corrente e FP de partida declarados; sem dinâmica eletromecânica completa.|
|Gerador|Equivalente inicial subtransitório no módulo nominal; sem AVR ou modelo completo da máquina.|
|UPS|Inversor limitado e bypass são cenários distintos; não se presume transferência automática.|
|Proteções|Dependem de referências, curvas e associações declaradas; não se presume seletividade ou homologação OEM.|
|Invólucros|Geometria/ocupação esquemáticas, sem prova térmica/IP/3D/fabricação.|
|AMB/DXF|Inspeção e desenhos nos tipos/versões declarados; não recuperam os cálculos elétricos de outro programa.|

Um resultado **Pendente** deve ser investigado, não convertido em aprovação. Uma comparação **OK** vale para seus dados e hipóteses, não para todas as exigências de uma instalação.

## Reproduzir

No Windows, execute `app/testar.ps1` após compilar. Os scripts de conferência ficam em `ferramentas/`. Para os cenários, use `validar-industriais.py`, `validar-fluxo.py` e `validar-saidas-industriais.py` sobre as saídas geradas por `IndustrialScenarioTests`.

Os validadores que confrontam documentos de fabricante exigem a referência original e seu hash; esses documentos e protótipos proprietários não são redistribuídos.
