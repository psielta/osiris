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
| Valor | Valor completo da compra ou, escolhendo "Valor da parcela", quanto vem em cada parcela |
| Data da compra | Data em que a compra aconteceu |
| Categoria | Motivo do gasto |
| Parcelas | Quantidade de parcelas |
| Observações | Informação opcional para lembrar depois |

Se a compra foi à vista no cartão, use 1 parcela.

## Compra parcelada

Em compras parceladas, informe o número de parcelas e o valor. Use o seletor **"Tipo de valor informado"** para escolher como o valor é interpretado:

- **Valor total** — o valor completo da compra; o Osiris divide entre as parcelas.
- **Valor da parcela** — quanto vem em cada parcela; o Osiris multiplica pelo número de parcelas para chegar ao total. Útil para compras retroativas em que a fatura só mostra o valor da parcela, sem precisar multiplicar na calculadora.

Em ambos os casos, o Osiris coloca cada parcela na fatura correspondente.

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
