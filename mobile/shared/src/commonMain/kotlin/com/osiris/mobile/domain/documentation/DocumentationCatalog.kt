package com.osiris.mobile.domain.documentation

data class DocumentationGuide(
    val slug: String,
    val title: String,
    val description: String,
    val group: String,
    val order: Int,
    val markdown: String,
)

object DocumentationCatalog {
    val guides: List<DocumentationGuide> = listOf(
        DocumentationGuide(
            slug = "getting-started",
            title = "Primeiros passos no Osiris",
            description = "Configure o básico para começar a acompanhar contas, cartões, gastos e saldo previsto.",
            group = "Começando",
            order = 1,
            markdown = """
# Primeiros passos no Osiris

O Osiris organiza a vida financeira em poucas partes. A ideia é separar o que é dinheiro disponível, o que é gasto, o que é dívida do cartão e o que ainda precisa ser pago.

## Sequência recomendada

Comece por esta ordem:

1. Confira as categorias.
2. Cadastre suas contas financeiras.
3. Cadastre seus cartões de crédito.
4. Registre compras no cartão e contas a pagar.
5. Acompanhe o painel e os relatórios.

Essa ordem evita confusão. Categorias explicam o motivo de cada entrada ou saída, contas mostram onde o dinheiro está, cartões guardam compras que serão pagas depois e contas a pagar registram obrigações fora do cartão.

## 1. Conferir categorias

O Osiris já cria categorias padrão para facilitar o começo. Acesse **Categorias** e veja se elas combinam com sua rotina.

Use categorias simples no início:

| Situação | Categoria comum |
| --- | --- |
| Salário recebido | Salário |
| Supermercado | Mercado |
| Restaurante | Alimentação |
| Aluguel | Moradia |
| Internet, streaming ou academia | Assinaturas |
| Ônibus, combustível ou aplicativo | Transporte |

Se uma categoria não fizer sentido, você pode editar, arquivar ou apagar quando ela ainda não foi usada. Depois que existe histórico, arquivar costuma ser melhor do que apagar.

Leia também: [Guia simples de categorias](/docs/categories).

## 2. Cadastrar contas financeiras

Depois, cadastre os lugares onde seu dinheiro fica. Pode ser banco, poupança, carteira ou outra conta.

Ao criar uma conta, informe o saldo inicial com o valor que existe nela naquele momento. A partir daí, o saldo passa a mudar conforme você registra receitas, despesas diretas, pagamentos de contas e pagamentos de faturas.

Não use a conta para registrar compra no cartão. Compra no cartão entra na área de cartão e só afeta a conta quando a fatura for paga.

Leia também: [Guia simples de contas](/docs/accounts).

## 3. Cadastrar cartões de crédito

Cadastre cada cartão com nome, limite, dia de fechamento e dia de vencimento. Se você costuma pagar a fatura sempre pela mesma conta, escolha essa conta como conta de pagamento padrão.

O cartão não guarda dinheiro. Ele representa uma forma de comprar agora e pagar depois. Por isso, a compra entra na fatura e o dinheiro sai da conta apenas no pagamento da fatura.

Leia também: [Guia simples de cartões](/docs/cards).

## 4. Registrar gastos do mês

Depois da configuração inicial, o uso diário fica em dois caminhos:

| O que aconteceu | Onde registrar |
| --- | --- |
| Compra no cartão de crédito | Compras no cartão |
| Parcela de compra no cartão | Compras no cartão, informando o número de parcelas |
| Aluguel, internet, escola, boleto ou assinatura fora do cartão | Contas a pagar |
| Entrada de dinheiro na conta | Extrato da conta |
| Saída direta da conta que não é fatura nem conta a pagar | Extrato da conta |
| Pagamento de fatura | Fatura do cartão |
| Pagamento de conta fora do cartão | Conta a pagar |

Essa separação é importante para o mesmo gasto não aparecer duas vezes.

## 5. Acompanhar o painel

O painel mostra duas leituras ao mesmo tempo:

- **Gastos do mês:** compras no cartão, contas a pagar e despesas diretas, separados por categoria.
- **Caixa do mês:** dinheiro que entrou ou saiu das contas e o saldo previsto depois de pagar obrigações em aberto.

Se o saldo previsto estiver negativo, isso não quer dizer que você já está sem dinheiro. Quer dizer que, considerando o saldo atual e o que ainda vence no mês, pode faltar dinheiro se tudo for pago.

Leia também: [Como ler o painel](/docs/dashboard).

## Rotina simples de uso

Uma rotina semanal suficiente para a maioria dos casos é:

1. Registrar compras novas no cartão.
2. Registrar contas fora do cartão quando elas aparecerem.
3. Marcar contas pagas.
4. Pagar faturas quando chegar o vencimento.
5. Conferir o painel do mês.

No fim do mês, use **Relatórios** para baixar uma visão de caixa em PDF se quiser revisar ou guardar um resumo.

## Onde cada guia ajuda

| Guia | Quando usar |
| --- | --- |
| Categorias | Quando quiser organizar receitas e gastos por motivo |
| Contas | Quando precisar entender saldo, extrato e lançamentos |
| Cartões | Quando estiver configurando cartão, limite e vencimento |
| Compras no cartão | Quando for registrar compra à vista ou parcelada |
| Faturas | Quando for acompanhar ou pagar a fatura |
| Contas a pagar | Quando for controlar boleto, aluguel, escola, assinatura ou outra obrigação fora do cartão |
| Relatórios | Quando quiser baixar PDF do caixa mensal |

## Regra principal

Se tiver dúvida, pergunte:

1. Isso é dinheiro real entrando ou saindo de uma conta agora?
2. Isso é uma compra no cartão para pagar depois?
3. Isso é uma obrigação fora do cartão com vencimento?

A resposta normalmente indica onde registrar.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "dashboard",
            title = "Como ler o painel",
            description = "Entenda os cartões, alertas, gráficos e a diferença entre visão de gastos e visão de caixa.",
            group = "Começando",
            order = 2,
            markdown = """
# Como ler o painel

O painel resume o mês escolhido. Ele junta gastos, saldos, faturas, contas a pagar e alertas para você entender a situação sem abrir cada tela separadamente.

## Filtro de mês e ano

No topo do painel você escolhe mês e ano. Todos os cartões, gráficos e vencimentos da página usam esse período como referência.

Use o filtro quando quiser:

- revisar um mês anterior;
- planejar o mês atual;
- conferir obrigações que vencem em um mês futuro.

## Primeiros passos

Quando a conta ainda está sendo configurada, o painel mostra uma lista de primeiros passos. Ela indica se você já criou conta financeira, cartão, categorias ativas e algum gasto inicial.

Essa lista não muda seus dados. Ela apenas aponta o que falta para o painel ficar mais útil.

## Alertas

Os alertas aparecem quando existe algo que merece atenção, como fatura vencida, conta vencida ou saldo previsto negativo.

Use os alertas como uma lista de checagem:

| Alerta | O que fazer |
| --- | --- |
| Conta vencida | Abra Contas a pagar e marque como paga ou ajuste a conta |
| Fatura vencida | Abra Faturas e registre o pagamento |
| Saldo previsto negativo | Confira contas, faturas e saldo das contas financeiras |
| Fatura parcialmente paga | Veja se ainda falta quitar o saldo em aberto |

## Cartões principais

Os cartões de resumo mostram valores importantes do mês:

| Campo | O que significa |
| --- | --- |
| Receitas do mês | Entradas registradas nas contas financeiras |
| Gastos do mês | Compras no cartão, contas a pagar e despesas diretas |
| Saldo em contas | Soma dos saldos das contas financeiras ativas |
| Saldo previsto do mês | Saldo atual menos contas e faturas em aberto no mês |
| Contas vencendo no mês | Total de contas a pagar com vencimento no período |
| Faturas vencendo no mês | Total de faturas com vencimento no período |
| Pagamentos de fatura no mês | Saída de caixa usada para quitar faturas |
| Total parcelado futuro | Parcelas que cairão em faturas depois do mês filtrado |

## Gastos por categoria

A área de gastos por categoria responde:

> Em que eu gastei dinheiro neste mês?

Ela soma três origens:

- compras no cartão;
- contas a pagar;
- despesas diretas registradas no extrato das contas.

O pagamento da fatura não entra de novo como gasto por categoria. A compra já foi contada quando aconteceu. O pagamento da fatura aparece na visão de caixa, porque ele muda o saldo da conta.

## Visão de caixa

A visão de caixa responde:

> Como o dinheiro das minhas contas mudou ou deve mudar?

Ela considera:

- receitas recebidas;
- contas pagas;
- pagamentos de faturas;
- despesas diretas;
- contas ainda em aberto no mês;
- faturas ainda em aberto no mês.

O saldo previsto é uma estimativa simples: saldo atual das contas menos obrigações em aberto do mês.

## Vencimentos do mês

A lista de vencimentos mostra contas e faturas abertas no período filtrado. Ela ajuda a decidir o que pagar primeiro.

Se um item aparece como vencido, ele ainda não foi marcado como pago no Osiris. Depois que você registra o pagamento, ele deixa de aparecer como pendente.

## Cartões de crédito no painel

Cada cartão mostra:

- limite total;
- limite usado;
- limite disponível;
- fatura do mês filtrado;
- vencimento;
- total pago;
- saldo em aberto;
- parcelado futuro.

Use essa área para perceber cartões perto do limite ou faturas importantes antes do vencimento.

## Quando o painel parece diferente do banco

O painel depende dos registros feitos no Osiris. Ele pode diferir do aplicativo do banco quando:

- faltou registrar uma compra;
- uma fatura foi paga no banco, mas não foi marcada no Osiris;
- uma conta fora do cartão foi paga, mas continua pendente;
- o saldo inicial de uma conta foi cadastrado errado;
- houve lançamento direto na conta que ainda não foi registrado.

Nesses casos, corrija pelo fluxo correto: registre o item que faltou ou lance um ajuste no extrato da conta.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "categories",
            title = "Guia simples de categorias",
            description = "Entenda como usar categorias para organizar receitas e despesas sem termos técnicos.",
            group = "Financeiro",
            order = 10,
            markdown = """
# Guia simples de categorias

Categorias servem para responder uma pergunta simples:

> Para onde foi o dinheiro? Ou de onde o dinheiro veio?

Elas funcionam como etiquetas. Cada receita ou gasto recebe uma categoria para que você consiga enxergar seus hábitos sem precisar entender contabilidade, economia ou termos técnicos.

## O que é uma categoria?

Uma categoria é um nome que agrupa movimentações parecidas.

Por exemplo:

| Situação | Categoria sugerida |
| --- | --- |
| Compra no supermercado | Mercado |
| Aluguel da casa | Moradia |
| Salário recebido | Salário |
| Corrida por aplicativo | Transporte |
| Consulta médica | Saúde |
| Mensalidade da escola | Educação |

A categoria não é o banco, não é o cartão e não é a forma de pagamento. Ela representa o motivo da entrada ou saída de dinheiro.

## Receita e despesa

Existem dois tipos de categoria.

| Tipo | Use quando |
| --- | --- |
| Receita | O dinheiro entra para você |
| Despesa | O dinheiro sai de você |

Exemplos de receitas:

- Salário
- Freelance
- Reembolso
- Venda de algo usado
- Rendimento

Exemplos de despesas:

- Alimentação
- Mercado
- Transporte
- Moradia
- Saúde
- Lazer
- Assinaturas
- Educação

## Como escolher boas categorias

Comece simples. Um erro comum é criar categorias demais logo no início.

Uma boa primeira lista pode ser:

- Salário
- Alimentação
- Mercado
- Moradia
- Transporte
- Saúde
- Educação
- Lazer
- Assinaturas
- Outros

Depois de algumas semanas, você pode ajustar. Se "Alimentação" ficou muito grande, talvez faça sentido separar "Restaurantes" e "Mercado". Se quase nunca usa uma categoria, talvez ela não precise existir.

## Cartão de crédito não é categoria

O cartão de crédito é uma forma de pagar e uma dívida a ser quitada depois. Ele não explica o motivo do gasto.

Exemplo:

| Lançamento | Categoria correta |
| --- | --- |
| Compra no mercado com cartão | Mercado |
| Restaurante pago no cartão | Alimentação |
| Remédio comprado no cartão | Saúde |

Quando você paga a fatura do cartão, isso é pagamento de uma dívida. Não deve virar uma nova despesa por categoria, senão o mesmo gasto aparece duas vezes.

## Conta, boleto e assinatura fora do cartão

Quando a obrigação não está no cartão, use uma categoria normal de despesa.

Exemplos:

- Aluguel: Moradia
- Internet: Casa ou Assinaturas
- Academia: Saúde ou Lazer
- Escola: Educação
- Boleto de serviço: categoria relacionada ao serviço

## Quando arquivar uma categoria

Arquivar significa esconder a categoria das novas escolhas, sem apagar o histórico.

Use arquivar quando:

- você não usa mais a categoria;
- a categoria foi criada por engano, mas já existe histórico;
- você quer simplificar a lista sem perder informações antigas.

Apagar deve ser usado apenas quando a categoria ainda não tem uso real. Se ela já aparece em históricos, arquivar costuma ser a decisão mais segura.

## Para que serve a cor?

A cor ajuda a reconhecer uma categoria rapidamente. Ela não muda os cálculos.

Use cores de um jeito simples:

- cores parecidas para assuntos parecidos;
- uma cor forte para categorias que você quer acompanhar de perto;
- cores neutras para categorias pouco importantes.

## Exemplos práticos

### Exemplo 1: supermercado

Você gastou R$ 250 no supermercado.

- Tipo: Despesa
- Categoria: Mercado

### Exemplo 2: salário

Você recebeu R$ 4.000 de salário.

- Tipo: Receita
- Categoria: Salário

### Exemplo 3: compra parcelada no cartão

Você comprou um celular em 10 parcelas no cartão.

- Tipo: Despesa
- Categoria: Eletrônicos ou Compras
- Observação: as parcelas entram nas faturas do cartão, mas a categoria continua sendo a do motivo da compra.

### Exemplo 4: pagamento da fatura

Você pagou R$ 900 da fatura do cartão.

- Isso é quitação de dívida.
- Não escolha uma categoria de despesa para duplicar os gastos.
- Os gastos já foram categorizados nas compras do cartão.

## Regra simples para decidir

Se estiver em dúvida, pergunte:

1. O dinheiro entrou ou saiu?
2. Qual foi o motivo?
3. Eu vou querer ver isso separado em um relatório?

Se a resposta da terceira pergunta for "não", use uma categoria mais geral.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "accounts",
            title = "Guia simples de contas",
            description = "Saiba o que é uma conta, como registrar entradas e saídas e como o saldo é calculado.",
            group = "Financeiro",
            order = 20,
            markdown = """
# Guia simples de contas

Uma conta responde a uma pergunta simples:

> Onde o seu dinheiro está guardado?

Cada conta representa um lugar onde existe dinheiro de verdade. Pode ser o banco, a poupança ou o dinheiro na carteira. Aqui você acompanha quanto entra e quanto sai de cada lugar.

## O que é uma conta?

Uma conta é um lugar onde o dinheiro fica. Você pode ter quantas precisar.

| Situação | Tipo de conta |
| --- | --- |
| Conta do banco usada no dia a dia | Conta corrente |
| Dinheiro guardado para render | Poupança |
| Dinheiro em espécie na carteira | Dinheiro |
| Qualquer outro lugar com dinheiro | Outra |

A conta não é a categoria do gasto. A categoria explica o motivo (mercado, transporte, salário). A conta explica o lugar (banco, carteira).

## Saldo inicial e saldo atual

Quando você cria uma conta, informa o **saldo inicial**: quanto existe nela hoje.

A partir daí, o **saldo atual** muda sozinho conforme você registra os lançamentos:

- uma entrada (receita) aumenta o saldo;
- uma saída (despesa) diminui o saldo.

O saldo inicial fica fixo. Se digitou um valor errado, não edite a conta: registre um lançamento de acerto no extrato.

## O que é um lançamento?

Um lançamento é uma movimentação de dinheiro real naquela conta.

| Aconteceu | Tipo de lançamento |
| --- | --- |
| Recebeu o salário no banco | Receita |
| Pagou a feira em dinheiro | Despesa |
| Recebeu um pagamento na carteira | Receita |
| Pagou a passagem com o saldo da conta | Despesa |

Cada lançamento tem uma data, um valor e uma descrição. Você também pode escolher uma categoria para lembrar o motivo.

## E se eu errar um lançamento?

Os lançamentos funcionam como um histórico do que aconteceu na conta. Depois de salvos, eles **não podem ser editados nem excluídos**. Isso é proposital: mantém o extrato fiel à realidade e evita que o saldo deixe de bater.

Para corrigir um erro, registre um novo lançamento no sentido contrário:

| O que aconteceu | Como corrigir |
| --- | --- |
| Lançou uma despesa que não existiu | Registre uma receita do mesmo valor |
| Lançou uma receita que não existiu | Registre uma despesa do mesmo valor |
| Lançou um valor maior do que o real | Registre a diferença no sentido contrário |

Vale escrever "Correção" na descrição para se lembrar depois. Assim que você salva o lançamento de correção, o saldo da conta se ajusta sozinho e o histórico continua mostrando tudo o que aconteceu.

## O que não entra como lançamento de conta

Algumas coisas parecem gasto, mas não são saída de dinheiro da conta neste momento:

- **Compra no cartão de crédito.** A compra é registrada como gasto do cartão, não como saída da conta. O dinheiro só sai da conta quando você paga a fatura.
- **Pagamento de fatura do cartão.** Isso é quitação de dívida e será tratado na área de cartões, para não contar o mesmo gasto duas vezes.

Por enquanto, use os lançamentos da conta para o dinheiro que realmente entra e sai dela, fora do cartão.

## Arquivar uma conta

Arquivar esconde a conta das novas escolhas sem apagar o histórico. Use quando parar de usar uma conta, mas quiser manter os lançamentos antigos.

Uma conta arquivada não aceita novos lançamentos.

## Regra simples para decidir

Se estiver em dúvida se algo é um lançamento de conta, pergunte:

1. O dinheiro entrou ou saiu de verdade deste lugar agora?
2. Foi fora do cartão de crédito?

Se as duas respostas forem "sim", registre o lançamento na conta.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "ofx-import",
            title = "Importar extrato em OFX",
            description = "Baixe o extrato do banco em OFX e importe os lançamentos para a conta, revisando antes de confirmar.",
            group = "Financeiro",
            order = 25,
            markdown = """
# Importar extrato em OFX

A importação por OFX evita digitar lançamento por lançamento. Você baixa o extrato da sua conta no banco e o Osiris transforma cada movimentação em um lançamento da conta.

> OFX é um formato de extrato que quase todos os bancos brasileiros oferecem para baixar.

## O que é o arquivo OFX

OFX é um arquivo (final `.ofx`) que o seu banco gera com as movimentações de um período: data, valor e descrição de cada entrada e saída.

Quase todo internet banking ou aplicativo do banco tem uma opção de **exportar/baixar extrato** em OFX. Procure por algo como "Exportar extrato", "Baixar OFX" ou "OFX (Money/Quicken)".

## Como importar

A importação acontece em duas etapas, para você revisar antes de salvar:

1. Na conta, escolha **Importar OFX** e selecione o arquivo `.ofx` baixado do banco.
2. O Osiris lê o arquivo e mostra a lista de lançamentos encontrados.
3. Revise a lista, escolha as categorias e confirme.

No celular, abra o extrato da conta e toque em **Importar**. No computador, abra a conta e clique em **Importar OFX**.

## Revisar antes de confirmar

Na tela de revisão você decide o que entra:

- **Marcar e desmarcar:** cada linha tem uma caixa de seleção. Desmarque o que não quiser importar.
- **Categoria:** escolha a categoria de cada lançamento, ou aplique a mesma categoria a todos de uma vez. A categoria é opcional — você pode categorizar depois no extrato.
- **Tipo:** entradas viram receita e saídas viram despesa, conforme o sinal do valor no extrato.

Nada é salvo até você confirmar.

## Lançamentos repetidos não entram duas vezes

O Osiris identifica cada movimentação pelo código único que o banco coloca no arquivo. Se você importar o mesmo extrato de novo, ou um período que se sobrepõe a outro já importado, os lançamentos repetidos aparecem como **"Já importado"** e vêm desmarcados.

Assim você pode reimportar sem medo de duplicar o saldo.

## Depois de importar

Os lançamentos importados entram no extrato da conta como qualquer outro lançamento, e o saldo da conta é atualizado na hora.

Como todo lançamento, eles **não podem ser editados nem excluídos** depois. Se algo veio errado do banco, registre um lançamento de correção no sentido contrário, como faria com um lançamento manual.

## O que ainda não é importado

- **Extrato de cartão de crédito.** A importação é para o extrato de uma conta (banco, poupança, dinheiro). Compras do cartão continuam sendo registradas na área de cartões.
- **Categorias automáticas.** O Osiris não adivinha a categoria pela descrição; você escolhe na hora de importar ou depois.

## Resumo

1. Baixe o extrato em OFX no seu banco.
2. Abra a conta e escolha Importar OFX.
3. Revise, escolha categorias e confirme.
4. Reimporte quando quiser: os repetidos não entram de novo.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "csv-import",
            title = "Importar extrato em CSV",
            description = "Importe um CSV do banco mapeando as colunas (data, descrição, valor) e revise antes de confirmar.",
            group = "Financeiro",
            order = 26,
            markdown = """
# Importar extrato em CSV

Nem todo banco oferece OFX confiável. Bancos digitais como **Nubank, Inter, C6 e Mercado Pago** costumam exportar o extrato em **CSV**. A importação por CSV cobre esses casos: você indica em qual coluna está a data, a descrição e o valor, e o Osiris transforma cada linha em um lançamento da conta.

> CSV é uma planilha em texto. Como cada banco organiza as colunas de um jeito, você faz o **mapeamento** uma vez — e o Osiris lembra dele para a próxima importação nesta conta.

## O que é o arquivo CSV

CSV (final `.csv`) é o extrato em formato de planilha: cada linha é uma movimentação e cada coluna é um campo (data, histórico, valor, saldo…). Procure no seu banco por algo como **"Exportar extrato"**, **"Baixar CSV"** ou **"Exportar para Excel/planilha"**.

## Como importar

A importação acontece em etapas, para você revisar antes de salvar:

1. No celular, abra o extrato da conta, toque em **Importar** e escolha **Importar CSV**. No computador, abra a conta e clique em **Importar CSV**.
2. **Mapeie as colunas:** diga qual coluna é a Data, a Descrição e o Valor.
3. Toque em **Pré-visualizar** para ver os lançamentos encontrados.
4. Revise a lista, escolha as categorias e confirme.

## Mapear as colunas

Na tela de mapeamento o Osiris já mostra uma amostra do arquivo e tenta adivinhar as colunas. Confira e ajuste:

- **Separador e codificação:** quase sempre detectados automaticamente (no Brasil o separador costuma ser `;`). Se a amostra aparecer embaralhada, troque.
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
- **Categoria:** escolha a categoria de cada lançamento, ou aplique a mesma a todos. A categoria é opcional.
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
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "pdf-import",
            title = "Importar extrato em PDF (IA)",
            description = "Importe um PDF do extrato e deixe a inteligência artificial extrair os lançamentos para você revisar e confirmar.",
            group = "Financeiro",
            order = 27,
            markdown = """
# Importar extrato em PDF (com IA)

Alguns bancos só entregam o extrato em **PDF**. A importação por PDF usa **inteligência artificial (IA)** para ler o arquivo e extrair os lançamentos automaticamente — você **não precisa mapear colunas** como no CSV. Depois é só revisar e confirmar.

> Diferente do OFX e do CSV, aqui a IA interpreta o PDF do jeito que o banco montou. Por isso vale conferir os valores extraídos antes de confirmar.

## Como funciona

Você envia o PDF do extrato e a IA identifica cada lançamento: data, descrição e valor (entrada ou saída). O Osiris monta a lista para você revisar — nada é salvo até você confirmar.

A leitura acontece em um serviço de IA na nuvem, então é preciso **conexão com a internet** e o processamento **pode levar alguns segundos**.

## Como importar

1. No celular, abra o extrato da conta, toque em **Importar** e escolha **Importar PDF (IA)**. No computador, abra a conta e clique em **Importar PDF (IA)**.
2. Aguarde a IA ler o arquivo (alguns segundos).
3. Revise a lista de lançamentos, escolha as categorias e confirme.

## Revisar antes de confirmar

Como a IA pode errar em algum lançamento, **revise com atenção** antes de confirmar:

- **Confira valores e datas:** veja se os valores e o sentido (entrada ou saída) batem com o extrato.
- **Marcar e desmarcar:** desmarque o que não quiser importar.
- **Categoria:** escolha a categoria de cada lançamento, ou aplique a mesma a todos. A categoria é opcional.

Nada é salvo até você confirmar.

## Lançamentos repetidos não entram duas vezes

O Osiris gera uma chave estável a partir de data, valor e descrição. Se você importar o mesmo período de novo (por OFX, CSV ou PDF), os repetidos aparecem como **"Já importado"** e vêm desmarcados. Assim dá para reimportar sem duplicar o saldo.

## Pontos de atenção

- **A IA pode errar.** Sempre confira os lançamentos extraídos antes de confirmar.
- **Precisa de internet** e pode levar alguns segundos.
- **PDFs protegidos por senha** não funcionam — remova a senha e tente de novo.
- O arquivo deve ter no máximo **15 MB**.

## Depois de importar

Os lançamentos entram no extrato da conta como qualquer outro, e o saldo é atualizado na hora. Como todo lançamento, eles **não podem ser editados nem excluídos** depois — se algo veio errado, registre um lançamento de correção no sentido contrário.

## Resumo

1. Baixe o extrato em PDF no seu banco.
2. Abra a conta e escolha Importar PDF (IA).
3. Aguarde a IA ler o arquivo.
4. Revise com atenção, escolha categorias e confirme.
5. Reimporte quando quiser: os repetidos não entram de novo.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "cards",
            title = "Guia simples de cartões",
            description = "Entenda o que é um cartão de crédito no Osiris, limite, fechamento e vencimento, e como ele difere de uma conta.",
            group = "Financeiro",
            order = 30,
            markdown = """
# Guia simples de cartões

Um cartão de crédito responde a uma pergunta diferente da conta:

> Quanto eu posso gastar agora para pagar depois?

No Osiris, o cartão guarda as informações de como ele funciona: o limite, o dia em que a fatura fecha e o dia em que ela vence. Você também registra as compras feitas no cartão, e o Osiris monta as faturas automaticamente.

## Cartão não é a mesma coisa que conta

São coisas diferentes:

| Conta | Cartão de crédito |
| --- | --- |
| É onde o seu dinheiro está guardado | É uma forma de pagar agora e quitar depois |
| O saldo sobe e desce na hora | A compra entra na fatura e só sai dinheiro quando você paga |

Por isso o cartão fica em uma área separada das contas.

## O que você informa ao cadastrar um cartão

| Campo | O que significa |
| --- | --- |
| Nome | Como você reconhece o cartão (por exemplo, "Nubank" ou "Cartão da loja") |
| Limite | O valor máximo que o cartão permite gastar |
| Dia de fechamento | O dia do mês em que a fatura fecha e para de aceitar novas compras daquele ciclo |
| Dia de vencimento | O dia do mês em que a fatura precisa ser paga |
| Conta de pagamento | A conta usada por padrão para pagar a fatura (opcional) |

## Fechamento e vencimento

São duas datas diferentes e é normal confundir:

- **Fechamento:** o dia em que a fatura "fecha". Compras feitas depois dessa data entram na próxima fatura.
- **Vencimento:** o dia em que você precisa pagar a fatura que fechou.

Por exemplo, uma fatura pode fechar no dia 3 e vencer no dia 10. Você informa esses dias como números de 1 a 31.

## Conta de pagamento

Você pode escolher uma conta para ser a forma padrão de pagar a fatura do cartão. Isso é opcional e só aparecem contas do seu próprio cadastro. Se a conta escolhida for arquivada depois, o cartão continua apontando para ela até você trocar.

## Compras no cartão

Cada compra que você faz no cartão é registrada com descrição, valor, data e uma categoria de despesa. É a compra que conta como gasto na sua visão por categoria — e ela conta no momento em que acontece, não quando a fatura é paga.

Se a compra for parcelada, você informa o número de parcelas. O Osiris divide o valor em partes iguais (centavos que sobrarem vão para a última parcela) e coloca cada parcela na fatura do mês correspondente.

Você pode excluir uma compra enquanto nenhuma fatura dela estiver paga. Ao excluir, as parcelas saem das faturas.

## Faturas criadas automaticamente

Você não precisa criar faturas. Quando uma compra é registrada, o Osiris descobre em qual fatura cada parcela entra:

- Compras feitas até o dia do fechamento entram na fatura daquele mês.
- Compras feitas depois do fechamento entram na fatura do mês seguinte.
- Em meses mais curtos (como fevereiro), vale o último dia do mês.

## Pagar a fatura

Na tela da fatura você vê o total, o quanto já foi pago, o saldo em aberto, o fechamento, o vencimento e a situação dela (aberta, fechada, parcialmente paga, paga ou vencida).

Você pode pagar o valor total ou registrar pagamentos parciais, quantas vezes precisar, até quitar a fatura. Se escolher uma conta, o valor sai do saldo dela como "pagamento de fatura".

Importante: pagar a fatura não cria uma nova despesa por categoria. O gasto já foi contado quando a compra aconteceu — o pagamento apenas quita a dívida e movimenta o caixa. Assim o mesmo gasto nunca conta duas vezes.

## Arquivar um cartão

Arquivar esconde o cartão das novas escolhas sem apagar o histórico. Use quando parar de usar um cartão, mas quiser manter os registros antigos. Um cartão arquivado continua aparecendo no histórico, mas não recebe novas compras.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "purchases",
            title = "Compras no cartão",
            description = "Registre compras à vista ou parceladas e entenda como elas entram nas faturas.",
            group = "Financeiro",
            order = 40,
            markdown = """
# Compras no cartão

A tela de compras no cartão registra gastos feitos no cartão de crédito. É aqui que entram compras à vista, compras parceladas e a categoria do gasto.

## Quando usar

Use compras no cartão quando você comprou algo no crédito e vai pagar depois pela fatura.

Exemplos:

| Situação | Registrar como |
| --- | --- |
| Mercado no cartão | Compra no cartão |
| Celular parcelado em 10 vezes | Compra no cartão com 10 parcelas |
| Restaurante no crédito | Compra no cartão |
| Assinatura cobrada no cartão | Compra no cartão |

Não registre a mesma compra como saída da conta. A conta só será movimentada quando a fatura for paga.

## Antes de registrar

Antes da primeira compra, confira:

1. O cartão está cadastrado.
2. A categoria de despesa existe.
3. O dia de fechamento e vencimento do cartão estão corretos.

Esses dados definem em qual fatura a compra vai cair.

## Campos da compra

| Campo | Como preencher |
| --- | --- |
| Descrição | Nome simples para reconhecer a compra |
| Valor total | Valor completo da compra, mesmo quando parcelada |
| Data da compra | Data em que a compra aconteceu |
| Categoria | Motivo do gasto |
| Parcelas | Quantidade de parcelas |
| Observações | Informação opcional para lembrar depois |

Se a compra foi à vista no cartão, use 1 parcela.

## Compra parcelada

Em compras parceladas, informe o valor total e o número de parcelas. O Osiris divide o valor entre as parcelas e coloca cada uma na fatura correspondente.

Exemplo:

| Campo | Valor |
| --- | --- |
| Descrição | Celular |
| Valor total | R$ 2.000,00 |
| Parcelas | 10 |
| Categoria | Eletrônicos |

O gasto é o valor total da compra, mas o pagamento acontece pelas faturas ao longo dos meses.

## Prévia de parcelas

Ao preencher valor, data e parcelas, a tela mostra uma prévia. Use essa prévia para conferir:

- quantas parcelas serão criadas;
- em quais faturas elas entrarão;
- qual valor ficará em cada parcela;
- se a primeira parcela caiu no mês esperado.

Se a primeira parcela caiu em uma fatura diferente do esperado, confira o dia de fechamento do cartão.

## Fechamento muda a fatura

O dia de fechamento define se a compra entra na fatura atual ou na próxima.

Exemplo: se o cartão fecha no dia 5, uma compra no dia 4 tende a entrar na fatura do mês atual. Uma compra no dia 6 tende a ir para a próxima fatura.

Em meses mais curtos, o sistema usa o último dia possível do mês.

## Categorias em compras no cartão

A categoria da compra é o motivo do gasto. Não use "Cartão" como categoria.

Exemplos:

| Compra | Categoria melhor |
| --- | --- |
| Supermercado no cartão | Mercado |
| Corrida por aplicativo | Transporte |
| Streaming | Assinaturas |
| Remédio | Saúde |

Essa categoria alimenta o gráfico de gastos do painel.

## Excluir compra

Você pode excluir uma compra enquanto nenhuma fatura dela estiver paga. Ao excluir, as parcelas saem das faturas.

Se alguma parcela já pertence a uma fatura paga, a exclusão é bloqueada para preservar o histórico.

## Conferência rápida

Depois de registrar uma compra:

1. Abra os detalhes da compra.
2. Confira as parcelas.
3. Veja a fatura associada a cada parcela.
4. Volte ao cartão e confira limite usado e fatura atual.

## Regra simples

Compra no cartão é gasto por categoria. Pagamento da fatura é saída de caixa. Não registre os dois como despesa por categoria.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "statements",
            title = "Faturas de cartão",
            description = "Acompanhe fechamento, vencimento, status, saldo em aberto e pagamentos de fatura.",
            group = "Financeiro",
            order = 50,
            markdown = """
# Faturas de cartão

Faturas reúnem as compras de um cartão em um ciclo de cobrança. Elas mostram quanto foi comprado, quanto já foi pago e quanto ainda está em aberto.

## Como as faturas aparecem

Você não precisa criar faturas manualmente. Elas são criadas quando compras são registradas no cartão.

O Osiris usa:

- data da compra;
- dia de fechamento do cartão;
- dia de vencimento do cartão;
- número de parcelas.

Com isso, cada compra ou parcela entra na fatura correta.

## Referência, fechamento e vencimento

Cada fatura tem três datas importantes:

| Campo | O que significa |
| --- | --- |
| Referência | Mês e ano da fatura |
| Fechamento | Data em que o ciclo fecha |
| Vencimento | Data limite para pagar |

Compras depois do fechamento entram na próxima fatura.

## Status da fatura

Os status ajudam a entender a situação:

| Status | Significado |
| --- | --- |
| Aberta | Ainda está no ciclo atual ou possui saldo em aberto |
| Fechada | O ciclo fechou e ainda existe valor a pagar |
| Parcialmente paga | Já houve pagamento, mas falta saldo |
| Paga | O valor total foi quitado |
| Vencida | Passou do vencimento e ainda existe saldo em aberto |

## Total, pago e em aberto

Na fatura, confira:

- **Total:** soma das compras e parcelas daquela fatura.
- **Pago:** soma dos pagamentos registrados.
- **Em aberto:** valor que ainda falta pagar.

Se o valor em aberto for zero, a fatura está quitada.

## Pagar fatura

Para pagar, abra a fatura e use a ação de pagamento. Você pode pagar o total ou registrar pagamentos parciais.

Campos principais:

| Campo | Como usar |
| --- | --- |
| Valor | Valor pago naquele momento |
| Data | Dia em que o pagamento aconteceu |
| Conta | Conta financeira de onde o dinheiro saiu |
| Observações | Informação opcional |

Quando você escolhe uma conta, o pagamento reduz o saldo dela.

## Pagamento não duplica gasto

O gasto por categoria já foi contado quando a compra foi registrada. O pagamento da fatura apenas quita a dívida e movimenta o caixa.

Por isso:

- a compra aparece nos gastos por categoria;
- o pagamento aparece na visão de caixa;
- o mesmo gasto não é contado duas vezes.

## Pagamentos parciais

Se você pagar apenas parte da fatura, registre o valor pago. A fatura fica parcialmente paga e mantém o saldo restante em aberto.

Quando pagar o restante, registre um novo pagamento. O histórico de pagamentos fica preservado.

## PDF da fatura

A tela da fatura permite exportar PDF. Use o PDF quando precisar guardar, enviar ou conferir uma fatura fora do sistema.

O PDF reflete os dados registrados no Osiris no momento da exportação.

## Quando revisar uma fatura

Revise a fatura quando:

- o total não bate com o banco;
- uma compra caiu no mês errado;
- uma parcela não apareceu;
- o pagamento foi feito, mas a fatura continua em aberto;
- o painel mostra fatura vencida.

Na maioria dos casos, a correção está na data da compra, no fechamento do cartão ou no pagamento que ainda não foi registrado.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "bills",
            title = "Contas a pagar",
            description = "Use contas a pagar para obrigações fora do cartão, como aluguel, internet, escola e boletos.",
            group = "Financeiro",
            order = 60,
            markdown = """
# Contas a pagar

Contas a pagar são obrigações fora do cartão de crédito. Elas ajudam a acompanhar vencimentos, valores em aberto e pagamentos de despesas como aluguel, internet, escola, academia, boletos e assinaturas fora do cartão.

## Quando usar

Use contas a pagar quando existe uma despesa com vencimento e ela não entrou no cartão.

Exemplos:

| Situação | Registrar como |
| --- | --- |
| Aluguel do mês | Conta a pagar |
| Internet no boleto | Conta a pagar |
| Escola ou curso | Conta a pagar |
| Academia no débito ou boleto | Conta a pagar |
| Assinatura fora do cartão | Conta a pagar |

Se a cobrança veio no cartão de crédito, registre como compra no cartão.

## Campos da conta

| Campo | Como preencher |
| --- | --- |
| Descrição | Nome da obrigação |
| Valor | Valor a pagar |
| Vencimento | Data limite de pagamento |
| Categoria | Motivo da despesa |
| Conta de pagamento | Conta de onde o dinheiro costuma sair |
| Observações | Informação opcional |

A conta de pagamento é útil para agilizar a baixa, mas pode ser ajustada no momento de marcar como paga.

## Lista do mês

A tela mostra as contas do mês e ano selecionados. Use o filtro para revisar meses anteriores ou planejar vencimentos futuros.

Na lista você consegue:

- abrir detalhes;
- marcar como paga;
- marcar como pendente;
- editar;
- excluir.

## Marcar como paga

Quando a conta for paga, marque como paga. Se uma conta financeira for informada, o saldo dela diminui.

Exemplo:

| Conta | Valor | Conta de pagamento |
| --- | ---: | --- |
| Internet | R$ 120,00 | Banco principal |

Ao marcar como paga, o saldo do Banco principal diminui R$ 120,00 e o painel passa a tratar a obrigação como quitada.

## Marcar como pendente

Se você marcou uma conta como paga por engano, volte para pendente. O sistema desfaz a baixa de caixa associada, quando houver.

Use essa opção logo que perceber o erro, para manter painel e saldo coerentes.

## Editar ou excluir

Edite quando a conta ainda representa a mesma obrigação, mas algum dado precisa mudar, como vencimento, categoria, valor ou observação.

Exclua apenas quando a conta foi cadastrada por engano. Se a conta existiu de verdade e foi paga, manter o histórico costuma ser melhor.

## Conta a pagar e categoria

A categoria explica o motivo da despesa. Ela alimenta a visão de gastos do painel.

Exemplos:

| Conta | Categoria |
| --- | --- |
| Aluguel | Moradia |
| Internet | Casa ou Assinaturas |
| Mensalidade da escola | Educação |
| Plano de saúde | Saúde |

## Conta a pagar e caixa

A conta a pagar aparece de duas formas:

- como gasto por categoria, pelo valor da obrigação;
- como saída de caixa, quando é marcada como paga com uma conta financeira.

Se ela ainda está aberta, o painel usa o valor para calcular o saldo previsto do mês.

## Boas práticas

Cadastre as contas assim que souber o vencimento. Isso deixa o painel útil antes do pagamento acontecer.

Use descrições consistentes, como "Internet", "Aluguel" e "Escola", para facilitar a leitura dos relatórios.

Se uma conta se repete todo mês, cadastre cada ocorrência enquanto ainda não houver recorrência automática.
            """.trimIndent(),
        ),
        DocumentationGuide(
            slug = "reports",
            title = "Relatórios",
            description = "Baixe PDFs sintéticos ou analíticos para conferir o caixa do mês e o saldo previsto.",
            group = "Financeiro",
            order = 70,
            markdown = """
# Relatórios

Relatórios geram PDFs para conferir o caixa de um mês. Eles são úteis para revisar, guardar ou compartilhar um resumo financeiro sem depender da tela do painel.

## Escolher mês e ano

Na tela de relatórios, escolha o mês e o ano antes de baixar o PDF. Os arquivos gerados usam esse período como referência.

Se você alterar o filtro, clique em atualizar ou baixe o relatório desejado com o período selecionado.

## Visão de caixa sintética

A visão sintética é o resumo. Use quando quiser uma leitura rápida.

Ela mostra itens como:

- receitas do mês;
- saídas de caixa;
- contas em aberto;
- faturas em aberto;
- saldo em contas;
- saldo previsto.

É o melhor relatório para responder:

> Depois de pagar o que falta no mês, como fica meu caixa?

## Visão de caixa analítica

A visão analítica detalha os valores que explicam o resumo. Use quando precisar conferir de onde veio cada número.

Ela pode incluir:

- contas financeiras;
- entradas;
- despesas diretas;
- pagamentos de contas;
- pagamentos de faturas;
- faturas do cartão;
- obrigações em aberto.

É o melhor relatório para revisar divergências ou entender por que o saldo previsto mudou.

## Diferença entre gastos e caixa

Relatórios seguem a mesma regra do painel:

- gasto por categoria mostra o motivo da despesa;
- caixa mostra entrada e saída real das contas.

Uma compra no cartão é gasto quando acontece. O pagamento da fatura é saída de caixa quando você paga. O relatório evita contar o mesmo gasto duas vezes.

## Quando usar cada PDF

| Necessidade | Use |
| --- | --- |
| Guardar um resumo mensal | Sintético |
| Conferir por que o saldo previsto está baixo | Analítico |
| Revisar contas e faturas abertas | Analítico |
| Compartilhar visão simples | Sintético |
| Auditar lançamentos antes de fechar o mês | Analítico |

## Antes de exportar

Para o PDF refletir melhor a realidade, confira:

1. Compras do cartão estão registradas.
2. Faturas pagas foram marcadas.
3. Contas a pagar foram marcadas como pagas ou pendentes corretamente.
4. Lançamentos diretos das contas estão atualizados.
5. O mês e ano selecionados estão corretos.

## O que fazer se o PDF não bater

Se o relatório não bater com sua expectativa:

- revise o filtro de mês e ano;
- confira se há contas vencidas ainda pendentes;
- abra as faturas do mês e confira pagamentos;
- veja se alguma compra caiu em fatura diferente por causa do fechamento;
- confira o extrato das contas financeiras.

Depois de corrigir os dados, baixe o PDF novamente.

## Histórico

O PDF é um retrato do momento em que foi gerado. Se você alterar dados depois, gere outro relatório para ter a versão atualizada.
            """.trimIndent(),
        ),
    ).sortedWith(compareBy<DocumentationGuide> { it.order }.thenBy { it.title })

    fun find(slug: String): DocumentationGuide? = guides.firstOrNull { it.slug == slug }

    fun groups(): List<String> = guides.map { it.group }.distinct()
}
