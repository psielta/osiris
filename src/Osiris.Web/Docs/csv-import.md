# Importar extrato em CSV

Nem todo banco oferece OFX confiável. Bancos digitais como **Nubank, Inter, C6 e Mercado Pago** costumam exportar o extrato em **CSV**. A importação por CSV cobre esses casos: você indica em qual coluna está a data, a descrição e o valor, e o Osiris transforma cada linha em um lançamento da conta.

> CSV é uma planilha em texto. Como cada banco organiza as colunas de um jeito, você faz o **mapeamento** uma vez — e o Osiris lembra dele para a próxima importação nesta conta.

## O que é o arquivo CSV

CSV (final `.csv`) é o extrato em formato de planilha: cada linha é uma movimentação e cada coluna é um campo (data, histórico, valor, saldo…). Procure no seu banco por algo como **"Exportar extrato"**, **"Baixar CSV"** ou **"Exportar para Excel/planilha"**.

## Como importar

A importação acontece em etapas, para você revisar antes de salvar:

1. Na conta, escolha **Importar CSV** e selecione o arquivo `.csv` baixado do banco.
2. **Mapeie as colunas:** diga qual coluna é a Data, a Descrição e o Valor.
3. Toque em **Pré-visualizar** para ver os lançamentos encontrados.
4. Revise a lista, escolha as categorias e confirme.

No computador, abra a conta e clique em **Importar CSV**. No celular, abra o extrato da conta, toque em **Importar** e escolha **Importar CSV**.

## Mapear as colunas

Na tela de mapeamento o Osiris já mostra uma amostra do arquivo e tenta adivinhar as colunas. Confira e ajuste:

- **Separador e codificação:** quase sempre detectados automaticamente (no Brasil o separador costuma ser `;`). Se a amostra aparecer embaralhada, troque e use **Atualizar amostra**.
- **Linha do cabeçalho:** muitos extratos têm linhas de título antes da tabela. Indique em qual linha estão os nomes das colunas; o que vier antes é ignorado.
- **Data, Descrição e Valor:** escolha a coluna de cada campo. Colunas que você não usar (como **Saldo**) é só não mapear.
- **Como o valor está no arquivo:**
  - **Valor com sinal** — uma única coluna onde negativo é despesa e positivo é receita.
  - **Crédito e débito separados** — duas colunas, uma para entradas e outra para saídas.
  - **Coluna de tipo** — uma coluna de texto (ex.: C/D, crédito/débito) que define se é entrada ou saída.
- **Formato da data e separador decimal:** confirme o formato (ex.: `dd/MM/aaaa`) e o separador decimal (vírgula no padrão brasileiro).

Linhas que não têm uma data e um valor válidos — como rodapés de "Total" ou linhas de saldo — são ignoradas automaticamente.

## O mapeamento fica salvo

Depois da primeira importação, o Osiris **lembra do mapeamento desta conta**. Na próxima vez que você importar um CSV do mesmo banco, os campos já vêm preenchidos — é só conferir e pré-visualizar.

## Revisar antes de confirmar

Na tela de revisão você decide o que entra:

- **Marcar e desmarcar:** cada linha tem uma caixa de seleção. Desmarque o que não quiser importar.
- **Categoria:** escolha a categoria de cada lançamento, ou use "Categoria para todos". A categoria é opcional.
- **Tipo:** entradas viram receita e saídas viram despesa, conforme o mapeamento que você escolheu.

Nada é salvo até você confirmar.

## Lançamentos repetidos não entram duas vezes

O Osiris identifica cada movimentação por uma chave estável (a partir da data, do valor e da descrição — ou de uma coluna de identificador, se o seu banco tiver uma). Se você importar o mesmo período de novo, os repetidos aparecem como **"Já importado"** e vêm desmarcados. É a mesma proteção da importação OFX, então dá para reimportar sem medo de duplicar o saldo.

## Depois de importar

Os lançamentos entram no extrato da conta como qualquer outro, e o saldo é atualizado na hora. Como todo lançamento, eles **não podem ser editados nem excluídos** depois — se algo veio errado do banco, registre um lançamento de correção no sentido contrário.

## Resumo

1. Baixe o extrato em CSV no seu banco.
2. Abra a conta e escolha Importar CSV.
3. Mapeie as colunas (data, descrição, valor) e pré-visualize.
4. Revise, escolha categorias e confirme.
5. Nas próximas vezes, o mapeamento já vem salvo e os repetidos não entram de novo.
